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
    private readonly Channel<CommandeMessage>    _commandeChannel;
    private readonly Channel<ExpeditionMessage>  _expeditionChannel;
    private readonly ILogger<FacturesController> _logger;

    public FacturesController(
        IFacturationService facturation,
        Channel<CommandeMessage> commandeChannel,
        Channel<ExpeditionMessage> expeditionChannel,
        ILogger<FacturesController> logger)
    {
        _facturation       = facturation;
        _commandeChannel   = commandeChannel;
        _expeditionChannel = expeditionChannel;
        _logger            = logger;
    }

    /// <summary>
    /// Signal d'expédition : déclenche la création de facture immédiatement.
    /// </summary>
    [HttpPost("expedition")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SignalExpedition([FromBody] ExpeditionRequest req, CancellationToken ct)
    {
        var noUtilisateur = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;

        var p = new CreerFactureParams(
            req.NoContrat, req.NoProduit, req.Quantite,
            req.Reference, noUtilisateur,
            "Facture auto — signal d'expédition REST");

        try
        {
            int noTx = await _facturation.CreerFactureSurExpeditionAsync(p, ct);
            _logger.LogInformation("Facture #{NoTx} créée via API expédition.", noTx);
            return Ok(new { noTransaction = noTx, message = "Facture créée." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la facturation expédition.");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Envoie une commande dans la file MQ pour traitement asynchrone (Always-On).
    /// </summary>
    [HttpPost("commande-mq")]
    [ProducesResponseType(202)]
    public async Task<IActionResult> EnvoyerCommandeMq([FromBody] CommandeRequest req, CancellationToken ct)
    {
        var noUtilisateur = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : (int?)null;

        var msg = new CommandeMessage(
            req.NoClient, req.NoContrat, req.NoProduit, req.Quantite,
            req.Reference, noUtilisateur);

        await _commandeChannel.Writer.WriteAsync(msg, ct);
        _logger.LogInformation("Commande MQ enfilée: {Client}/{Produit}", req.NoClient, req.NoProduit);
        return Accepted(new { message = "Commande reçue et mise en file." });
    }
}

// ── DTO Records ──────────────────────────────────────────────────────────────
public record ExpeditionRequest(int NoContrat, string NoProduit, int Quantite, string? Reference = null);
public record CommandeRequest(string NoClient, int NoContrat, string NoProduit, int Quantite, string? Reference = null);
