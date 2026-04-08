using CRM.Domain.Entities;
using CRM.Domain.Interfaces.Repositories;
using CRM.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Agent")]
[Produces("application/json")]
public class ContratsController : ControllerBase
{
    private readonly IContratRepository       _repo;
    private readonly IContratValidationService _validation;
    private readonly ICreditControlService     _credit;
    private readonly ILogger<ContratsController> _logger;

    public ContratsController(
        IContratRepository repo,
        IContratValidationService validation,
        ICreditControlService credit,
        ILogger<ContratsController> logger)
    {
        _repo       = repo;
        _validation = validation;
        _credit     = credit;
        _logger     = logger;
    }

    /// <summary>Liste les contrats d'un client.</summary>
    [HttpGet("client/{noClient}")]
    [ProducesResponseType(typeof(IEnumerable<Contrat>), 200)]
    public async Task<IActionResult> GetByClient(string noClient, CancellationToken ct)
        => Ok(await _repo.GetByClientAsync(noClient.ToUpper(), ct));

    /// <summary>Retourne un contrat par ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Contrat), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var contrat = await _repo.GetByIdAsync(id, ct);
        return contrat is null ? NotFound() : Ok(contrat);
    }



    /// <summary>Crée un nouveau contrat.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Contrat), 201)]
    [Authorize(Policy = "DirecteurFinances")]
    public async Task<IActionResult> Create([FromBody] Contrat contrat, CancellationToken ct)
    {
        await _repo.AddAsync(contrat, ct);
        _logger.LogInformation("Contrat #{NoContrat} créé pour {NoClient}.", contrat.NoContrat, contrat.NoClient);
        return CreatedAtAction(nameof(GetById), new { id = contrat.NoContrat }, contrat);
    }

    /// <summary>Modifie le plafond de crédit — DirecteurFinances seulement.</summary>
    [HttpPatch("{id:int}/plafond")]
    [Authorize(Policy = "DirecteurFinances")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ModifierPlafond(int id, [FromBody] decimal nouveauMontantMax, CancellationToken ct)
    {
        var ok = await _credit.ModifierPlafondAsync(id, nouveauMontantMax, ct);
        if (!ok) return NotFound();

        _logger.LogInformation(
            "Plafond contrat #{Id} modifié à {Montant:C} par {User}.",
            id, nouveauMontantMax, User.Identity?.Name);

        return NoContent();
    }
}
