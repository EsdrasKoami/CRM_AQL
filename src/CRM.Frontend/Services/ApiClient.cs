using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace CRM.Frontend.Services;

/// <summary>
/// Client HTTP centralisé. TOUTE communication avec la BD passe par cette classe
/// via les endpoints de l'API. Aucune connexion SQL directe.
/// </summary>
public class ApiClient
{
    private readonly HttpClient   _http;
    private readonly AuthService  _auth;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive  = true,
        PropertyNamingPolicy         = JsonNamingPolicy.CamelCase,
    };

    public ApiClient(HttpClient http, AuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    // ── Auth ────────────────────────────────────────────────────────────────
    public async Task<LoginResponse?> LoginAsync(string login, string password)
    {
        var resp = await _http.PostAsJsonAsync("auth/login",
            new { Login = login, Password = password });
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<LoginResponse>(JsonOpts);
    }

    // ── Clients ──────────────────────────────────────────────────────────────
    public Task<List<ClientDto>?> GetClientsAsync()
        => GetAsync<List<ClientDto>>("clients");

    public Task<ClientDto?> GetClientAsync(string noClient)
        => GetAsync<ClientDto>($"clients/{noClient}");

    public Task<ClientDto?> CreateClientAsync(ClientDto client)
        => PostAsync<ClientDto>("clients", client);

    // ── Contrats ─────────────────────────────────────────────────────────────
    public Task<List<ContratDto>?> GetContratsClientAsync(string noClient)
        => GetAsync<List<ContratDto>>($"contrats/client/{noClient}");

    public Task<ContratDto?> GetContratAsync(int id)
        => GetAsync<ContratDto>($"contrats/{id}");

    public Task<ContratValidResult?> ContratValidAsync(string noClient)
        => GetAsync<ContratValidResult>($"contrats/client/{noClient}/valide");

    public Task<ContratDto?> CreateContratAsync(ContratDto contrat)
        => PostAsync<ContratDto>("contrats", contrat);

    public async Task<bool> ModifierPlafondAsync(int noContrat, decimal nouveau)
    {
        AttachToken();
        var resp = await _http.PatchAsJsonAsync(
            $"contrats/{noContrat}/plafond", nouveau);
        return resp.IsSuccessStatusCode;
    }

    // ── Transactions & Solde ─────────────────────────────────────────────────
    public Task<List<SoldeClientDto>?> GetSoldeAsync(string noClient)
        => GetAsync<List<SoldeClientDto>>($"transactions/solde/{noClient}");

    public Task<List<TransactionDto>?> GetTransactionsContratAsync(int noContrat)
        => GetAsync<List<TransactionDto>>($"transactions/contrat/{noContrat}");

    public async Task<ApiResult> EnregistrerPaiementAsync(int noContrat, decimal montant, string? reference)
    {
        AttachToken();
        var resp = await _http.PostAsJsonAsync("transactions/paiement",
            new { NoContrat = noContrat, Montant = montant, Reference = reference });
        return await ParseResult(resp);
    }

    // ── Factures ─────────────────────────────────────────────────────────────
    public async Task<ApiResult> DeclencharExpeditionAsync(int noContrat, string noProduit, int quantite, string? reference)
    {
        AttachToken();
        var resp = await _http.PostAsJsonAsync("factures/expedition",
            new { NoContrat = noContrat, NoProduit = noProduit, Quantite = quantite, Reference = reference });
        return await ParseResult(resp);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void AttachToken()
    {
        _http.DefaultRequestHeaders.Authorization =
            _auth.Token is not null
                ? new AuthenticationHeaderValue("Bearer", _auth.Token)
                : null;
    }

    private async Task<T?> GetAsync<T>(string path)
    {
        AttachToken();
        var resp = await _http.GetAsync(path);
        if (!resp.IsSuccessStatusCode) return default;
        return await resp.Content.ReadFromJsonAsync<T>(JsonOpts);
    }

    private async Task<T?> PostAsync<T>(string path, object body)
    {
        AttachToken();
        var resp = await _http.PostAsJsonAsync(path, body);
        if (!resp.IsSuccessStatusCode) return default;
        return await resp.Content.ReadFromJsonAsync<T>(JsonOpts);
    }

    private static async Task<ApiResult> ParseResult(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode)
            return new ApiResult(true, null);

        string raw = await resp.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var msg = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : raw;
            return new ApiResult(false, msg);
        }
        catch { return new ApiResult(false, raw); }
    }
}

// ── DTOs (miroir des modèles backend) ────────────────────────────────────────
public record LoginResponse(string Token, string Role, string Nom, string Prenom);

public record ApiResult(bool Success, string? Message);

public class ClientDto
{
    public string NoClient      { get; set; } = string.Empty;
    public string NomEntreprise { get; set; } = string.Empty;
    public string? Courriel     { get; set; }
    public string? Telephone    { get; set; }
    public string? Ville        { get; set; }
    public bool   EstActif      { get; set; } = true;
}

public class ContratDto
{
    public int     NoContrat   { get; set; }
    public string  NoClient    { get; set; } = string.Empty;
    public DateTime DateDebut  { get; set; }
    public DateTime DateFin    { get; set; }
    public decimal MontantMax  { get; set; }
    public string? Description { get; set; }
    public bool    EstActif    { get; set; }
}

public class SoldeClientDto
{
    public string  NoClient          { get; set; } = string.Empty;
    public string  NomEntreprise     { get; set; } = string.Empty;
    public int     NoContrat         { get; set; }
    public decimal MontantMax        { get; set; }
    public decimal TotalFactures     { get; set; }
    public decimal TotalPaiements    { get; set; }
    public decimal SoldeContrat      { get; set; }
    public decimal CreditDisponible  { get; set; }
    public bool    ContratActif      { get; set; }
}

public class TransactionDto
{
    public int      NoTransaction   { get; set; }
    public int      NoContrat       { get; set; }
    public string   TypeTransaction { get; set; } = string.Empty;
    public decimal  Montant         { get; set; }
    public string?  Reference       { get; set; }
    public string?  NoProduit       { get; set; }
    public int?     Quantite        { get; set; }
    public DateTime DateTransaction { get; set; }
}

public class ContratValidResult
{
    public string  NoClient  { get; set; } = string.Empty;
    public bool    EstValide  { get; set; }
    public int?    NoContrat  { get; set; }
    public string? Raison     { get; set; }
}
