using System.Collections.ObjectModel;
using CRM.Frontend.Services;
using CRM.Frontend.ViewModels;

namespace CRM.Frontend.ViewModels.Pages;

public class ContratsViewModel : ViewModelBase
{
    private readonly ApiClient   _api;
    private readonly AuthService _auth;

    private string      _searchNoClient = string.Empty;
    private bool        _hasMessage;
    private bool        _isError;
    private string      _statusMessage = string.Empty;
    private ContratDto? _selected;

    public ObservableCollection<ContratDto> Contrats { get; } = new();

    public string   SearchNoClient  { get => _searchNoClient; set => SetField(ref _searchNoClient, value); }
    public bool     HasMessage      { get => _hasMessage;     set => SetField(ref _hasMessage, value); }
    public bool     IsError         { get => _isError;        set => SetField(ref _isError, value); }
    public string   StatusMessage   { get => _statusMessage;  set => SetField(ref _statusMessage, value); }
    public ContratDto? Selected     { get => _selected;       set => SetField(ref _selected, value); }

    public bool CanCreate => _auth.IsDirecteurFinances;

    public AsyncRelayCommand SearchCommand      { get; }
    public AsyncRelayCommand CheckValidCommand  { get; }
    public RelayCommand      ShowFormCommand    { get; }

    public ContratsViewModel(ApiClient api, AuthService auth)
    {
        _api  = api;
        _auth = auth;

        SearchCommand      = new AsyncRelayCommand(_ => SearchAsync());
        CheckValidCommand  = new AsyncRelayCommand(_ => CheckValidAsync());
        ShowFormCommand    = new RelayCommand(_ => ShowCreateForm());
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchNoClient))
        {
            SetStatus("Saisissez un No Client avant de rechercher.", true);
            return;
        }

        HasMessage = false;
        var list = await _api.GetContratsClientAsync(SearchNoClient.ToUpper());
        Contrats.Clear();
        if (list is null || list.Count == 0)
        {
            SetStatus($"Aucun contrat trouvé pour {SearchNoClient.ToUpper()}.", true);
            return;
        }
        foreach (var c in list) Contrats.Add(c);
    }

    private async Task CheckValidAsync()
    {
        if (Selected is null) return;
        var result = await _api.ContratValidAsync(Selected.NoClient);
        if (result is null)
        {
            SetStatus("Erreur de communication avec le serveur.", true);
            return;
        }

        if (result.EstValide)
            SetStatus($"✅ ContratValid = VRAI — Contrat #{result.NoContrat} actif pour {result.NoClient}.", false);
        else
            SetStatus($"⛔ ContratValid = FAUX — {result.Raison}", true);
    }

    private void ShowCreateForm()
    {
        // Simplifié : un message invite à utiliser l'API directement ou un dialogue
        SetStatus("Pour créer un contrat, utilisez la console Swagger ou contactez votre administrateur.", false);
    }

    private void SetStatus(string msg, bool isErr)
    {
        StatusMessage = msg;
        IsError       = isErr;
        HasMessage    = true;
    }
}
