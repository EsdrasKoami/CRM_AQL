using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using RMQHelperDLL;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CRM.MessagingConsole;

class Program
{
    const string HostName = "172.16.88.38"; // IP du serveur RabbitMQ
    const string QueueEcoute = "crm";       // File du prof qu'on écoute
    const string QueueReponse = "edi-reponses"; // File où on envoie les réponses

    const string RMQ_USER = "guest";
    const string RMQ_PASS = "guest";

    static IConnection? _connection;
    static IChannel? _channel;

    static async Task Main()
    {
        Console.WriteLine("=== MODULE CRM - WORKER SERVICE ===");
        Console.WriteLine("=== DIAGNOSTIC RÉSEAU ===");

        bool mqAccessible = false;
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            var result = client.BeginConnect(HostName, 5672, null, null);
            bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
            if (success && client.Connected)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[OK] Serveur RabbitMQ ({HostName}:5672) est accessible.");
                mqAccessible = true;
            }
            else throw new Exception();
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ÉCHEC] Serveur RabbitMQ ({HostName}:5672) est FERMÉ.");
        }
        Console.ResetColor();
        Console.WriteLine("=========================\n");

        if (mqAccessible)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = HostName,
                    Port = 5672,
                    UserName = RMQ_USER,
                    Password = RMQ_PASS,
                    RequestedHeartbeat = TimeSpan.FromSeconds(60)
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync(); // Channel principal pour l'envoi

                Console.WriteLine("[NET] Initialisation des écoutes multi-files...");
                string[] queuesToListen = { "crm", "crm-commandes", "crm-command" };

                foreach (var queue in queuesToListen)
                {
                    await AssureQueueAndSubscribeAsync(queue, _connection);
                }

                Console.WriteLine("\n=============================================");
                Console.WriteLine("       ORCHESTRATEUR SAGA CRM - ACTIF        ");
                Console.WriteLine("=============================================");
                Console.WriteLine(" [MOTEUR AUTO] Écoute RabbitMQ en direct.");
                Console.WriteLine(" [SCÉNARIO] CR1 100% Automatisé (Facturation, Certificats...)");
                Console.WriteLine("=============================================\n");

                while (true)
                {
                    Console.WriteLine("\n[MODE MANUEL] L'Automate gère les processus CR1. Mode forçage direct :");
                    Console.WriteLine("  1. Forcer l'Approbation d'un Contrat (vers edi)");
                    Console.WriteLine("  2. Forcer le Refus d'un Contrat (vers edi)");
                    Console.WriteLine("  3. Forcer l'Autorisation Commande (vers erp)");
                    Console.WriteLine("  4. Forcer le Blocage d'une Commande (vers erp)");
                    Console.WriteLine("  5. Message personnalisé / Libre au Simulateur");
                    Console.WriteLine("  0. Quitter l'application");
                    Console.Write("Votre action manuelle (ou attendez l'automate): ");

                    var choix = Console.ReadLine()?.Trim();
                    if (choix == "0") break;

                    string targetQueue = "";
                    string msgName = "";
                    string statutSelectionne = "";

                    switch (choix)
                    {
                        case "1":
                            targetQueue = "edi";
                            msgName = "ContratValid";
                            statutSelectionne = "Approuvé";
                            break;
                        case "2":
                            targetQueue = "edi";
                            msgName = "ContratInvalide";
                            statutSelectionne = "Refusé";
                            break;
                        case "3":
                            targetQueue = "erp";
                            msgName = "CommandeAutorisee";
                            statutSelectionne = "Approuvé";
                            break;
                        case "4":
                            targetQueue = "erp";
                            msgName = "CommandeBloquee";
                            statutSelectionne = "Refusé";
                            break;
                        case "5":
                            Console.Write("Nom de la file cible (ex: edi, erp, crm): ");
                            targetQueue = Console.ReadLine()?.Trim().ToLower() ?? "";
                            if (string.IsNullOrWhiteSpace(targetQueue)) continue;

                            Console.Write("Nom du message: ");
                            msgName = Console.ReadLine()?.Trim() ?? "TestMessage";
                            break;
                        default:
                            Console.WriteLine("Choix invalide. Veuillez réessayer.");
                            continue;
                    }

                    // Validation côté "Interface" (ne pas envoyer si vide)
                    Console.Write($"Texte ou ID (Statut: {statutSelectionne}) : ");
                    var msgText = Console.ReadLine()?.Trim();

                    if (string.IsNullOrWhiteSpace(msgText))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[ERREUR] Impossible d'envoyer un message vide. Action annulée.");
                        Console.ResetColor();
                        continue;
                    }

                    // On inclut le statut directement dans le texte pour la démo console
                    if (!string.IsNullOrEmpty(statutSelectionne) && choix != "5")
                        msgText = $"[{statutSelectionne}] {msgText}";

                    var envelope = new RMQEnveloppe(msgName, "ConsoleTest", "Actif", msgText);
                    string json = JsonSerializer.Serialize(envelope);

                    await SendAsync(targetQueue, msgName, json);
                    Logger.Log("EnvoiConsole", $"{targetQueue}_{msgName}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERREUR CRITIQUE] {ex.Message}");
                Console.ResetColor();
            }
        }

        if (_channel != null) await _channel.CloseAsync();
        if (_connection != null) await _connection.CloseAsync();
    }

    static async Task AssureQueueAndSubscribeAsync(string qName, IConnection conn)
    {
        var channel = await conn.CreateChannelAsync();
        try
        {
            // Essayer durable=false en premier car le prof utilise ça pour sa file "crm"
            await channel.QueueDeclareAsync(queue: qName, durable: false, exclusive: false, autoDelete: false);
        }
        catch
        {
            // Si RabbitMQ refuse (car la file existe déjà avec durable=true), le channel est fermé.
            // On le ré-ouvre et on essaie durable=true.
            try
            {
                channel = await conn.CreateChannelAsync();
                await channel.QueueDeclareAsync(queue: qName, durable: true, exclusive: false, autoDelete: false);
            }
            catch
            {
                // Ignorer et essayer de consommer quand même
            }
        }

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var rawBody = ea.Body.ToArray();
                var rawText = Encoding.UTF8.GetString(rawBody);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n[MQ MSG BRUT REÇU sur '{qName}'] {rawText}");
                Console.ResetColor();

                var msg = RMQEnveloppe.Deserialise(rawBody);

                Logger.Log($"Reception_{qName}", msg.MessageName);

                // Colorisation dynamique selon le statut (Remplacement de l'UI Graphique)
                if (msg.MessageName.Contains("Invalide") || msg.MessageName.Contains("Bloquee") || msg.MessageText.Contains("Refusé", StringComparison.OrdinalIgnoreCase))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else if (msg.MessageName.Contains("Valid") || msg.MessageName.Contains("Autorisee") || msg.MessageText.Contains("Approuvé", StringComparison.OrdinalIgnoreCase))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }

                Console.WriteLine($"[MQ DÉCODÉ sur '{qName}'] | Type: {msg.MessageName} | Texte: {msg.MessageText}");
                Console.ResetColor();

                // ==========================================
                // MOTEUR AUTOMATIQUE DE SAGA (SCÉNARIO CR1)
                // ==========================================
                if (qName.StartsWith("crm"))
                {
                    // CR1.1 - Réception ContratValid (Commande depuis EDI)
                    if (msg.MessageName.Contains("ContratValid", StringComparison.OrdinalIgnoreCase) || msg.MessageName == "850" || msg.MessageText.Contains("850:", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("\n>> [SAGA] CR1.1 : Message 'ContratValid' reçu. Validation en cours...");
                        Console.WriteLine(">> [SAGA] CR1.1 : Contrat Valable! (Simulé) Envoi de 'ContratValideReponse' à EDI...");
                        string payloadEDI = "NoClient=C123456\nNoCommande=PO00001\nTrackingNo=TR20260401-001\nStatut=En traitement";
                        var repEDI = new RMQEnveloppe("ContratValideReponse", "CRM", "Actif", payloadEDI);
                        await SendAsync("edi", "ContratValideReponse", JsonSerializer.Serialize(repEDI));

                        Console.WriteLine(">> [SAGA] CR1.2 : En parallèle, envoi de 'CommandeConfirmée' (ou demande) à l'ERP...");
                        var reqProd = new RMQEnveloppe("ScheduleProduction", "CRM", "Actif", "NoClient=C123456\nNoCommande=PO00001");
                        await SendAsync("erp", "ScheduleProduction", JsonSerializer.Serialize(reqProd));
                    }

                    // CR1.3 - Certificat reçu de MES/ERP
                    else if (msg.MessageName.Contains("Certificat", StringComparison.OrdinalIgnoreCase) || msg.MessageName == "855")
                    {
                        Console.WriteLine("\n>> [SAGA] CR1.3 : Certificat reçu de MES/ERP. Mise à jour interne : Statut=Produite, Qté=1.");
                        Console.WriteLine(">> [SAGA] CR1.3 : Transfert via 'CertificatQualite' à EDI...");
                        string payloadCertif = "NoClient=C123456\nTrackingNumber=TR20260401-001\nNoCommande=PO00001\nLigneItem=1\nItem=1\nNoSerie=XXXXXX-000001\nDescription=ABCD5x15\nA=100%";
                        var repCertif = new RMQEnveloppe("CertificatQualite", "CRM", "Actif", payloadCertif);
                        await SendAsync("edi", "CertificatQualite", JsonSerializer.Serialize(repCertif));
                    }

                    // CR1.4 - Notification d'expédition du ERP
                    else if (msg.MessageName.Contains("Expedi", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("\n>> [SAGA] CR1.4 : Notification d'expédition reçue. Statut de la commande mis à jour sur 'Expédiée'.");
                        Console.WriteLine(">> [SAGA] CR1.4 : Émission de 'Facture' pour le client EDI...");
                        string payloadFacture = "NoClient=C123456\nTrackingNumber=TR20260401-001\nNoCommande=PO00001\nLigneItem=1\nPrixunitaire=500$\nQty=1\nSousTotal:1=500$\nTotal=500$";
                        var repFacture = new RMQEnveloppe("Facture", "CRM", "Actif", payloadFacture);
                        await SendAsync("edi", "Facture", JsonSerializer.Serialize(repFacture));
                    }

                    // CR1.5 - Client envoie le message Paiement
                    else if (msg.MessageName.Contains("Paiement", StringComparison.OrdinalIgnoreCase) || msg.MessageName == "999" || msg.MessageText.Contains("Paimement", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("\n>> [SAGA] CR1.5 : Message 'Paiement' croisé, reçu du client.");
                        Console.WriteLine(">> [SAGA] CR1.5 : Paiement de la facture validé. Solde Client à 0$. LA SAGA EST TERMINÉE ! 🎉");
                    }
                }
                // ==========================================

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERREUR TRAITEMENT sur '{qName}'] Décryptage impossible: {ex.Message}");
                Console.ResetColor();
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
            }
        };

        await channel.BasicConsumeAsync(qName, false, "", false, false, null, consumer);
        Console.WriteLine($"[NET] En écoute sur la file : {qName}");
    }

    static async Task SendAsync(string queueName, string messageName, string jsonPayload)
    {
        if (_channel == null) return;
        var body = Encoding.UTF8.GetBytes(jsonPayload);
        await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, mandatory: true, basicProperties: new BasicProperties(), body: body);
        Console.WriteLine($"\n[NET] => MESSAGE ENVOYÉ ! File: {queueName} | Message: {messageName}");
        Console.WriteLine("---------------------------------------------");
    }
}

static class Logger
{
    public static void Log(string action, string nomMessage)
    {
        Directory.CreateDirectory("logs");
        string cheminLog = System.IO.Path.Combine("logs", $"BE{DateTime.Now:yyyyMMdd}.log");
        string ligne = $"{DateTime.Now:yyyy-MM-dd HH'h'mm}::{action}::{nomMessage}()";
        File.AppendAllText(cheminLog, ligne + Environment.NewLine);
    }
}
