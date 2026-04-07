using CRM.Domain.Interfaces.Repositories;
using CRM.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace CRM.Business.Services;

/// <summary>
/// Contrôle de crédit : vérifie que le solde client ne dépasse pas le MontantMax du contrat.
/// Le déblocage du plafond est réservé au rôle DirecteurFinances.
/// </summary>
public class CreditControlService : ICreditControlService
{
    private readonly IContratRepository     _contratRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ILogger<CreditControlService> _logger;

    public CreditControlService(
        IContratRepository contratRepo,
        ITransactionRepository transactionRepo,
        ILogger<CreditControlService> logger)
    {
        _contratRepo     = contratRepo;
        _transactionRepo = transactionRepo;
        _logger          = logger;
    }

    /// <inheritdoc />
    public async Task<bool> CommandeAutoriseeAsync(string noClient, decimal montantCommande, CancellationToken ct = default)
    {
        var soldes = await _transactionRepo.GetSoldeClientAsync(noClient, ct);
        var solde = soldes.FirstOrDefault(s => s.ContratActif);

        if (solde is null)
        {
            _logger.LogWarning("CreditControl — Aucun contrat actif pour {NoClient}.", noClient);
            return false;
        }

        var totalApres = solde.SoldeContrat + montantCommande;
        var autorisee  = totalApres <= solde.MontantMax;

        _logger.LogInformation(
            "CreditControl — Client {NoClient}: solde={Solde:C}, commande={Commande:C}, plafond={Plafond:C} → {Resultat}",
            noClient, solde.SoldeContrat, montantCommande, solde.MontantMax,
            autorisee ? "AUTORISÉE" : "BLOQUÉE");

        return autorisee;
    }

    /// <inheritdoc />
    /// <remarks>Doit être appelé uniquement par un utilisateur avec le rôle DirecteurFinances.</remarks>
    public async Task<bool> ModifierPlafondAsync(int noContrat, decimal nouveauMontantMax, CancellationToken ct = default)
    {
        var contrat = await _contratRepo.GetByIdAsync(noContrat, ct);
        if (contrat is null)
        {
            _logger.LogWarning("CreditControl — Contrat #{NoContrat} introuvable.", noContrat);
            return false;
        }

        var ancienPlafond = contrat.MontantMax;
        contrat.MontantMax = nouveauMontantMax;
        await _contratRepo.UpdateAsync(contrat, ct);

        _logger.LogInformation(
            "CreditControl — Plafond du contrat #{NoContrat} modifié : {Ancien:C} → {Nouveau:C}.",
            noContrat, ancienPlafond, nouveauMontantMax);

        return true;
    }
}
