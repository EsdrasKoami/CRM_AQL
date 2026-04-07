using CRM.Domain.Interfaces.Repositories;
using CRM.Domain.Interfaces.Services;
using CRM.Domain.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRM.Business.Services;

/// <summary>
/// Gestion JIT : valide les quotas mensuels min/max par produit.
/// </summary>
public class JitQuotaService : IJitQuotaService
{
    private readonly IItemContratRepository  _itemRepo;
    private readonly ITransactionRepository  _transactionRepo;
    private readonly ILogger<JitQuotaService> _logger;

    // Pour les quotas, on passe par la SP qui fait l'agrégation.
    private readonly string _connectionString;

    public JitQuotaService(
        IItemContratRepository itemRepo,
        ITransactionRepository transactionRepo,
        ILogger<JitQuotaService> logger,
        string connectionString)
    {
        _itemRepo        = itemRepo;
        _transactionRepo = transactionRepo;
        _logger          = logger;
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task<bool> QuotaRespecteeAsync(
        int noContrat, string noProduit, int quantite, int annee, int mois,
        CancellationToken ct = default)
    {
        var item = await _itemRepo.GetAsync(noContrat, noProduit, ct);
        if (item is null)
        {
            _logger.LogWarning("JIT — Produit {Produit} absent du contrat #{Contrat}.", noProduit, noContrat);
            return false;
        }

        var quotas = await GetConsommationMensuelleAsync(noContrat, annee, mois, ct);
        var q = quotas.FirstOrDefault(x => x.NoProduit == noProduit);

        int dejaCommandee = q?.QuantiteCommandee ?? 0;
        int totalApresCde = dejaCommandee + quantite;

        bool ok = totalApresCde >= item.QuotaMin && totalApresCde <= item.QuotaMax;

        _logger.LogInformation(
            "JIT — Contrat #{Contrat}, Produit {Produit}, {Mois:00}/{Annee}: déjà={Deja}, +{Qte}={Total} [{Min}..{Max}] → {Res}",
            noContrat, noProduit, mois, annee, dejaCommandee, quantite, totalApresCde,
            item.QuotaMin, item.QuotaMax, ok ? "OK" : "QUOTA_INVALIDE");

        return ok;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<QuotaMensuelDto>> GetConsommationMensuelleAsync(
        int noContrat, int annee, int mois, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        var cmd = new CommandDefinition(
            "dbo.usp_ConsommationMensuelleJIT",
            new { NoContrat = noContrat, Annee = annee, Mois = mois },
            commandType: System.Data.CommandType.StoredProcedure,
            cancellationToken: ct);

        return await conn.QueryAsync<QuotaMensuelDto>(cmd);
    }
}
