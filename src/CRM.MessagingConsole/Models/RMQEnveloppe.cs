using System.Text.Json;
using System.Text;

namespace CRM.MessagingConsole.Models;

public class RMQEnveloppe
{
    public string MessageName { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;

    public RMQEnveloppe() { }

    public RMQEnveloppe(string messageName, string sender, string status, string messageText)
    {
        MessageName = messageName;
        Sender = sender;
        Status = status;
        MessageText = messageText;
    }

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
            // Fallback in case it's not a standard Envelope
        }

        return new RMQEnveloppe("Unknown", "System", "Error", Encoding.UTF8.GetString(rawBody));
    }
}
