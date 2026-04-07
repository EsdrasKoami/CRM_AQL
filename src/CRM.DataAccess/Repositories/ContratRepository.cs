using CRM.DataAccess.Context;
using CRM.Domain.Entities;
using CRM.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.DataAccess.Repositories;

public class ContratRepository : IContratRepository
{
    private readonly CrmDbContext _db;

    public ContratRepository(CrmDbContext db) => _db = db;

    public async Task<Contrat?> GetByIdAsync(int noContrat, CancellationToken ct = default)
        => await _db.Contrats.AsNoTracking()
               .Include(c => c.Items)
               .FirstOrDefaultAsync(c => c.NoContrat == noContrat, ct);

    /// <summary>
    /// Retourne le contrat actif pour un client à la date donnée.
    /// Un client ne devrait avoir qu'un seul contrat actif à la fois.
    /// </summary>
    public async Task<Contrat?> GetContratActifAsync(string noClient, DateTime asOf, CancellationToken ct = default)
    {
        var date = asOf.Date;
        return await _db.Contrats.AsNoTracking()
               .Include(c => c.Items)
               .Where(c =>
                   c.NoClient == noClient &&
                   c.EstActif &&
                   c.DateDebut.Date <= date &&
                   c.DateFin.Date   >= date)
               .OrderByDescending(c => c.DateDebut)
               .FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<Contrat>> GetByClientAsync(string noClient, CancellationToken ct = default)
        => await _db.Contrats.AsNoTracking()
               .Include(c => c.Items)
               .Where(c => c.NoClient == noClient)
               .OrderByDescending(c => c.DateDebut)
               .ToListAsync(ct);

    public async Task AddAsync(Contrat contrat, CancellationToken ct = default)
    {
        await _db.Contrats.AddAsync(contrat, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Contrat contrat, CancellationToken ct = default)
    {
        contrat.DateModification = DateTime.UtcNow;
        _db.Contrats.Update(contrat);
        await _db.SaveChangesAsync(ct);
    }
}
