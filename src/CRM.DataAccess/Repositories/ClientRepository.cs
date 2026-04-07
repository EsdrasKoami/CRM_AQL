using CRM.DataAccess.Context;
using CRM.Domain.Entities;
using CRM.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.DataAccess.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly CrmDbContext _db;

    public ClientRepository(CrmDbContext db) => _db = db;

    public async Task<Client?> GetByIdAsync(string noClient, CancellationToken ct = default)
        => await _db.Clients.AsNoTracking()
               .FirstOrDefaultAsync(c => c.NoClient == noClient, ct);

    public async Task<IEnumerable<Client>> GetAllActiveAsync(CancellationToken ct = default)
        => await _db.Clients.AsNoTracking()
               .Where(c => c.EstActif)
               .OrderBy(c => c.NomEntreprise)
               .ToListAsync(ct);

    public async Task AddAsync(Client client, CancellationToken ct = default)
    {
        await _db.Clients.AddAsync(client, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Client client, CancellationToken ct = default)
    {
        _db.Clients.Update(client);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(string noClient, CancellationToken ct = default)
        => await _db.Clients.AnyAsync(c => c.NoClient == noClient && c.EstActif, ct);
}
