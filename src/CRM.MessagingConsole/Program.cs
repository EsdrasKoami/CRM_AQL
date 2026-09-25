using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using CRM.MessagingConsole.Models;
using CRM.MessagingConsole.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CRM.MessagingConsole;

/// <summary>
/// Point d'entrée principal du module d'orchestration CRM / Smart Factory.
/// Cette console permet de piloter et superviser les échanges asynchrones RabbitMQ
/// et d'orchestrer la Saga industrielle (Contrat -> Ordonnancement ERP -> Contrôle Qualité MES -> Facturation EDI).
/// </summary>
class Program
{
    // Configuration de la connexion RabbitMQ
    private const string RmqHost = "172.16.88.227";
    private const int RmqPort = 5672;
    private const string RmqUser = "guest";
    private const string RmqPass = "guest";

    // Configuration de la traçabilité et de l'audit distribué
    private const string LogQueue = "logs";
    private const string ServiceIdentifier = "Esdras - CRM"; // Identifiant du service d'orchestration CRM

    private static IConnection? _connection;
    private static IChannel? _publishChannel;
    private static readonly CrmRepository _repo = new();

    /// <summary>
    /// Initialisation de l'application et gestion du cycle de vie des connexions.
    /// </summary>
    static async Task Main()
    {
        Console.WriteLine("======================================================");
        Console.WriteLine(" MODULE CRM INDUSTRIEL - CONSOLE D'ORCHESTRATION JIT  ");
        Console.WriteLine("======================================================\n");

        // Vérification préliminaire de la connectivité réseau vers RabbitMQ
        if (!CheckRabbitMqAvailibility())
        {
            Console.ReadLine();
            return;
        }

        try
        {
            // Initialisation des ressources de messagerie
            await InitializeRabbitMqAsync();

            // Écoute des files critiques pour le CRM et mouchards pour EDI/ERP
            await StartConsumersAsync(new[] { "crm", "edi", "erp" });

            // Lancement de l'interface utilisateur interactive
            await RunDashboardAsync();
        }
        catch (Exception ex) { WriteColor($"[ERREUR] {ex.Message}", ConsoleColor.Red); }
        finally
        {
            // Libération propre des ressources RabbitMQ
            if (_publishChannel != null) await _publishChannel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();
        }
    }

    /// <summary>
    /// Boucle de navigation principale permettant de piloter le workflow ou simuler des flux partenaires.
    /// </summary>
    private static async Task RunDashboardAsync()
    {
        Console.WriteLine("\n[ SYSTÈME D'ORCHESTRATION PRÊT - ÉCOUTE ACTIVE DES FILES ]");
        Console.WriteLine("Les flux partenaires entrants (EDI / ERP / MES) sont analysés et traités en temps réel.");

        while (true)
        {
            Console.WriteLine("\n--------------------------------------------------------------");
            Console.WriteLine(" WORKFLOW INDUSTRIEL & SUPERVISION CRM                       ");
            Console.WriteLine("--------------------------------------------------------------");
            Console.WriteLine(" >> PILOTAGE DE LA SAGA INDUSTRIELLE : ");
            Console.WriteLine("  1. Déclencher l'ordre de fabrication 'Ceduler' vers l'ERP (Usine)");
            Console.WriteLine("  2. Transmettre le 'CertificatQualite' validé vers l'EDI");
            Console.WriteLine("  3. Émettre la 'Facture' officielle vers l'EDI");
            Console.WriteLine("  4. Émission libre : Spécifier la file cible et le message");
            Console.WriteLine("\n >> SIMULATION DE FLUX ENTRANTS (Tests d'intégration) : ");
            Console.WriteLine("  S. Simuler un événement entrant (EDI / ERP -> CRM)");
            Console.WriteLine("--------------------------------------------------------------");
            Console.Write("Votre Choix : ");

            var choix = Console.ReadLine()?.Trim().ToUpper();

            // Gestion des commandes manuelles
            if (choix == "1")
            {
                var envelope = new RMQEnveloppe("Ceduler", "CRM", "Actif", "Ordre de fabrication CRM");
                await PublishMessageAsync("erp", "Ceduler", JsonSerializer.Serialize(envelope));
            }
            else if (choix == "2")
            {
                var envelope = new RMQEnveloppe("CertificatQualite", "CRM", "Actif", "Certificat certifié par CRM");
                await PublishMessageAsync("edi", "CertificatQualite", JsonSerializer.Serialize(envelope));
            }
            else if (choix == "3")
            {
                var envelope = new RMQEnveloppe("Facture", "CRM", "Actif", "Facture officielle de 500$");
                await PublishMessageAsync("edi", "Facture", JsonSerializer.Serialize(envelope));
                _repo.EnregistrerTransaction("C1", 500m, "Facture");
            }
            else if (choix == "4") // Mode expert pour tout type de message
            {
                Console.Write(" >> VERS quelle file (ex: edi, erp, crm) ? : ");
                var cible = Console.ReadLine()?.Trim().ToLower();
                Console.Write(" >> QUEL NOM de message (Ex: ContratValid) ? : ");
                var nom = Console.ReadLine()?.Trim();

                if (!string.IsNullOrWhiteSpace(cible) && !string.IsNullOrWhiteSpace(nom))
                {
                    var envelope = new RMQEnveloppe(nom, "CRM_Manuel", "Actif", $"Généré manuellement: {nom}");
                    await PublishMessageAsync(cible, nom, JsonSerializer.Serialize(envelope));
                }
            }
            else if (choix == "S") // Simulation pour prouver la logique sans dépendre d'un tiers
            {
                Console.WriteLine(" [Simulation] Quel message entrant voulez-vous simuler ?");
                Console.WriteLine("   a) L'EDI envoie 'ContratValid'");
                Console.WriteLine("   b) L'ERP envoie 'CertificatQualite'");
                Console.WriteLine("   c) L'EDI envoie 'Paiement'");
                Console.WriteLine("   d) L'EDI envoie 'ContratValid' (Client Insolvable)");
                Console.Write(" > (a/b/c/d) : ");
                var subChoix = Console.ReadLine()?.Trim().ToLower();

                if (subChoix == "a") await PublishMessageAsync("crm", "ContratValid", JsonSerializer.Serialize(new RMQEnveloppe("ContratValid", "C1", "Actif", "Demande simulée")));
                if (subChoix == "b") await PublishMessageAsync("crm", "CertificatQualite", JsonSerializer.Serialize(new RMQEnveloppe("CertificatQualite", "ERP_Virtuel", "Actif", "Certificat simulé")));
                if (subChoix == "c") await PublishMessageAsync("crm", "Paiement", JsonSerializer.Serialize(new RMQEnveloppe("Paiement", "C1", "Actif", "Paiement simulé")));
                if (subChoix == "d") await PublishMessageAsync("crm", "ContratValid", JsonSerializer.Serialize(new RMQEnveloppe("ContratValid", "C2", "Actif", "Demande d'un client sans crédit")));
            }

            await Task.Delay(500); // Petite pause pour la fluidité d'affichage console
        }
    }

    /// <summary>
    /// Traitement asynchrone de chaque message reçu sur l'une des files écoutées.
    /// Implémente l'intelligence d'affaires et l'analyse du workflow (Saga).
    /// </summary>
    private static async Task ProcessIncomingMessageAsync(string queueName, BasicDeliverEventArgs ea, IChannel channel)
    {
        try
        {
            // Désérialisation via le modèle standard RMQEnveloppe
            var msg = RMQEnveloppe.Deserialise(ea.Body.ToArray());

            // Archivage dans les logs locaux (BE*.log)
            Logger.Log($"Reception_{queueName}", msg.MessageName);

            // Scénario : Le CRM reçoit un message (Destination finale ou étape de Saga)
            if (queueName.StartsWith("crm"))
            {
                WriteColor($"\n  [RÉCEPTION CRM] Événement reçu : '{msg.MessageName}' émis par [{msg.Sender}]", ConsoleColor.Cyan);
                WriteColor($"  >> CHARGE UTILE : {msg.MessageText}", ConsoleColor.DarkCyan);

                // --- AUDIT TRAIL DISTANT / SUPERVISION CENTRALISÉE ---
                await SendRemoteLogAsync($"Message '{msg.MessageName}' reçu de {msg.Sender}. Contenu: {msg.MessageText}");

                // --- LOGIQUE D'ORCHESTRATION DE LA SAGA INDUSTRIELLE ---
                if (msg.MessageName.Contains("Contrat", StringComparison.OrdinalIgnoreCase) || msg.MessageName == "850")
                {
                    WriteColor($"  >> [RÈGLE D'AFFAIRES] Le CRM interroge la base de données pour le client : {msg.Sender}...", ConsoleColor.DarkGray);
                    
                    var contrat = _repo.GetContratByClient(msg.Sender);

                    if (contrat == null || !contrat.EstValide)
                    {
                        string raison = contrat == null ? "Client inconnu" : "Crédit insuffisant ou dates expirées";
                        WriteColor($"  >> [RÉSULTAT SAGA] REJET : Le contrat est invalide ({raison}) !", ConsoleColor.Red);
                        WriteColor($"  => [CONSEIL] Refusez l'ordre. Tapez '4', envoyez vers 'edi', nom 'ContratRefuse'.", ConsoleColor.Red);
                    }
                    else
                    {
                        WriteColor($"  >> [RÉSULTAT SAGA] SUCCÈS : Contrat valide jusqu'au {contrat.DateFin:dd/MM/yyyy}. Solde OK.", ConsoleColor.Green);
                        WriteColor($"  => [CONSEIL] Vous pouvez lancer l'Usine. Tapez la touche '1' pour envoyer 'Ceduler' à l'ERP.", ConsoleColor.Green);
                    }
                }
                else if (msg.MessageName.Contains("Paiement", StringComparison.OrdinalIgnoreCase))
                {
                    _repo.EnregistrerTransaction(msg.Sender, 500m, "Paiement");
                    var solde = _repo.GetSoldeClient(msg.Sender);
                    WriteColor($"  >> [FINANCE] Paiement de 500$ reçu de {msg.Sender}. Nouveau solde : {solde}$", ConsoleColor.Green);
                }
                else if (msg.MessageName.Contains("Certificat", StringComparison.OrdinalIgnoreCase) || msg.MessageName == "855")
                {
                    WriteColor($"  >> [RÉSULTAT SAGA] L'Usine a fini le travail. La pièce est conforme.", ConsoleColor.Green);
                    WriteColor($"  => [CONSEIL] Tapez '2' pour renvoyer le certificat, ou '3' pour envoyer la facture à l'EDI.", ConsoleColor.Green);
                }
                else
                {
                    WriteColor($"  => La parole est à vous. Utilisez le menu ci-dessus pour ordonner une réponse !", ConsoleColor.Cyan);
                }

                Console.Write("\nVotre Choix : "); // Répète l'invite pour l'UI
            }
            // Scénario : Mouchards réseaux (espionnage des files locales EDI/ERP)
            // Permet de prouver que le message a bien quitté le CRM vers sa destination.
            else if (queueName.StartsWith("edi") || queueName.StartsWith("erp"))
            {
                WriteColor($"\n  [MOUCHARD RÉSEAU] La file distante '{queueName}' a bien accusé réception du message '{msg.MessageName}' !", ConsoleColor.Magenta);
                WriteColor($"  >> CONTENU TRANSMIS : {msg.MessageText}", ConsoleColor.DarkMagenta);
                Console.Write("Votre Choix : ");
            }

            // Accusé de réception (Acknowledge) pour RabbitMQ
            await channel.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            WriteColor($"[ERREUR DÉCODAGE sur '{queueName}'] {ex.Message}", ConsoleColor.Red);
            // Rejet du message en cas d'erreur de décodage logicielle
            await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
        }
    }

    /// <summary>
    /// Vérifie si le port RabbitMQ est ouvert avant de tenter une connexion lourde.
    /// </summary>
    private static bool CheckRabbitMqAvailibility()
    {
        Console.WriteLine("Tentative de connexion à RabbitMQ...");
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            var result = client.BeginConnect(RmqHost, RmqPort, null, null);
            if (result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2)) && client.Connected)
            {
                WriteColor($"[OK] Connexion réussie à {RmqHost} !", ConsoleColor.Green);
                return true;
            }
        }
        catch { }

        WriteColor($"[ÉCHEC] Impossible de joindre RabbitMQ sur {RmqHost}.", ConsoleColor.Red); return false;
    }

    /// <summary>
    /// Initialise la connexion et le canal de publication par défaut.
    /// </summary>
    private static async Task InitializeRabbitMqAsync()
    {
        var factory = new ConnectionFactory { HostName = RmqHost, Port = RmqPort, UserName = RmqUser, Password = RmqPass, RequestedHeartbeat = TimeSpan.FromSeconds(60) };
        _connection = await factory.CreateConnectionAsync();
        _publishChannel = await _connection.CreateChannelAsync();
    }

    /// <summary>
    /// Démarre l'écoute sur un ensemble de files.
    /// </summary>
    private static async Task StartConsumersAsync(string[] queuesToListen)
    {
        foreach (var queue in queuesToListen) await EnsureQueueAndSubscribeAsync(queue);
    }

    /// <summary>
    /// S'assure de l'existence d'une file et y attache un consommateur d'événements asynchrone.
    /// </summary>
    private static async Task EnsureQueueAndSubscribeAsync(string queueName)
    {
        if (_connection == null) return;
        var channel = await _connection.CreateChannelAsync();
        try
        {
            // Tentative en mode non-durable (commun pour les démos)
            await channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false);
        }
        catch
        {
            // Fallback si la file existe déjà en mode durable
            channel = await _connection.CreateChannelAsync();
            await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);
        }

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) => await ProcessIncomingMessageAsync(queueName, ea, channel);
        await channel.BasicConsumeAsync(queueName, false, "", false, false, null, consumer);
    }

    /// <summary>
    /// Publie un message JSON (encodé en UTF-8) vers la file spécifiée.
    /// </summary>
    private static async Task PublishMessageAsync(string queueName, string messageName, string jsonPayload)
    {
        if (_publishChannel == null) return;
        var body = Encoding.UTF8.GetBytes(jsonPayload);
        await _publishChannel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, mandatory: true, basicProperties: new BasicProperties(), body: body);
        WriteColor($"\n  [ENVOI PAR LE CRM] Le message '{messageName}' est expédié vers la file '{queueName}' !", ConsoleColor.Yellow);
    }

    /// <summary>
    /// Envoie un rapport d'audit à distance vers la file 'logs' pour traçabilité et monitoring distribué.
    /// </summary>
    private static async Task SendRemoteLogAsync(string detail)
    {
        var logEntry = new RMQEnveloppe("LOG_RECEPTION", ServiceIdentifier, "Log", detail);
        var json = JsonSerializer.Serialize(logEntry);
        await PublishMessageAsync(LogQueue, "LOG_RECEPTION", json);
        WriteColor($"  [AUDIT DISTANT] Télémétrie d'audit expédiée vers la file centralisée '{LogQueue}'.", ConsoleColor.DarkYellow);
    }

    /// <summary>
    /// Utilitaire pour écrire dans la console avec une couleur spécifique.
    /// </summary>
    private static void WriteColor(string text, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }
}

/// <summary>
/// Utilitaire de journalisation métier.
/// Enregistre chaque mouvement de message dans un fichier journal daté dans le dossier /logs.
/// </summary>
static class Logger
{
    public static void Log(string action, string nomMessage)
    {
        Directory.CreateDirectory("logs");
        File.AppendAllText(Path.Combine("logs", $"BE{DateTime.Now:yyyyMMdd}.log"), $"{DateTime.Now:yyyy-MM-dd HH'h'mm}::{action}::{nomMessage}()\n");
    }
}
