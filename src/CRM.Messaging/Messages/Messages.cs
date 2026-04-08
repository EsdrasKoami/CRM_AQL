namespace CRM.Messaging.Messages;

/// <summary>
/// Structure du JSON entrant dans 'crm-commandes'.
/// Contient soit une commande réelle, soit un signal de test.
/// </summary>
public class IncomingMqMessage
{
    public string? NoClient { get; set; }
    public decimal? MontantTotal { get; set; }
    public string? Reference { get; set; }
    
    // Utilisé pour identifier un "TestMessage"
    public string? MessageType { get; set; }
}

/// <summary>
/// Structure de la réponse publiée dans 'edi-reponses'.
/// </summary>
public record EdiResponse(
    string Status, // "Approved" | "Rejected"
    string? Reference);

/// <summary>
/// [INTERNE] Utilisé pour le déclenchement de facturation via API ou MQ interne.
/// </summary>
public record ExpeditionMessage(
    int NoContrat,
    string NoProduit,
    int Quantite,
    string? Reference = null,
    int? NoUtilisateur = null);

