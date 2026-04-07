using CRM.Domain.Interfaces.Repositories;
using CRM.Domain.Interfaces.Services;
using CRM.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Agent")]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _repo;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(ITransactionRepository repo, ILogger<TransactionsController> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    /// <summary>Historique des transactions d'un contrat.</summary>
    [HttpGet("contrat/{noContrat:int}")]
    [ProducesResponseType(typeof(IEnumerable<object>), 200)]
    public async Task<IActionResult> GetByContrat(int noContrat, CancellationToken ct)
        => Ok(await _repo.GetByContratAsync(noContrat, ct));

    /// <summary>Solde temps réel d'un client (via usp_GetSoldeClient).</summary>
    [HttpGet("solde/{noClient}")]
    [ProducesResponseType(typeof(IEnumerable<SoldeClientDto>), 200)]
    public async Task<IActionResult> GetSolde(string noClient, CancellationToken ct)
        => Ok(await _repo.GetSoldeClientAsync(noClient.ToUpper(), ct));

    /// <summary>Enregistre un paiement partiel ou total.</summary>
    [HttpPost("paiement")]
    [Authorize(Policy = "DirecteurFinances")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> EnregistrerPaiement([FromBody] EnregistrerPaiementParams p, CancellationToken ct)
    {
        var noTx = await _repo.EnregistrerPaiementAsync(p, ct);
        _logger.LogInformation("Paiement #{NoTx} de {Montant:C} enregistré, contrat #{Contrat}.",
            noTx, p.Montant, p.NoContrat);
        return Ok(new { noTransaction = noTx });
    }
}
