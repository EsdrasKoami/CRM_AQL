namespace CRM.Domain.Models;

/// <summary>DTO retourné par usp_GetSoldeClient.</summary>
public class SoldeClientDto
{
    public string NoClient { get; set; } = string.Empty;
    public string NomEntreprise { get; set; } = string.Empty;
    public int NoContrat { get; set; }
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public decimal MontantMax { get; set; }
    public decimal TotalFactures { get; set; }
    public decimal TotalPaiements { get; set; }
    public decimal SoldeContrat { get; set; }
    public decimal CreditDisponible { get; set; }
    public bool ContratActif { get; set; }
}

/// <summary>Paramètres pour créer une facture via stored proc.</summary>
public record CreerFactureParams(
    int NoContrat,
    string NoProduit,
    int Quantite,
    string? Reference = null,
    int? NoUtilisateur = null,
    string? Commentaire = null);

/// <summary>Paramètres pour enregistrer un paiement via stored proc.</summary>
public record EnregistrerPaiementParams(
    int NoContrat,
    decimal Montant,
    string? Reference = null,
    int? NoUtilisateur = null,
    string? Commentaire = null);

/// <summary>DTO pour les quotas JIT mensuels.</summary>
public class QuotaMensuelDto
{
    public string NoProduit { get; set; } = string.Empty;
    public string? DescriptionProduit { get; set; }
    public int QuotaMin { get; set; }
    public int QuotaMax { get; set; }
    public int QuantiteCommandee { get; set; }
    public int EcartMin { get; set; }
    public int EcartMax { get; set; }
    public string StatutQuota { get; set; } = string.Empty;
}

/// <summary>Résultat de la validation d'un contrat.</summary>
public record ContratValidResult(bool IsValid, string? Raison = null, int? NoContrat = null);
