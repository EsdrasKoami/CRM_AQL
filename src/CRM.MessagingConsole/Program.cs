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
                _channel = await _connection.CreateChannelAsync();

                // Déclarer les files nécessaires
                await _channel.QueueDeclareAsync(queue: QueueReponse, durable: true, exclusive: false, autoDelete: false);
                try { await _channel.QueueDeclareAsync(queue: QueueEcoute, durable: false, exclusive: false, autoDelete: false); }
                catch { /* File déjà existante sur le serveur du prof */ }

                Console.WriteLine($"[NET] Connecté. En écoute sur : {QueueEcoute}");
                Console.WriteLine("[NET] En attente des messages du prof...\n");

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    var msg = RMQEnveloppe.Deserialise(ea.Body.ToArray());

                    Logger.Log("Reception", msg.MessageName);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"\n[MQ] REÇU : {msg.MessageName}");
                    Console.WriteLine($"[MQ] Texte : {msg.MessageText}");
                    Console.ResetColor();

                    try
                    {
                        switch (msg.MessageName)
                        {
                            case "TestMessage":
                                Console.WriteLine("[MQ] Ping intercepté. Envoi RetourMessage...");
                                var repPing = new RMQEnveloppe("RetourMessage", "CRM", "Actif", "");
                                await SendAsync(QueueReponse, "RetourMessage", JsonSerializer.Serialize(repPing));
                                Logger.Log("Envoie", "RetourMessage");
                                break;

                            case "ContratValid":
                                string noClient = msg.MessageText.Split('|')[0].Split(':')[1];
                                var repCmd = new RMQEnveloppe("ReponseCommande", "CRM", "Approuvé", "");
                                await SendAsync(QueueReponse, "ReponseCommande", JsonSerializer.Serialize(repCmd));
                                Logger.Log("Envoie", "ReponseCommande");
                                Console.WriteLine($"[MQ] Réponse envoyée -> Client: {noClient} | Statut: Approuvé");
                                break;

                            default:
                                Console.WriteLine($"[MQ] Message reçu : {msg.MessageName} — aucun traitement défini.");
                                break;
                        }

                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERREUR TRAITEMENT] {ex.Message}");
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
                    }
                };

                await _channel.BasicConsumeAsync(QueueEcoute, false, "", false, false, null, consumer);

                // Attendre indéfiniment (Ctrl+C pour quitter)
                Console.WriteLine("[NET] Appuyez sur Ctrl+C pour arrêter.");
                await Task.Delay(Timeout.Infinite);
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

    static async Task SendAsync(string queueName, string messageName, string jsonPayload)
    {
        if (_channel == null) return;
        var body = Encoding.UTF8.GetBytes(jsonPayload);
        await _channel.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, mandatory: true, basicProperties: new BasicProperties(), body: body);
        Console.WriteLine($"[NET] Réponse envoyée -> {queueName} ({messageName})");
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
