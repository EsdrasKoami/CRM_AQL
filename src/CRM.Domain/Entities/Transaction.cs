using CRM.Domain.Enums;

namespace CRM.Domain.Entities;

/// <summary>
/// Représente une transaction financière : Facture (débit) ou Paiement (crédit).
/// </summary>
public class Transaction
{
    public int NoTransaction { get; set; }
    public int NoContrat { get; set; }
    public TypeTransaction TypeTransaction { get; set; }

    /// <summary>Toujours positif. Le type détermine le sens comptable.</summary>
    public decimal Montant { get; set; }

    public string? Reference { get; set; }
    public string? NoProduit { get; set; }
    public int? Quantite { get; set; }
    public DateTime DateTransaction { get; set; } = DateTime.UtcNow;
    public int? NoUtilisateur { get; set; }
    public string? Commentaire { get; set; }

    // Navigation
    public Contrat? Contrat { get; set; }
    public Utilisateur? Utilisateur { get; set; }
}
