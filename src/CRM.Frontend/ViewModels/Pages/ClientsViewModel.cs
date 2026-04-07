using System.Collections.ObjectModel;
using CRM.Frontend.Services;
using CRM.Frontend.ViewModels;

namespace CRM.Frontend.ViewModels.Pages;

public class ClientsViewModel : ViewModelBase
{
    private readonly ApiClient   _api;
    private readonly AuthService _auth;

    private bool       _showForm;
    private bool       _hasMessage;
    private bool       _isError;
    private string     _statusMessage = string.Empty;
    private ClientDto? _selectedClient;
    private ClientDto  _newClient = new();

    public ObservableCollection<ClientDto> Clients { get; } = new();

    public bool CanCreate    => _auth.IsDirecteurFinances;
    public bool ShowForm     { get => _showForm;     set => SetField(ref _showForm, value); }
    public bool HasMessage   { get => _hasMessage;   set => SetField(ref _hasMessage, value); }
    public bool IsError      { get => _isError;      set => SetField(ref _isError, value); }
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }
    public ClientDto? SelectedClient { get => _selectedClient; set => SetField(ref _selectedClient, value); }
    public ClientDto  NewClient      { get => _newClient;      set => SetField(ref _newClient, value); }

    public AsyncRelayCommand LoadCommand      { get; }
    public RelayCommand      ShowAddFormCommand { get; }
    public RelayCommand      CancelFormCommand  { get; }
    public AsyncRelayCommand SaveClientCommand  { get; }

    public ClientsViewModel(ApiClient api, AuthService auth)
    {
        _api  = api;
        _auth = auth;

        LoadCommand       = new AsyncRelayCommand(_ => LoadAsync());
        ShowAddFormCommand = new RelayCommand(_ => { NewClient = new ClientDto(); ShowForm = true; HasMessage = false; });
        CancelFormCommand  = new RelayCommand(_ => ShowForm = false);
        SaveClientCommand  = new AsyncRelayCommand(_ => SaveAsync());
    }

    public async Task LoadAsync()
    {
        var list = await _api.GetClientsAsync();
        Clients.Clear();
        if (list is not null)
            foreach (var c in list) Clients.Add(c);
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(NewClient.NoClient) || string.IsNullOrWhiteSpace(NewClient.NomEntreprise))
        {
            SetStatus("No Client et Nom de l'entreprise sont obligatoires.", true);
            return;
        }

        var result = await _api.CreateClientAsync(NewClient);
        if (result is null)
        {
            SetStatus("Erreur : impossible de créer le client. Vérifiez que le No Client n'existe pas déjà.", true);
            return;
        }

        Clients.Add(result);
        ShowForm = false;
        SetStatus($"Client {result.NoClient} créé avec succès.", false);
    }

    private void SetStatus(string msg, bool isErr)
    {
        StatusMessage = msg;
        IsError       = isErr;
        HasMessage    = true;
    }
}
