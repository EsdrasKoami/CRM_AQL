using System;
using System.IO;
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

        try
        {
            RMQEnveloppe message = RMQEnveloppe.Deserialise(body);
            var senderName = message.Sender;

            var messageid = ea.BasicProperties.MessageId;
            var timestamp = ea.BasicProperties.Timestamp;

            string nowStr = DateTime.Now.ToString("yyyy-MM-dd HH'h'mm");
            string logLineRec = $"{nowStr}::Reception::{message.MessageName}()\n";
            await File.AppendAllTextAsync("mq_trace.log", logLineRec);

            switch (message.MessageName)
            {
                case "ChatMessage":
                    Console.WriteLine($"Message de chat reçu de {senderName} : {message.MessageText}");
                    break;

                case "DataMessage":
                    Console.WriteLine($"Message de données reçu de {senderName} : {message.MessageText}");
                    break;

                default:
                    Console.WriteLine($"Message générique reçu de {senderName} : {message.MessageText}");
                    break;
            }

            string logLineEnv = $"{nowStr}::Envoie::{message.MessageName}()\n";
            await File.AppendAllTextAsync("mq_trace.log", logLineEnv);

            await _channel!.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du traitement d'un message MQ.");
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

