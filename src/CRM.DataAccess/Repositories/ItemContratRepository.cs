using CRM.DataAccess.Context;
using CRM.Domain.Entities;
using CRM.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.DataAccess.Repositories;

public class ItemContratRepository : IItemContratRepository
{
    private readonly CrmDbContext _db;

    public ItemContratRepository(CrmDbContext db) => _db = db;

    public async Task<ItemContrat?> GetAsync(int noContrat, string noProduit, CancellationToken ct = default)
        => await _db.ItemsContrat.AsNoTracking()
               .FirstOrDefaultAsync(i => i.NoContrat == noContrat && i.NoProduit == noProduit, ct);

    public async Task<IEnumerable<ItemContrat>> GetByContratAsync(int noContrat, CancellationToken ct = default)
        => await _db.ItemsContrat.AsNoTracking()
               .Where(i => i.NoContrat == noContrat)
               .ToListAsync(ct);

    public async Task AddAsync(ItemContrat item, CancellationToken ct = default)
    {
        await _db.ItemsContrat.AddAsync(item, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ItemContrat item, CancellationToken ct = default)
    {
        _db.ItemsContrat.Update(item);
        await _db.SaveChangesAsync(ct);
    }
}
