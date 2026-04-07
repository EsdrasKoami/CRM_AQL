namespace CRM.Messaging.Messages;

/// <summary>Message de commande entrant dans la file MQ.</summary>
public record CommandeMessage(
    string NoClient,
    int NoContrat,
    string NoProduit,
    int Quantite,
    string? Reference = null,
    int? NoUtilisateur = null);

/// <summary>Réponse après traitement ContratValid() + pipeline complet.</summary>
public record CommandeResponse(
    bool Succes,
    string? NoTransaction,
    string? Erreur = null);

/// <summary>Signal d'expédition pour déclenchement de facture.</summary>
public record ExpeditionMessage(
    int NoContrat,
    string NoProduit,
    int Quantite,
    string? Reference = null,
    int? NoUtilisateur = null);
