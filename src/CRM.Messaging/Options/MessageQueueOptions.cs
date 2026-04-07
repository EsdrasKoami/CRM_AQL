namespace CRM.Messaging.Options;

/// <summary>
/// Configuration de la file de messages.
/// Liée à la section "MessageQueue" dans appsettings.json.
/// </summary>
public class MessageQueueOptions
{
    public const string Section = "MessageQueue";

    /// <summary>Nom de la queue (utilisé si on migre vers RabbitMQ/Azure SB).</summary>
    public string QueueName { get; set; } = "crm-commandes";

    /// <summary>Capacité maximale du Channel in-memory.</summary>
    public int Capacity { get; set; } = 1000;

    /// <summary>Délai entre deux tentatives en cas d'erreur (ms).</summary>
    public int RetryDelayMs { get; set; } = 5000;

    /// <summary>Nombre max de tentatives avant abandon d'un message.</summary>
    public int MaxRetries { get; set; } = 3;
}
