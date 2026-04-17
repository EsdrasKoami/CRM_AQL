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
                string[] queuesToListen = { "crm", "crm-commandes", "crm-command", "edi", "erp", "edi-reponses" };
                
                foreach (var queue in queuesToListen)
                {
                    await AssureQueueAndSubscribeAsync(queue, _connection);
                }

                Console.WriteLine("\n=============================================");
                Console.WriteLine("           CLI INTERACTIF CRM AQL            ");
                Console.WriteLine("=============================================");

                while (true)
                {
                    Console.WriteLine("\n[MENU] Choisissez la destination de votre message d'essai :");
                    Console.WriteLine("  1. EDI (file: edi)");
                    Console.WriteLine("  2. ERP (file: erp)");
                    Console.WriteLine("  3. CRM (file: crm)");
                    Console.WriteLine("  4. Autre (spécifier une file...)");
                    Console.WriteLine("  0. Quitter l'application");
                    Console.Write("Votre choix: ");
                    
                    var choix = Console.ReadLine();
                    if (choix == "0") break;

                    string targetQueue = choix switch
                    {
                        "1" => "edi",
                        "2" => "erp",
                        "3" => "crm",
                        "4" => "",
                        _ => null
                    };

                    if (targetQueue == null)
                    {
                        Console.WriteLine("Choix invalide. Veuillez réessayer.");
                        continue;
                    }

                    if (choix == "4")
                    {
                        Console.Write("Entrez le nom exact de la file cible: ");
                        var q = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(q)) continue;
                        targetQueue = q;
                    }

                    if (!string.IsNullOrWhiteSpace(targetQueue))
                    {
                        Console.Write("Nom du message (ex: TestMessage ou ContratValid) : ");
                        var msgName = Console.ReadLine();
                        if (string.IsNullOrWhiteSpace(msgName)) msgName = "TestMessage";
                        
                        Console.Write("Contenu texte du message d'essai : ");
                        var msgText = Console.ReadLine() ?? "";

                        var envelope = new RMQEnveloppe(msgName, "ConsoleTest", "Actif", msgText);
                        string json = JsonSerializer.Serialize(envelope);
                        
                        await SendAsync(targetQueue, msgName, json);
                        Logger.Log("EnvoiConsole", $"{targetQueue}_{msgName}");
                    }
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
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[MQ DÉCODÉ sur '{qName}'] | Type: {msg.MessageName} | Texte: {msg.MessageText}");
                Console.ResetColor();

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
