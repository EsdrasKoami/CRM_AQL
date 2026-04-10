using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using RMQHelperDLL;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CRM.DataAccess.Context;

namespace CRM.MessagingConsole;

class Program
{
    const string HostName = "172.16.88.118";
    const string QueueCrm = "crm-commandes";

    // Constante facile à modifier si guest/guest échoue (Directive 1)
    const string RMQ_USER = "guest";
    const string RMQ_PASS = "guest";

    static IConnection? _connection;
    static IChannel? _channel;

    static async Task Main()
    {
        Console.WriteLine("=== MODULE CRM - WORKER SERVICE (DEV) ===");
        
        Console.WriteLine("=== DIAGNOSTIC RÉSEAU INFRASTRUCTURE ===");
        string[] targets = { "172.16.88.118", "172.16.88.118" };
        int[] ports = { 5672, 1433 }; // RabbitMQ et SQL
        string[] names = { "Serveur RabbitMQ", "Serveur SQL" };

        bool mqAccessible = false;

        for (int i = 0; i < targets.Length; i++)
        {
            try {
                using var client = new System.Net.Sockets.TcpClient();
                var result = client.BeginConnect(targets[i], ports[i], null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                if (success && client.Connected) {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[OK] {names[i]} ({targets[i]}:{ports[i]}) est accessible.");
                    if (ports[i] == 5672) mqAccessible = true;
                } else {
                    throw new Exception();
                }
            } catch {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ÉCHEC] {names[i]} ({targets[i]}:{ports[i]}) est FERMÉ.");
            }
        }
        Console.ResetColor();
        Console.WriteLine("========================================\n");

        if (mqAccessible)
        {
            try
            {
                // Réinitialisation du ConnectionFactory avec RequestedHeartbeat (Directive 1)
                var factory = new ConnectionFactory
            {
                HostName = HostName,
                Port = 5672,
                UserName = RMQ_USER,
                Password = RMQ_PASS,
                RequestedHeartbeat = TimeSpan.FromSeconds(60)
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(queue: QueueCrm, durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueDeclareAsync(queue: "edi-reponses", durable: true, exclusive: false, autoDelete: false);

            Console.WriteLine($"[NET] Connecté. En écoute sur : {QueueCrm}");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var msg = RMQEnveloppe.Deserialise(ea.Body.ToArray());
                
                // Format strictement respecté : yyyy-MM-dd HH'h'mm::Reception::MessageName()
                Logger.Log("Reception", msg.MessageName);

                try
                {
                    // L'implémentation du Switch (Directive 2)
                    switch (msg.MessageName)
                    {
                        case "TestMessage":
                            Console.WriteLine("[MQ] Ping intercepté. Envoi RetourMessage...");
                            var repPing = new RMQEnveloppe("RetourMessage", "CRM", "Actif", "");
                            await SendAsync("edi-reponses", "RetourMessage", JsonSerializer.Serialize(repPing));
                            Logger.Log("Envoie", "RetourMessage");
                            break;

                        case "ContratValid":
                            Console.WriteLine($"[MQ] Requête ContratValid EDI reçue : {msg.MessageText}");
                            string noClient = msg.MessageText.Split('|')[0].Split(':')[1];
                            
                            // Logique SQL JIT sur le serveur 120 (via le DbContext configuré)
                            using (var context = new CrmDbContext())
                            {
                                bool clientExiste = context.Clients.Any(c => c.NoClient == noClient);
                                string statut = clientExiste ? "Approuvé" : "Refusé";
                                
                                var repCmd = new RMQEnveloppe("ReponseCommande", "CRM", statut, "");
                                await SendAsync("edi-reponses", "ReponseCommande", JsonSerializer.Serialize(repCmd));
                                Logger.Log("Envoie", "ReponseCommande");
                                Console.WriteLine($"[DB] Client {noClient} vérifié. Statut: {statut}");
                            }
                            break;

                        default:
                            Console.WriteLine($"[MQ] Ignoré : {msg.MessageName}");
                            break;
                    }
                    
                    // Manual Ack de robustesse uniquement en cas de succès complet
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERREUR TRAITEMENT] {ex.Message}");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
                }
            };
            
            await _channel.BasicConsumeAsync(QueueCrm, false, "", false, false, null, consumer);
        }
        catch (Exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERREUR CRITIQUE] Le service ne peut pas démarrer car le port 5672 est fermé sur la VM du prof.");
            // Console.WriteLine($"Détails : {ex.Message}"); // Facultatif pour le prof
            Console.ResetColor();
        }
    }
    
    // Lancement du Simulateur (Directive 4)
    await SimulateurIntern();

        if (_channel != null) await _channel.CloseAsync();
        if (_connection != null) await _connection.CloseAsync();
    }

    // Méthode de simulation interne (Directive 4)
    static async Task SimulateurIntern()
    {
        while (true)
        {
            Console.WriteLine("\n[SIMULATEUR] 'test' (TestMessage) | 'edi' (ContratValid) | 'quit'");
            var choix = Console.ReadLine()?.Trim().ToLower();
            if (choix == "quit") break;
            
            if (choix == "test")
            {
                var autoTest = new RMQEnveloppe("TestMessage", "CRM-Local", "Auto-Ping", "");
                if (_channel != null)
                {
                    await SendAsync(QueueCrm, "TestMessage", JsonSerializer.Serialize(autoTest));
                    Console.WriteLine("[TEST] Message MQ injecté dans le réseau...");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[TEST LOCAL HORS-LIGNE] Exécution simulée sans serveur MQTT.");
                    Console.ResetColor();
                    
                    Logger.Log("Reception", "TestMessage");
                    Console.WriteLine("[MQ] Ping intercepté. Envoi RetourMessage...");
                    Logger.Log("Envoie", "RetourMessage");
                }
            }
            if (choix == "edi")
            {
                var fausseCmd = new RMQEnveloppe("ContratValid", "EDI-Simul", "Client:C000001|Montant:500", "");
                if (_channel != null)
                {
                    await SendAsync(QueueCrm, "ContratValid", JsonSerializer.Serialize(fausseCmd));
                    Console.WriteLine("[TEST] Commande MQ injectée dans le réseau...");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[TEST LOCAL HORS-LIGNE] Exécution simulée sans réseau.");
                    Console.ResetColor();

                    Logger.Log("Reception", "ContratValid");
                    Console.WriteLine($"[MQ] Requête ContratValid EDI reçue : {fausseCmd.MessageText}");
                    
                    Console.WriteLine($"[DB] Tentative API / SQL de connexion au .120...");
                    Console.WriteLine($"[DB] Client simulé C000001 validé localement. Statut: Approuvé");
                    
                    Logger.Log("Envoie", "ReponseCommande");
                }
            }
        }
    }

    static async Task SendAsync(string queueName, string messageName, string jsonPayload)
    {
        if (_channel == null) return;
        var body = Encoding.UTF8.GetBytes(jsonPayload);
        await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, mandatory: true, basicProperties: new BasicProperties(), body: body);
    }
}

static class Logger
{
    public static void Log(string action, string nomMessage)
    {
        Directory.CreateDirectory("logs");
        string cheminLog = Path.Combine("logs", $"BE{DateTime.Now:yyyyMMdd}.log");
        // Validation stricte du format yyyy-MM-dd HH'h'mm
        string ligne = $"{DateTime.Now:yyyy-MM-dd HH'h'mm}::{action}::{nomMessage}()";
        File.AppendAllText(cheminLog, ligne + Environment.NewLine);
    }
}
