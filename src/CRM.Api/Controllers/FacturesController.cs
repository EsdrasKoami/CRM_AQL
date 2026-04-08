using CRM.Domain.Interfaces.Services;
using CRM.Domain.Models;
using CRM.Messaging.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Channels;

namespace CRM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Agent")]
[Produces("application/json")]
public class FacturesController : ControllerBase
{
    private readonly IFacturationService         _facturation;
    private readonly ILogger<FacturesController> _logger;

    public FacturesController(
        IFacturationService facturation,
        ILogger<FacturesController> logger)
    {
        _facturation       = facturation;
        _logger            = logger;
    }


    /* Endpoints de validation HTTP désactivés pour conformité Architecture Découplée (MQ Only)
    [HttpPost("expedition")]
    public async Task<IActionResult> SignalExpedition([FromBody] ExpeditionRequest req, CancellationToken ct) { ... }
    
    [HttpPost("commande-mq")]
    public async Task<IActionResult> EnvoyerCommandeMq([FromBody] CommandeRequest req, CancellationToken ct) { ... }
    */

}

// ── DTO Records ──────────────────────────────────────────────────────────────
public record ExpeditionRequest(int NoContrat, string NoProduit, int Quantite, string? Reference = null);
public record CommandeRequest(string NoClient, int NoContrat, string NoProduit, int Quantite, string? Reference = null);
