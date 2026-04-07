using CRM.Domain.Entities;

namespace CRM.Domain.Interfaces.Repositories;

public interface IItemContratRepository
{
    Task<ItemContrat?> GetAsync(int noContrat, string noProduit, CancellationToken ct = default);
    Task<IEnumerable<ItemContrat>> GetByContratAsync(int noContrat, CancellationToken ct = default);
    Task AddAsync(ItemContrat item, CancellationToken ct = default);
    Task UpdateAsync(ItemContrat item, CancellationToken ct = default);
}
