using CRM.DataAccess.Context;
using CRM.Domain.Entities;
using CRM.Domain.Interfaces.Repositories;
using CRM.Domain.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.DataAccess.Repositories;

/// <summary>
/// Utilise Dapper pour les procédures stockées (performances) et EF Core pour les lectures.
/// Toutes les transactions BD passent exclusivement par ce repository.
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly CrmDbContext _db;
    private readonly string _connectionString;

    public TransactionRepository(CrmDbContext db)
    {
        _db = db;
        _connectionString = db.Database.GetConnectionString()
            ?? throw new InvalidOperationException("ConnectionString manquante.");
    }

    public async Task<Transaction?> GetByIdAsync(int noTransaction, CancellationToken ct = default)
        => await _db.Transactions.AsNoTracking()
               .FirstOrDefaultAsync(t => t.NoTransaction == noTransaction, ct);

    public async Task<IEnumerable<Transaction>> GetByContratAsync(int noContrat, CancellationToken ct = default)
        => await _db.Transactions.AsNoTracking()
               .Where(t => t.NoContrat == noContrat)
               .OrderByDescending(t => t.DateTransaction)
               .ToListAsync(ct);

    public async Task<IEnumerable<SoldeClientDto>> GetSoldeClientAsync(string noClient, CancellationToken ct = default)
    {
        using var conn = new SqlConnection(_connectionString);
        // Utilisation de Dapper pour appeler la procédure stockée (Performance Maximale)
        return await conn.QueryAsync<SoldeClientDto>(
            "dbo.usp_GetSoldeClient",
            new { NoClient = noClient },
            commandType: System.Data.CommandType.StoredProcedure);
    }


    public async Task<int> CreerFactureAsync(CreerFactureParams p, CancellationToken ct = default)
    {
        var item = await _db.Set<ItemContrat>()
            .FirstOrDefaultAsync(i => i.NoContrat == p.NoContrat && i.NoProduit == p.NoProduit, ct);
            
        if (item == null) throw new InvalidOperationException("Produit non trouvé au contrat.");

        var t = new Transaction
        {
            NoContrat = p.NoContrat,
            TypeTransaction = CRM.Domain.Enums.TypeTransaction.Facture,
            Montant = item.PrixUnitaire * p.Quantite,
            Reference = p.Reference,
            NoProduit = p.NoProduit,
            Quantite = p.Quantite,
            NoUtilisateur = p.NoUtilisateur,
            Commentaire = p.Commentaire,
            DateTransaction = DateTime.UtcNow
        };
        _db.Transactions.Add(t);
        await _db.SaveChangesAsync(ct);
        return t.NoTransaction;
    }

    public async Task<int> EnregistrerPaiementAsync(EnregistrerPaiementParams p, CancellationToken ct = default)
    {
        var t = new Transaction
        {
            NoContrat = p.NoContrat,
            TypeTransaction = CRM.Domain.Enums.TypeTransaction.Paiement,
            Montant = p.Montant,
            Reference = p.Reference,
            NoUtilisateur = p.NoUtilisateur,
            Commentaire = p.Commentaire,
            DateTransaction = DateTime.UtcNow
        };
        _db.Transactions.Add(t);
        await _db.SaveChangesAsync(ct);
        return t.NoTransaction;
    }
}
