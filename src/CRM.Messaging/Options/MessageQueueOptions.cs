namespace CRM.Messaging.Options;

/// <summary>
/// Configuration de la file de messages RabbitMQ.
/// Liée à la section "MessageQueue" dans appsettings.json.
/// </summary>
public class MessageQueueOptions
{
    public const string Section = "MessageQueue";

    /// <summary>Adresse du serveur RabbitMQ (ex: 172.16.88.118).</summary>
    public string HostName { get; set; } = "172.16.88.118";

    /// <summary>Nom d'utilisateur (défaut: guest).</summary>
    public string UserName { get; set; } = "guest";

    /// <summary>Mot de passe (défaut: guest).</summary>
    public string Password { get; set; } = "guest";

    /// <summary>Nom de la file d'entrée (EDI -> CRM).</summary>
    public string QueueName { get; set; } = "crm-commandes";

    /// <summary>Nom de la file de sortie (CRM -> EDI).</summary>
    public string ResponseQueueName { get; set; } = "edi-reponses";

    /// <summary>Délai entre deux tentatives en cas d'erreur (ms).</summary>
    public int RetryDelayMs { get; set; } = 5000;

    /// <summary>Nombre max de tentatives avant abandon d'un message.</summary>
    public int MaxRetries { get; set; } = 3;
}

