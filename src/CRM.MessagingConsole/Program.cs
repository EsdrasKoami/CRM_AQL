using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RMQHelperDLL;
using RabbitMQ.Client.Events;
using CRM.DataAccess.Context;

namespace CRM.MessagingConsole;

class Program
{
    const string HostName = "172.16.88.118";
    const string QueueCrm = "crm-commandes";

    static async Task Main()
    {
        Console.WriteLine("=== MODULE CRM - WORKER SERVICE (DEV) ===");
        RMQConnectionHelper? rmq = null;

        try
        {
            rmq = new RMQConnectionHelper($"amqp://guest:guest@{HostName}:5672/", QueueCrm);
            await rmq.Connect();
            Console.WriteLine($"[NET] Connecté. En écoute sur : {QueueCrm}");

            var consumer = new AsyncEventingBasicConsumer(rmq.CurrentChannel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var msg = RMQEnveloppe.Deserialise(ea.Body.ToArray());
                Logger.Log("Reception", msg.MessageName);

                try
                {
                    switch (msg.MessageName)
                    {
                        case "TestMessage":
                            Console.WriteLine("[MQ] Ping intercepté. Envoi RetourMessage...");
                            var repPing = new RMQEnveloppe("RetourMessage", "CRM", "Actif", "");
                            await rmq.SendAsync("edi-reponses", "RetourMessage", repPing.Serialize());
                            Logger.Log("Envoie", "RetourMessage");
                            break;

                        case "Commande":
                            Console.WriteLine($"[MQ] Commande EDI reçue : {msg.MessageText}");
                            // Extraction basique (format attendu : Client:C000001|Produit:...)
                            string noClient = msg.MessageText.Split('|')[0].Split(':')[1];
                            
                            // Connexion SQL JIT
                            using (var context = new CrmDbContext())
                            {
                                bool clientExiste = context.Clients.Any(c => c.NoClient == noClient);
                                string statut = clientExiste ? "Approuvé" : "Refusé";
                                
                                var repCmd = new RMQEnveloppe("ReponseCommande", "CRM", statut, "");
                                await rmq.SendAsync("edi-reponses", "ReponseCommande", repCmd.Serialize());
                                Logger.Log("Envoie", "ReponseCommande");
                                Console.WriteLine($"[DB] Client {noClient} vérifié. Statut: {statut}");
                            }
                            break;

                        default:
                            Console.WriteLine($"[MQ] Ignoré : {msg.MessageName}");
                            break;
                    }
                    
                    // MANUAL ACK - Seulement après le succès métier et du log
                    await rmq.CurrentChannel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERREUR TRAITEMENT] {ex.Message}");
                    // Remise en queue optionnelle ou enregistrement erreur sans l'acquitter.
                    await rmq.CurrentChannel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
                }
            };
            // AUTOACK = FALSE
            await rmq.CurrentChannel.BasicConsumeAsync(QueueCrm, false, "", false, false, null, consumer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERREUR] Réseau inaccessible. {ex.Message}");
        }

        // SIMULATEUR D'INJECTION
        while (true)
        {
            Console.WriteLine("\n[SIMULATEUR] 'test' (Ping du prof) | 'edi' (Fausse Commande) | 'quit'");
            var choix = Console.ReadLine()?.Trim().ToLower();
            if (choix == "quit") break;
            
            if (choix == "test" && rmq?.CurrentChannel != null)
            {
                var autoTest = new RMQEnveloppe("TestMessage", "CRM-Local", "Auto-Ping", "");
                await rmq.SendAsync(QueueCrm, "TestMessage", autoTest.Serialize());
            }
            if (choix == "edi" && rmq?.CurrentChannel != null)
            {
                var fausseCmd = new RMQEnveloppe("Commande", "EDI-Simul", "Client:C000001|Montant:500", "");
                await rmq.SendAsync(QueueCrm, "Commande", fausseCmd.Serialize());
            }
        }
        if (rmq?.CurrentChannel != null) rmq.CurrentChannel.Dispose();
    }
}

static class Logger
{
    public static void Log(string action, string nomMessage)
    {
        Directory.CreateDirectory("logs");
        string cheminLog = Path.Combine("logs", $"BE{DateTime.Now:yyyyMMdd}.log");
        string ligne = $"{DateTime.Now:yyyy-MM-dd HH'h'mm}::{action}::{nomMessage}()";
        File.AppendAllText(cheminLog, ligne + Environment.NewLine);
    }
}
