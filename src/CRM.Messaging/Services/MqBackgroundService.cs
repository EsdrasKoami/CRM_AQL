using CRM.Domain.Interfaces.Services;
using CRM.Domain.Models;
using CRM.Messaging.Messages;
using CRM.Messaging.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace CRM.Messaging.Services;

/// <summary>
/// BackgroundService "Always On" : écoute en continu la file de messages.
/// Traite chaque CommandeMessage en appelant le pipeline complet :
///   ContratValid → CreditControl → JitQuota → CreerFacture
///
/// Remplacez le Channel&lt;T&gt; in-memory par RabbitMQ/Azure Service Bus
/// pour un environnement de production multi-serveurs.
/// </summary>
public sealed class MqBackgroundService : BackgroundService
{
    private readonly Channel<CommandeMessage>     _commandeChannel;
    private readonly Channel<ExpeditionMessage>   _expeditionChannel;
    private readonly IContratValidationService     _validationService;
    private readonly ICreditControlService         _creditService;
    private readonly IJitQuotaService              _jitService;
    private readonly IFacturationService           _facturationService;
    private readonly MessageQueueOptions           _options;
    private readonly ILogger<MqBackgroundService>  _logger;

    public MqBackgroundService(
        Channel<CommandeMessage> commandeChannel,
        Channel<ExpeditionMessage> expeditionChannel,
        IContratValidationService validationService,
        ICreditControlService creditService,
        IJitQuotaService jitService,
        IFacturationService facturationService,
        IOptions<MessageQueueOptions> options,
        ILogger<MqBackgroundService> logger)
    {
        _commandeChannel    = commandeChannel;
        _expeditionChannel  = expeditionChannel;
        _validationService  = validationService;
        _creditService      = creditService;
        _jitService         = jitService;
        _facturationService = facturationService;
        _options            = options.Value;
        _logger             = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MqBackgroundService démarré — écoute de la file [{Queue}].", _options.QueueName);

        // Traiter les deux channels en parallèle
        var t1 = ProcessCommandesAsync(stoppingToken);
        var t2 = ProcessExpeditionsAsync(stoppingToken);

        await Task.WhenAll(t1, t2);

        _logger.LogInformation("MqBackgroundService arrêté.");
    }

    // ── Commandes ────────────────────────────────────────────────────────────
    private async Task ProcessCommandesAsync(CancellationToken ct)
    {
        await foreach (var msg in _commandeChannel.Reader.ReadAllAsync(ct))
        {
            _logger.LogInformation(
                "MQ ← COMMANDE reçue: Client={Client}, Contrat={Contrat}, Produit={Produit}, Qté={Qte}",
                msg.NoClient, msg.NoContrat, msg.NoProduit, msg.Quantite);

            int tentative = 0;
            bool traite   = false;

            while (!traite && tentative < _options.MaxRetries)
            {
                tentative++;
                try
                {
                    await TraiterCommandeAsync(msg, ct);
                    traite = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "MQ — Erreur tentative {T}/{Max} pour la commande {Client}/{Produit}.",
                        tentative, _options.MaxRetries, msg.NoClient, msg.NoProduit);

                    if (tentative < _options.MaxRetries)
                        await Task.Delay(_options.RetryDelayMs, ct);
                }
            }

            if (!traite)
                _logger.LogCritical(
                    "MQ — Message ABANDONNÉ après {Max} tentatives: {Client}/{Produit}.",
                    _options.MaxRetries, msg.NoClient, msg.NoProduit);
        }
    }

    private async Task TraiterCommandeAsync(CommandeMessage msg, CancellationToken ct)
    {
        // 1. ContratValid()
        var validResult = await _validationService.ContratValidAsync(msg.NoClient, ct);
        if (!validResult.IsValid)
        {
            _logger.LogWarning("MQ — ContratValid=FALSE: {Raison}", validResult.Raison);
            return;
        }

        // 2. Contrôle de crédit
        // (Le montant de la commande sera calculé dans usp_CreerFacture selon le prix contrat)
        // On effectue une pré-vérification indicative ici.
        var itemPrix = 0m; // Sera vérifié atomiquement par la SP
        bool creditOk = await _creditService.CommandeAutoriseeAsync(msg.NoClient, itemPrix, ct);
        if (!creditOk)
        {
            _logger.LogWarning("MQ — Commande BLOQUÉE par contrôle de crédit pour {Client}.", msg.NoClient);
            return;
        }

        // 3. Quota JIT
        bool quotaOk = await _jitService.QuotaRespecteeAsync(
            msg.NoContrat, msg.NoProduit, msg.Quantite,
            DateTime.UtcNow.Year, DateTime.UtcNow.Month, ct);

        if (!quotaOk)
        {
            _logger.LogWarning(
                "MQ — Commande BLOQUÉE par quota JIT: Contrat={Contrat}, Produit={Produit}.",
                msg.NoContrat, msg.NoProduit);
            return;
        }

        // 4. La facture sera créée sur signal d'expédition (ExpeditionMessage).
        _logger.LogInformation(
            "MQ — Commande ACCEPTÉE: Client={Client}, Contrat={Contrat}, Produit={Produit}, Qté={Qte}.",
            msg.NoClient, msg.NoContrat, msg.NoProduit, msg.Quantite);
    }

    // ── Expéditions ───────────────────────────────────────────────────────────
    private async Task ProcessExpeditionsAsync(CancellationToken ct)
    {
        await foreach (var msg in _expeditionChannel.Reader.ReadAllAsync(ct))
        {
            _logger.LogInformation(
                "MQ ← EXPÉDITION reçue: Contrat={Contrat}, Produit={Produit}, Qté={Qte}",
                msg.NoContrat, msg.NoProduit, msg.Quantite);

            try
            {
                var p = new CreerFactureParams(
                    msg.NoContrat, msg.NoProduit, msg.Quantite,
                    msg.Reference, msg.NoUtilisateur,
                    "Facture auto sur expédition");

                int noTx = await _facturationService.CreerFactureSurExpeditionAsync(p, ct);

                _logger.LogInformation(
                    "MQ — Facture #{NoTx} créée sur expédition pour Contrat={Contrat}.",
                    noTx, msg.NoContrat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "MQ — Erreur lors de la facturation sur expédition: Contrat={Contrat}.",
                    msg.NoContrat);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MqBackgroundService — arrêt en cours...");
        // Signaler la fin de l'écriture pour débloquer les ReadAllAsync
        _commandeChannel.Writer.TryComplete();
        _expeditionChannel.Writer.TryComplete();
        await base.StopAsync(cancellationToken);
    }
}
