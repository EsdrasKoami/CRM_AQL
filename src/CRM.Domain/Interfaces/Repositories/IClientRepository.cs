using CRM.Domain.Entities;

namespace CRM.Domain.Interfaces.Repositories;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(string noClient, CancellationToken ct = default);
    Task<IEnumerable<Client>> GetAllActiveAsync(CancellationToken ct = default);
    Task AddAsync(Client client, CancellationToken ct = default);
    Task UpdateAsync(Client client, CancellationToken ct = default);
    Task<bool> ExistsAsync(string noClient, CancellationToken ct = default);
}
