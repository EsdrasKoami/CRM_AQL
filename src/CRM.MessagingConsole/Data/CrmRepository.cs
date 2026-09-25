using System;
using System.Collections.Generic;
using System.Linq;

namespace CRM.MessagingConsole.Data;

/// <summary>
/// Modèle représentant un contrat client JIT (Just In Time).
/// Répond à l'exigence fonctionnelle "Étape 1 - Le contrat".
/// </summary>
public class Contrat
{
    public string ClientId { get; set; } = string.Empty;
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }
    public decimal MontantMaxCredit { get; set; }
    public decimal SoldeActuel { get; set; }
    public List<string> ProduitsAutorises { get; set; } = new();

    public bool EstValide => DateTime.Now >= DateDebut && DateTime.Now <= DateFin && SoldeActuel < MontantMaxCredit;
}

/// <summary>
/// Modèle représentant une transaction financière.
/// Répond à l'exigence fonctionnelle "Étape 6 - Facturation".
/// </summary>
public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ClientId { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = "Facture"; // Facture ou Paiement
}

/// <summary>
/// Simulation de la base de données (Mock Repository).
/// Preuve de l'exigence "DB envisagée" pour le CRM.
/// </summary>
public class CrmRepository
{
    private readonly List<Contrat> _contrats = new();
    private readonly List<Transaction> _transactions = new();

    public CrmRepository()
    {
        // Données de démonstration (Seed Data)
        _contrats.Add(new Contrat 
        { 
            ClientId = "C1", 
            DateDebut = DateTime.Now.AddDays(-10), 
            DateFin = DateTime.Now.AddDays(30),
            MontantMaxCredit = 5000m,
            SoldeActuel = 1200m,
            ProduitsAutorises = new List<string> { "Piece_A", "Piece_B" }
        });

        _contrats.Add(new Contrat 
        { 
            ClientId = "C2", 
            DateDebut = DateTime.Now.AddDays(-10), 
            DateFin = DateTime.Now.AddDays(30),
            MontantMaxCredit = 1000m,
            SoldeActuel = 1500m, // Crédit dépassé !
            ProduitsAutorises = new List<string> { "Piece_A" }
        });
    }

    public Contrat? GetContratByClient(string clientId)
    {
        return _contrats.FirstOrDefault(c => c.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase));
    }

    public void EnregistrerTransaction(string clientId, decimal montant, string type)
    {
        _transactions.Add(new Transaction { ClientId = clientId, Montant = montant, Date = DateTime.Now, Type = type });
    }

    public decimal GetSoldeClient(string clientId)
    {
        var factures = _transactions.Where(t => t.ClientId == clientId && t.Type == "Facture").Sum(t => t.Montant);
        var paiements = _transactions.Where(t => t.ClientId == clientId && t.Type == "Paiement").Sum(t => t.Montant);
        return factures - paiements;
    }
}
