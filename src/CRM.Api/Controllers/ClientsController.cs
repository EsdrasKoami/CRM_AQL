using CRM.Domain.Entities;
using CRM.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Agent")]
[Produces("application/json")]
public class ClientsController : ControllerBase
{
    private readonly IClientRepository _repo;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(IClientRepository repo, ILogger<ClientsController> logger)
    {
        _repo   = repo;
        _logger = logger;
    }

    /// <summary>Liste tous les clients actifs.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Client>), 200)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _repo.GetAllActiveAsync(ct));

    /// <summary>Retourne un client par son identifiant (ex: C000001).</summary>
    [HttpGet("{noClient}")]
    [ProducesResponseType(typeof(Client), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string noClient, CancellationToken ct)
    {
        var client = await _repo.GetByIdAsync(noClient.ToUpper(), ct);
        return client is null ? NotFound() : Ok(client);
    }

    /// <summary>Crée un nouveau client.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Client), 201)]
    [ProducesResponseType(400)]
    [Authorize(Policy = "DirecteurFinances")]
    public async Task<IActionResult> Create([FromBody] Client client, CancellationToken ct)
    {
        client.NoClient = client.NoClient.ToUpper();
        if (await _repo.ExistsAsync(client.NoClient, ct))
            return BadRequest(new { message = $"Le client {client.NoClient} existe déjà." });

        await _repo.AddAsync(client, ct);
        _logger.LogInformation("Client {NoClient} créé.", client.NoClient);
        return CreatedAtAction(nameof(GetById), new { noClient = client.NoClient }, client);
    }

    /// <summary>Met à jour un client existant.</summary>
    [HttpPut("{noClient}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [Authorize(Policy = "DirecteurFinances")]
    public async Task<IActionResult> Update(string noClient, [FromBody] Client client, CancellationToken ct)
    {
        if (!await _repo.ExistsAsync(noClient.ToUpper(), ct))
            return NotFound();

        client.NoClient = noClient.ToUpper();
        await _repo.UpdateAsync(client, ct);
        _logger.LogInformation("Client {NoClient} mis à jour.", noClient);
        return NoContent();
    }
}
