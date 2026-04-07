using System.Collections.ObjectModel;
using CRM.Frontend.Services;
using CRM.Frontend.ViewModels;

namespace CRM.Frontend.ViewModels.Pages;

public class TableauDeBordViewModel : ViewModelBase
{
    private readonly ApiClient   _api;
    private readonly AuthService _auth;

    private string        _searchNoClient   = string.Empty;
    private bool          _hasMessage;
    private bool          _isError;
    private string        _statusMessage    = string.Empty;
    private SoldeClientDto? _selectedSolde;
    private string        _paiementNoContrat = string.Empty;
    private string        _paiementMontant   = string.Empty;
    private string        _paiementRef       = string.Empty;

    public ObservableCollection<SoldeClientDto> Soldes { get; } = new();

    public string       SearchNoClient    { get => _searchNoClient;    set => SetField(ref _searchNoClient, value); }
    public bool         HasMessage        { get => _hasMessage;         set => SetField(ref _hasMessage, value); }
    public bool         IsError           { get => _isError;            set => SetField(ref _isError, value); }
    public string       StatusMessage     { get => _statusMessage;      set => SetField(ref _statusMessage, value); }
    public SoldeClientDto? SelectedSolde { get => _selectedSolde;     set => SetField(ref _selectedSolde, value); }
    public string       PaiementNoContrat { get => _paiementNoContrat; set => SetField(ref _paiementNoContrat, value); }
    public string       PaiementMontant   { get => _paiementMontant;   set => SetField(ref _paiementMontant, value); }
    public string       PaiementRef       { get => _paiementRef;       set => SetField(ref _paiementRef, value); }

    public bool IsDirecteurFinances => _auth.IsDirecteurFinances;

    public AsyncRelayCommand LoadSoldeCommand         { get; }
    public AsyncRelayCommand EnregistrerPaiementCommand { get; }

    public TableauDeBordViewModel(ApiClient api, AuthService auth)
    {
        _api  = api;
        _auth = auth;

        LoadSoldeCommand          = new AsyncRelayCommand(_ => LoadSoldeAsync());
        EnregistrerPaiementCommand = new AsyncRelayCommand(_ => EnregistrerPaiementAsync(),
            _ => IsDirecteurFinances);
    }

    private async Task LoadSoldeAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchNoClient))
        {
            SetStatus("Saisissez un No Client.", true);
            return;
        }

        HasMessage = false;
        var list = await _api.GetSoldeAsync(SearchNoClient.ToUpper());
        Soldes.Clear();

        if (list is null || list.Count == 0)
        {
            SetStatus($"Aucune donnée financière pour {SearchNoClient.ToUpper()}.", true);
            return;
        }

        foreach (var s in list) Soldes.Add(s);

        // Avertissement si crédit dépassé
        var critique = list.FirstOrDefault(s => s.ContratActif && s.CreditDisponible < 0);
        if (critique is not null)
            SetStatus(
                $"⛔ ALERTE CRÉDIT : Le client {critique.NoClient} dépasse son plafond de {critique.MontantMax:C}. " +
                $"Solde actuel : {critique.SoldeContrat:C}.", true);
    }

    private async Task EnregistrerPaiementAsync()
    {
        if (!int.TryParse(PaiementNoContrat, out int noContrat))
        { SetStatus("No Contrat invalide.", true); return; }

        if (!decimal.TryParse(PaiementMontant, System.Globalization.NumberStyles.Any,
                              System.Globalization.CultureInfo.InvariantCulture, out decimal montant)
            || montant <= 0)
        { SetStatus("Montant invalide (doit être un nombre positif).", true); return; }

        var result = await _api.EnregistrerPaiementAsync(noContrat, montant, PaiementRef);

        if (!result.Success)
        {
            SetStatus($"⛔ Erreur : {result.Message}", true);
            return;
        }

        SetStatus($"✅ Paiement de {montant:C} enregistré pour le contrat #{noContrat}.", false);
        PaiementMontant   = string.Empty;
        PaiementRef       = string.Empty;
        await LoadSoldeAsync(); // Rafraîchir le solde
    }

    private void SetStatus(string msg, bool isErr)
    {
        StatusMessage = msg;
        IsError       = isErr;
        HasMessage    = true;
    }
}
