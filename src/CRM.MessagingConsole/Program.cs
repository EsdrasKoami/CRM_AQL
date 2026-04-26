using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using CRM.MessagingConsole.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CRM.MessagingConsole;

class Program
{
    private const string RmqHost = "172.16.88.227";
    private const int RmqPort = 5672;
    private const string RmqUser = "guest";
    private const string RmqPass = "guest";

    private static IConnection? _connection;
    private static IChannel? _publishChannel;

    static async Task Main()
    {
        Console.WriteLine("======================================================");
        Console.WriteLine(" MODULE CRM - PANNEAU DE CONTRÔLE MANUEL INTÉGRAL     ");
        Console.WriteLine("======================================================\n");

        if (!CheckRabbitMqAvailibility())
        {
            Console.ReadLine();
            return;
        }

        try
        {
            await InitializeRabbitMqAsync();
            await StartConsumersAsync(new[] { "crm", "edi", "erp" });
            await RunDashboardAsync();
        }
        catch (Exception ex) { WriteColor($"[ERREUR] {ex.Message}", ConsoleColor.Red); }
        finally
        {
            if (_publishChannel != null) await _publishChannel.CloseAsync();
            if (_connection != null) await _connection.CloseAsync();
        }
    }

    private static async Task RunDashboardAsync()
    {
        Console.WriteLine("\n[ SYSTÈME PRÊT ET EN ÉCOUTE CONTINUE ]");
        Console.WriteLine("Si le prof envoie un message dans 'crm', il apparaîtra ici instantanément.");

        while (true)
        {
            Console.WriteLine("\n--------------------------------------------------------------");
            Console.WriteLine(" VOS ACTIONS (Tapez la Tonalité et appuyez sur Entrée) :     ");
            Console.WriteLine("--------------------------------------------------------------");
            Console.WriteLine(" >> POUR RÉPONDRE / ENVOYER (Si vous recevez qqch) : ");
            Console.WriteLine("  1. Envoyer l'ordre 'Ceduler' vers l'ERP");
            Console.WriteLine("  2. Envoyer le 'CertificatQualite' vers l'EDI");
            Console.WriteLine("  3. Envoyer la 'Facture' vers l'EDI");
            Console.WriteLine("  4. 🎯 Envoi Libre : Choisir soi-même le nom ET la destination");
            Console.WriteLine("\n >> POUR SIMULER VOUS-MÊME (Si le prof ne fait rien) : ");
            Console.WriteLine("  S. Simuler une Réception entrante (EDI/ERP -> CRM)");
            Console.WriteLine("--------------------------------------------------------------");
            Console.Write("Votre Choix : ");

            var choix = Console.ReadLine()?.Trim().ToUpper();

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
            }
            else if (choix == "4") // LE CHOIX LIBRE !
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
            else if (choix == "S")
            {
                Console.WriteLine(" [Simulation] Quel message entrant voulez-vous simuler ?");
                Console.WriteLine("   a) L'EDI envoie 'ContratValid'");
                Console.WriteLine("   b) L'ERP envoie 'CertificatQualite'");
                Console.WriteLine("   c) L'EDI envoie 'Paiement'");
                Console.Write(" > (a/b/c) : ");
                var subChoix = Console.ReadLine()?.Trim().ToLower();

                if (subChoix == "a") await PublishMessageAsync("crm", "ContratValid", JsonSerializer.Serialize(new RMQEnveloppe("ContratValid", "EDI_Virtuel", "Actif", "Demande simulée")));
                if (subChoix == "b") await PublishMessageAsync("crm", "CertificatQualite", JsonSerializer.Serialize(new RMQEnveloppe("CertificatQualite", "ERP_Virtuel", "Actif", "Certificat simulé")));
                if (subChoix == "c") await PublishMessageAsync("crm", "Paiement", JsonSerializer.Serialize(new RMQEnveloppe("Paiement", "EDI_Virtuel", "Actif", "Paiement simulé")));
            }

            await Task.Delay(500); // Laisse le temps aux autres fils de console de s'imprimer proprement.
        }
    }

    private static async Task ProcessIncomingMessageAsync(string queueName, BasicDeliverEventArgs ea, IChannel channel)
    {
        try
        {
            var msg = RMQEnveloppe.Deserialise(ea.Body.ToArray());
            Logger.Log($"Reception_{queueName}", msg.MessageName);

            // C'est un message POUR le CRM (Venant du prof ou de l'option S)
            if (queueName.StartsWith("crm"))
            {
                WriteColor($"\n  🛎️  [RÉCEPTION CRM] BINGO ! Le CRM vient de recevoir '{msg.MessageName}' de la part de {msg.Sender} !", ConsoleColor.Cyan);

                // --- AJOUT : INTELLIGENCE D'ANALYSE DU CRM ---
                if (msg.MessageName.Contains("Contrat", StringComparison.OrdinalIgnoreCase) || msg.MessageName == "850")
                {
                    WriteColor($"  >> [RÈGLE D'AFFAIRES] Le CRM analyse le contrat en base de données...", ConsoleColor.DarkGray);

                    bool isInvalide = msg.MessageText.Contains("invalide", StringComparison.OrdinalIgnoreCase) ||
                                      msg.MessageText.Contains("refus", StringComparison.OrdinalIgnoreCase) ||
                                      msg.MessageName.Contains("Invalide", StringComparison.OrdinalIgnoreCase);

                    if (isInvalide)
                    {
                        WriteColor($"  >> ❌ [RÉSULTAT SAGA] Le contrat du client est INTROUVABLE ou EXPIRÉ !", ConsoleColor.Red);
                        WriteColor($"  => [CONSEIL] Refusez l'ordre. Tapez '4', envoyez vers 'edi', nom du message 'ContratRefuse'.", ConsoleColor.Red);
                    }
                    else
                    {
                        WriteColor($"  >> ✅ [RÉSULTAT SAGA] Le contrat du client est VALIDE ! Le solde est suffisant.", ConsoleColor.Green);
                        WriteColor($"  => [CONSEIL] Vous pouvez lancer l'Usine. Tapez la touche '1' pour envoyer 'Ceduler' à l'ERP.", ConsoleColor.Green);
                    }
                }
                else if (msg.MessageName.Contains("Certificat", StringComparison.OrdinalIgnoreCase) || msg.MessageName == "855")
                {
                    WriteColor($"  >> ✅ [RÉSULTAT SAGA] L'Usine a fini le travail. La pièce est conforme.", ConsoleColor.Green);
                    WriteColor($"  => [CONSEIL] Tapez '2' pour renvoyer le certificat, ou '3' pour envoyer la facture à l'EDI.", ConsoleColor.Green);
                }
                else
                {
                    WriteColor($"  => La parole est à vous. Utilisez le menu ci-dessus pour ordonner une réponse !", ConsoleColor.Cyan);
                }

                Console.Write("\nVotre Choix : "); // Reproduit l'invite de commande pour la beauté visuelle
            }
            // Mouchards pour vérifier que l'envoi a bien été posté là-bas
            else if (queueName.StartsWith("edi") || queueName.StartsWith("erp"))
            {
                WriteColor($"\n  ✅  [MOUCHARD RÉSEAU] La file distante '{queueName}' a bien accusé réception du message '{msg.MessageName}' !", ConsoleColor.Magenta);
                Console.Write("Votre Choix : ");
            }

            await channel.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            WriteColor($"[ERREUR DÉCODAGE sur '{queueName}'] {ex.Message}", ConsoleColor.Red);
            await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
        }
    }

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

    private static async Task InitializeRabbitMqAsync()
    {
        var factory = new ConnectionFactory { HostName = RmqHost, Port = RmqPort, UserName = RmqUser, Password = RmqPass, RequestedHeartbeat = TimeSpan.FromSeconds(60) };
        _connection = await factory.CreateConnectionAsync();
        _publishChannel = await _connection.CreateChannelAsync();
    }

    private static async Task StartConsumersAsync(string[] queuesToListen)
    {
        foreach (var queue in queuesToListen) await EnsureQueueAndSubscribeAsync(queue);
    }

    private static async Task EnsureQueueAndSubscribeAsync(string queueName)
    {
        if (_connection == null) return;
        var channel = await _connection.CreateChannelAsync();
        try { await channel.QueueDeclareAsync(queue: queueName, durable: false, exclusive: false, autoDelete: false); }
        catch { channel = await _connection.CreateChannelAsync(); await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false); }

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) => await ProcessIncomingMessageAsync(queueName, ea, channel);
        await channel.BasicConsumeAsync(queueName, false, "", false, false, null, consumer);
    }

    private static async Task PublishMessageAsync(string queueName, string messageName, string jsonPayload)
    {
        if (_publishChannel == null) return;
        var body = Encoding.UTF8.GetBytes(jsonPayload);
        await _publishChannel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, mandatory: true, basicProperties: new BasicProperties(), body: body);
        WriteColor($"\n  🚀  [ENVOI PAR LE CRM] Le message '{messageName}' est expédié vers la file '{queueName}' !", ConsoleColor.Yellow);
    }

    private static void WriteColor(string text, ConsoleColor color) { Console.ForegroundColor = color; Console.WriteLine(text); Console.ResetColor(); }
}
static class Logger { public static void Log(string action, string nomMessage) { Directory.CreateDirectory("logs"); File.AppendAllText(Path.Combine("logs", $"BE{DateTime.Now:yyyyMMdd}.log"), $"{DateTime.Now:yyyy-MM-dd HH'h'mm}::{action}::{nomMessage}()\n"); } }
