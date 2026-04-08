using CRM.Domain.Interfaces.Services;
using CRM.Messaging.Messages;
using CRM.Messaging.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace CRM.Messaging.Services;

/// <summary>
/// BackgroundService "Reactor Core" : Point d'entrée unique de l'EDI.
/// Utilise RabbitMQ pour le découplage total et la robustesse JIT.
/// </summary>
public sealed class MqBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MessageQueueOptions _options;
    private readonly ILogger<MqBackgroundService> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public MqBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<MessageQueueOptions> options,
        ILogger<MqBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MqBackgroundService démarré — Serveur: {Host}", _options.HostName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAsync(stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel!);
                consumer.ReceivedAsync += OnMessageReceivedAsync;

                await _channel!.BasicConsumeAsync(
                    queue: _options.QueueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("MqBackgroundService — Écoute active sur [{Queue}].", _options.QueueName);

                // Attendre indéfiniment (ou jusqu'à annulation)
                await Task.Delay(-1, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MqBackgroundService — Rupture de connexion. Nouvelle tentative dans {Delay}ms...", _options.RetryDelayMs);
                await Task.Delay(_options.RetryDelayMs, stoppingToken);
            }
        }
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            UserName = _options.UserName,
            Password = _options.Password
        };


        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

        // Déclaration des files (Durable = true pour la robustesse)
        await _channel.QueueDeclareAsync(_options.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: ct);
        await _channel.QueueDeclareAsync(_options.ResponseQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: ct);

        // Qualité de service : Traitement parallèle (10 messages max par service)
        await _channel.BasicQosAsync(0, 10, false, ct);
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var body = ea.Body.ToArray();
        var content = Encoding.UTF8.GetString(body);

        try
        {
            var incoming = JsonSerializer.Deserialize<IncomingMqMessage>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (incoming == null)
            {
                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            // [TIMESTAMP] MQ_REC : Commande reçue pour {noClient}
            _logger.LogInformation("MQ_REC : Commande reçue pour {NoClient}", incoming.NoClient ?? "Inconnu");

            string status = "Rejected";

            // Règle 5 : TestMessage handling
            if (incoming.MessageType == "TestMessage")
            {
                status = "Approved";
            }
            else
            {
                // Règle d'acier 1 : Pas de logique ici, on appelle la DLL
                using var scope = _serviceProvider.CreateScope();
                var validationService = scope.ServiceProvider.GetRequiredService<IContratValidationService>();
                var creditService = scope.ServiceProvider.GetRequiredService<ICreditControlService>();

                try
                {
                    var vResult = await validationService.ContratValidAsync(incoming.NoClient ?? "", CancellationToken.None);
                    
                    // On vérifie le crédit basé sur le montant total reçu du message
                    bool creditOk = await creditService.CommandeAutoriseeAsync(
                        incoming.NoClient ?? "", 
                        incoming.MontantTotal ?? 0m, 
                        CancellationToken.None);

                    if (vResult.IsValid && creditOk)
                    {
                        status = "Approved";
                    }

                    // [TIMESTAMP] SQL_CHECK : Validation via 172.16.88.120 - Résultat : {Statut}
                    _logger.LogInformation("SQL_CHECK : Validation via 172.16.88.120 - Résultat : {Status}", status);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SQL_CHECK : Erreur de connexion au serveur SQL 172.16.88.120.");
                    // En cas d'erreur DB, on Nack avec requeue pour retry (Règle d'acier 3 - Logic)
                    await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
                    return;
                }
            }

            // Règle 3 : Implémentation du "Répondre" (Publish)
            var response = new EdiResponse(status, incoming.Reference);
            var responseJson = JsonSerializer.Serialize(response);
            var responseBody = Encoding.UTF8.GetBytes(responseJson);

            await _channel!.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _options.ResponseQueueName,
                mandatory: true,
                basicProperties: new BasicProperties(),
                body: responseBody);

            // [TIMESTAMP] MQ_PUB : Réponse envoyée vers EDI
            _logger.LogInformation("MQ_PUB : Réponse envoyée vers EDI");

            await _channel!.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du traitement d'un message MQ.");
            // Si erreur de format, on ignore pour ne pas bloquer la file indéfiniment
            await _channel!.BasicAckAsync(ea.DeliveryTag, false);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MqBackgroundService — Arrêt en cours...");
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}

