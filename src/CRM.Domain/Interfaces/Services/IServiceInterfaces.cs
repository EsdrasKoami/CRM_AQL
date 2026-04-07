using CRM.Domain.Models;

namespace CRM.Domain.Interfaces.Services;

public interface IContratValidationService
{
    /// <summary>
    /// Vérifie si un client possède un contrat actif à la date courante.
    /// C'est le point central appelé par le BackgroundService MQ.
    /// </summary>
    Task<ContratValidResult> ContratValidAsync(string noClient, CancellationToken ct = default);
}

public interface ICreditControlService
{
    /// <summary>
    /// Retourne true si la commande peut être acceptée sans dépasser le MontantMax.
    /// </summary>
    Task<bool> CommandeAutoriseeAsync(string noClient, decimal montantCommande, CancellationToken ct = default);

    /// <summary>
    /// Débloquer manuellement la limite (rôle DirecteurFinances seulement).
    /// </summary>
    Task<bool> ModifierPlafondAsync(int noContrat, decimal nouveauMontantMax, CancellationToken ct = default);
}

public interface IJitQuotaService
{
    /// <summary>
    /// Vérifie que la quantité commandée respecte le quota mensuel JIT.
    /// </summary>
    Task<bool> QuotaRespecteeAsync(int noContrat, string noProduit, int quantite, int annee, int mois, CancellationToken ct = default);

    Task<IEnumerable<QuotaMensuelDto>> GetConsommationMensuelleAsync(int noContrat, int annee, int mois, CancellationToken ct = default);
}

public interface IFacturationService
{
    /// <summary>
    /// Crée une facture dès réception du signal d'expédition.
    /// </summary>
    Task<int> CreerFactureSurExpeditionAsync(CreerFactureParams p, CancellationToken ct = default);

    Task<int> EnregistrerPaiementAsync(EnregistrerPaiementParams p, CancellationToken ct = default);
}
