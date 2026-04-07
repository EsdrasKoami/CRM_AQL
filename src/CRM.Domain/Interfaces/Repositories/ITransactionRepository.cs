using CRM.Domain.Entities;
using CRM.Domain.Models;

namespace CRM.Domain.Interfaces.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(int noTransaction, CancellationToken ct = default);
    Task<IEnumerable<Transaction>> GetByContratAsync(int noContrat, CancellationToken ct = default);

    /// <summary>Calcule le solde temps réel via la procédure stockée usp_GetSoldeClient.</summary>
    Task<IEnumerable<SoldeClientDto>> GetSoldeClientAsync(string noClient, CancellationToken ct = default);

    Task<int> CreerFactureAsync(CreerFactureParams p, CancellationToken ct = default);
    Task<int> EnregistrerPaiementAsync(EnregistrerPaiementParams p, CancellationToken ct = default);
}
