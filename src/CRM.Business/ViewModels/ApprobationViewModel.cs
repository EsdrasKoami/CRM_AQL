using CRM.Domain.Interfaces.Services;

namespace CRM.Business.ViewModels;

public class ApprobationResult
{
    public bool EstApprouve { get; set; }
    public string Message { get; set; } = string.Empty;
    public string CouleurAffichage => EstApprouve ? "Vert" : "Rouge";
}

/// <summary>
/// Connecteur Vue/Services (ViewModel ou passe-plats) pour l'Interface Visuelle.
/// Gère la communication avec ContratValidationService et détermine
/// la logique d'affichage ("Approuvé" ou "Refusé", en vert ou en rouge)
/// </summary>
public class ApprobationViewModel
{
    private readonly IContratValidationService _contratValidationService;
    private readonly IJitQuotaService _jitQuotaService;

    public ApprobationViewModel(
        IContratValidationService contratValidationService,
        IJitQuotaService jitQuotaService)
    {
        _contratValidationService = contratValidationService;
        _jitQuotaService = jitQuotaService;
    }

    /// <summary>
    /// Évalue une demande de transaction pour un client et un produit, 
    /// en vérifiant le contrat et les quotas.
    /// Retourne un objet contenant le texte (Approuvé/Refusé) et la couleur d'affichage.
    /// </summary>
    public async Task<ApprobationResult> EvaluerDemandeApprobationAsync(
        string noClient, 
        string noProduit, 
        int quantite, 
        int annee, 
        int mois, 
        CancellationToken ct = default)
    {
        // 1. Validation du contrat
        var contratResult = await _contratValidationService.ContratValidAsync(noClient, ct);
        
        if (!contratResult.IsValid || contratResult.NoContrat == null)
        {
            return new ApprobationResult
            {
                EstApprouve = false,
                Message = $"Refusé : {contratResult.Raison ?? "Aucun contrat actif."}"
            };
        }

        int noContrat = contratResult.NoContrat.Value;

        // 2. Validation des quotas JIT
        bool quotaRespecte = await _jitQuotaService.QuotaRespecteeAsync(noContrat, noProduit, quantite, annee, mois, ct);

        if (!quotaRespecte)
        {
            return new ApprobationResult
            {
                EstApprouve = false,
                Message = "Refusé : Les quotas pour ce produit ne sont pas respectés."
            };
        }

        // 3. Tout est valide
        return new ApprobationResult
        {
            EstApprouve = true,
            Message = "Approuvé"
        };
    }
}
