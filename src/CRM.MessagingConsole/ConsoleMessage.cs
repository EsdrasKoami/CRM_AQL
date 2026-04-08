using System;

namespace CRM.MessagingConsole;

/// <summary>
/// Structure du message échangé entre les systèmes via RabbitMQ.
/// </summary>
public class ConsoleMessage
{
    public string De { get; set; } = string.Empty;
    public string A { get; set; } = string.Empty;
    public string Contenu { get; set; } = string.Empty;
    public DateTime EnvoyeLe { get; set; } = DateTime.UtcNow;
}
