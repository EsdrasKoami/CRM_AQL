using System.Data;
using System.IO;
using System.Text;
using System.Text.Json;

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

public class RMQEnveloppe
{
    private string _xmlData = string.Empty;

    public RMQEnveloppe() { }

    public RMQEnveloppe(string messageName, string sender, string messageText, string xmlData)
    {
        MessageName = messageName;
        Sender = sender;
        MessageText = messageText;
        _xmlData = xmlData;
    }

    public string MessageName { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;

    public void SetData(DataSet? ds)
    {
        if (ds == null) { _xmlData = string.Empty; return; }
        using var sw = new StringWriter();
        ds.WriteXml(sw);
        _xmlData = sw.ToString();
    }

    public DataSet GetData()
    {
        var ds = new DataSet();
        if (!string.IsNullOrEmpty(_xmlData))
        {
            using var sr = new StringReader(_xmlData);
            ds.ReadXml(sr);
        }
        return ds;
    }

    public string XmlData { get => _xmlData; set => _xmlData = value; }

    public static RMQEnveloppe Deserialise(byte[] body)
    {
        var content = Encoding.UTF8.GetString(body);
        return JsonSerializer.Deserialize<RMQEnveloppe>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new RMQEnveloppe();
    }
}

