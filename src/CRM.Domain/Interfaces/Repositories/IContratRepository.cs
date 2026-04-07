using CRM.Domain.Entities;

namespace CRM.Domain.Interfaces.Repositories;

public interface IContratRepository
{
    Task<Contrat?> GetByIdAsync(int noContrat, CancellationToken ct = default);
    Task<Contrat?> GetContratActifAsync(string noClient, DateTime asOf, CancellationToken ct = default);
    Task<IEnumerable<Contrat>> GetByClientAsync(string noClient, CancellationToken ct = default);
    Task AddAsync(Contrat contrat, CancellationToken ct = default);
    Task UpdateAsync(Contrat contrat, CancellationToken ct = default);
}
