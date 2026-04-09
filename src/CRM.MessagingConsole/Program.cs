using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CRM.MessagingConsole;

class Program
{
    const string HostName = "172.16.88.118";
    
    // Noms des files de destination
    const string QueueCrm = "crm-commandes";
    const string QueueErp = "erp-commandes";
    const string QueueEdr = "edr-commandes";

    static async Task Main(string[] args)
    {
        Console.WriteLine("=======================================");
        Console.WriteLine("    CRM MESSAGING CONSOLE (RabbitMQ)   ");
        Console.WriteLine($"    Serveur : {HostName}");
        Console.WriteLine("=======================================\n");

        while (true)
        {
            Console.WriteLine("Que voulez-vous faire ?");
            Console.WriteLine("[1] ENVOYER un message");
            Console.WriteLine("[2] ÉCOUTER une file (recevoir les messages)");
            Console.WriteLine("[Q] Quitter");
            Console.Write("\nChoix : ");
            
            var choix = Console.ReadLine()?.Trim().ToUpper();

            if (choix == "Q") break;

            if (choix == "1")
            {
                await EnvoyerMessageAsync();
            }
            else if (choix == "2")
            {
                await EcouterFileAsync();
                // Une fois qu'on arrête d'écouter, on revient au menu principal
            }
            else
            {
                Console.WriteLine("Choix invalide.\n");
            }
        }
    }

    static async Task EnvoyerMessageAsync()
    {
        Console.WriteLine("\n--- ENVOI DE MESSAGE ---");
        
        Console.Write("De (votre identité, ex: CRM, ERP, EDR) : ");
        var de = Console.ReadLine() ?? "Inconnu";

        Console.WriteLine("À qui (destinataire) ?");
        Console.WriteLine("  [1] CRM (" + QueueCrm + ")");
        Console.WriteLine("  [2] ERP (" + QueueErp + ")");
        Console.WriteLine("  [3] EDR (" + QueueEdr + ")");
        Console.Write("Destinataire : ");
        
        var choixDest = Console.ReadLine();
        string queueName = choixDest switch
        {
            "1" => QueueCrm,
            "2" => QueueErp,
            "3" => QueueEdr,
            _ => QueueCrm // Défaut
        };

        Console.Write("Contenu du message : ");
        var contenu = Console.ReadLine() ?? "";

        Console.Write("Type de message (ex: ChatMessage, DataMessage, TestMessage) : ");
        var typeMsg = Console.ReadLine() ?? "ChatMessage";

        var message = new
        {
            MessageName = typeMsg,
            Sender = de,
            MessageText = contenu,
            XmlData = string.Empty
        };

        try
        {
            var factory = new ConnectionFactory { HostName = HostName, UserName = "guest", Password = "guest" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            // S'assurer que la file existe avant d'envoyer
            await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var messageJson = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(messageJson);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: true,
                basicProperties: new BasicProperties(),
                body: body);

            Console.WriteLine($"\n[SUCCÈS] Message envoyé à '{queueName}' ! \n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERREUR] Impossible d'envoyer : {ex.Message}\n");
        }
    }

    static async Task EcouterFileAsync()
    {
        Console.WriteLine("\n--- ÉCOUTE DES MESSAGES ---");
        Console.WriteLine("Quelle file voulez-vous écouter ?");
        Console.WriteLine("  [1] CRM (" + QueueCrm + ")");
        Console.WriteLine("  [2] ERP (" + QueueErp + ")");
        Console.WriteLine("  [3] EDR (" + QueueEdr + ")");
        Console.Write("File : ");
        
        var choixFile = Console.ReadLine();
        string queueName = choixFile switch
        {
            "1" => QueueCrm,
            "2" => QueueErp,
            "3" => QueueEdr,
            _ => QueueCrm
        };

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("\nArrêt de l'écoute...");
            e.Cancel = true; // Empêche la fermeture de l'app complète
            cts.Cancel();
        };

        try
        {
            var factory = new ConnectionFactory { HostName = HostName, UserName = "guest", Password = "guest" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);
                
                try
                {
                    // Tente de désérialiser au format ConsoleMessage
                    var msg = JsonSerializer.Deserialize<ConsoleMessage>(messageJson);
                    if (msg != null && !string.IsNullOrEmpty(msg.Contenu))
                    {
                        Console.WriteLine($"\n--- NOUVEAU MESSAGE ({msg.EnvoyeLe:HH:mm:ss}) ---");
                        Console.WriteLine($"De      : {msg.De}");
                        Console.WriteLine($"Contenu : {msg.Contenu}");
                        Console.WriteLine("------------------------------------------");
                    }
                    else 
                    {
                        // Si le json ne correspond pas à ConsoleMessage (ex: IncomingMqMessage du projet principal)
                        AfficherMessageBrut(messageJson);
                    }
                }
                catch
                {
                    // JSON invalide ou différent, on l'affiche tel quel
                    AfficherMessageBrut(messageJson);
                }

                await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);

            Console.WriteLine($"\n[EN ÉCOUTE] sur la file '{queueName}'. (Crtl+C retournera au menu)\n");

            // Attendre jusqu'à l'annulation via Ctrl+C
            try
            {
                await Task.Delay(-1, cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Normal
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERREUR] Impossible d'écouter : {ex.Message}\n");
        }
        
        Console.WriteLine();
    }

    static void AfficherMessageBrut(string contenu)
    {
        Console.WriteLine($"\n--- MESSAGE BRUT REÇU ({DateTime.Now:HH:mm:ss}) ---");
        Console.WriteLine(contenu);
        Console.WriteLine("------------------------------------------");
    }
}
