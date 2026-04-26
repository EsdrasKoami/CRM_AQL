using System.Text.Json;
using System.Text;

namespace CRM.MessagingConsole.Models;

/// <summary>
/// Représente l'enveloppe de message standard utilisée pour les échanges RabbitMQ.
/// Cette structure permet d'uniformiser les données transmises entre le CRM, l'EDI et l'ERP.
/// </summary>
public class RMQEnveloppe
{
    /// <summary> Nom ou Type du message (ex: ContratValid, 850, Facture) </summary>
    public string MessageName { get; set; } = string.Empty;

    /// <summary> Identifiant de l'expéditeur (ex: EDI, CRM, ERP) </summary>
    public string Sender { get; set; } = string.Empty;

    /// <summary> État du message (ex: Actif, Erreur) </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary> Corps du message en texte clair (détails de l'ordre, adresse, etc.) </summary>
    public string MessageText { get; set; } = string.Empty;

    public RMQEnveloppe() { }

    public RMQEnveloppe(string messageName, string sender, string status, string messageText)
    {
        MessageName = messageName;
        Sender = sender;
        Status = status;
        MessageText = messageText;
    }

    /// <summary>
    /// Désérialise un tableau de octets (RAW RabbitMQ) en un objet RMQEnveloppe.
    /// Gère les erreurs de format en retournant un objet "Unknown" sécurisé.
    /// </summary>
    public static RMQEnveloppe Deserialise(byte[] rawBody)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<RMQEnveloppe>(rawBody, options);
            if (result != null) return result;
        }
        catch
        {
            // Fallback si le format JSON n'est pas respecté ou est incompatible
        }

        // Retourne un message générique pour éviter les crashs de l'application
        return new RMQEnveloppe("Unknown", "System", "Error", Encoding.UTF8.GetString(rawBody));
    }
}
