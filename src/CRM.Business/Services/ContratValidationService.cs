using CRM.Domain.Interfaces.Repositories;
using CRM.Domain.Interfaces.Services;
using CRM.Domain.Models;
using Microsoft.Extensions.Logging;

namespace CRM.Business.Services;

/// <summary>
/// Service central : ContratValid().
/// Vérifie qu'un client possède un contrat actif à la date courante.
/// Appelé par le BackgroundService MQ.
/// </summary>
public class ContratValidationService : IContratValidationService
{
    private readonly IContratRepository _contratRepo;
    private readonly ILogger<ContratValidationService> _logger;

    public ContratValidationService(
        IContratRepository contratRepo,
        ILogger<ContratValidationService> logger)
    {
        _contratRepo = contratRepo;
        _logger      = logger;
    }

    /// <inheritdoc />
    public async Task<ContratValidResult> ContratValidAsync(string noClient, CancellationToken ct = default)
    {
        _logger.LogInformation("ContratValid — vérification pour le client {NoClient}", noClient);

        if (string.IsNullOrWhiteSpace(noClient))
        {
            _logger.LogWarning("ContratValid — NoClient invalide (vide).");
            return new ContratValidResult(false, "NoClient ne peut pas être vide.");
        }

        var contrat = await _contratRepo.GetContratActifAsync(noClient, DateTime.UtcNow, ct);

        if (contrat is null)
        {
            _logger.LogWarning("ContratValid — Aucun contrat actif pour {NoClient}.", noClient);
            return new ContratValidResult(false, $"Aucun contrat actif pour le client {noClient}.", null);
        }

        _logger.LogInformation(
            "ContratValid — Contrat #{NoContrat} actif pour {NoClient} (fin: {DateFin:yyyy-MM-dd}).",
            contrat.NoContrat, noClient, contrat.DateFin);

        return new ContratValidResult(true, null, contrat.NoContrat);
    }
}
