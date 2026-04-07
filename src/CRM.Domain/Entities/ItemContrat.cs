namespace CRM.Domain.Entities;

/// <summary>
/// Ligne d'un contrat : produit spécifique avec prix fixe et quotas mensuels JIT.
/// </summary>
public class ItemContrat
{
    public int NoItemContrat { get; set; }
    public int NoContrat { get; set; }
    public string NoProduit { get; set; } = string.Empty;
    public string? DescriptionProduit { get; set; }

    /// <summary>Prix unitaire négocié dans ce contrat.</summary>
    public decimal PrixUnitaire { get; set; }

    /// <summary>Quantité minimale mensuelle (JIT).</summary>
    public int QuotaMin { get; set; }

    /// <summary>Quantité maximale mensuelle (JIT).</summary>
    public int QuotaMax { get; set; }

    // Navigation
    public Contrat? Contrat { get; set; }
}
