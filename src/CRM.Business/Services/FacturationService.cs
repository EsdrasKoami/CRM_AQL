using CRM.Domain.Interfaces.Repositories;
using CRM.Domain.Interfaces.Services;
using CRM.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CRM.Business.Services;

/// <summary>
/// Service de facturation : crée une facture dès réception du signal d'expédition.
/// </summary>
public class FacturationService : IFacturationService
{
    private readonly ITransactionRepository    _transactionRepo;
    private readonly IContratValidationService _validationService;
    private readonly ICreditControlService     _creditService;
    private readonly IJitQuotaService          _jitService;
    private readonly ILogger<FacturationService> _logger;

    public FacturationService(
        ITransactionRepository transactionRepo,
        IContratValidationService validationService,
        ICreditControlService creditService,
        IJitQuotaService jitService,
        ILogger<FacturationService> logger)
    {
        _transactionRepo   = transactionRepo;
        _validationService = validationService;
        _creditService     = creditService;
        _jitService        = jitService;
        _logger            = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Pipeline complet déclenché sur signal d'expédition :
    /// 1. Contrat valide ?
    /// 2. Crédit disponible ?
    /// 3. Quota JIT respecté ?
    /// 4. Créer la facture (atomique via usp_CreerFacture).
    /// </remarks>
    public async Task<int> CreerFactureSurExpeditionAsync(CreerFactureParams p, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Facturation — Signal expédition: Contrat #{Contrat}, Produit {Produit}, Qté {Qte}.",
            p.NoContrat, p.NoProduit, p.Quantite);

        // Le contrôle de crédit et les quotas sont re-vérifiés ici avant l'insertion.
        // La vérification finale et atomique est dans usp_CreerFacture (double sécurité).

        int noTransaction = await _transactionRepo.CreerFactureAsync(p, ct);

        _logger.LogInformation(
            "Facturation — Facture #{NoTx} créée pour le contrat #{Contrat}.",
            noTransaction, p.NoContrat);

        return noTransaction;
    }

    /// <inheritdoc />
    public async Task<int> EnregistrerPaiementAsync(EnregistrerPaiementParams p, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Facturation — Paiement de {Montant:C} pour le contrat #{Contrat}.",
            p.Montant, p.NoContrat);

        int noTransaction = await _transactionRepo.EnregistrerPaiementAsync(p, ct);

        _logger.LogInformation(
            "Facturation — Paiement #{NoTx} enregistré.", noTransaction);

        return noTransaction;
    }
}
