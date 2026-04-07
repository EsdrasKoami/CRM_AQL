using System.Windows.Controls;
using CRM.Frontend.Services;

namespace CRM.Frontend.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly ApiClient    _api;
    private readonly AuthService  _auth;
    private readonly Action       _onSuccess;

    private string  _login        = string.Empty;
    private string  _errorMessage = string.Empty;
    private bool    _hasError;
    private bool    _isBusy;

    public string Login
    {
        get => _login;
        set => SetField(ref _login, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set { SetField(ref _errorMessage, value); HasError = !string.IsNullOrEmpty(value); }
    }

    public bool HasError
    {
        get => _hasError;
        set => SetField(ref _hasError, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public AsyncRelayCommand LoginCommand { get; }

    public LoginViewModel(ApiClient api, AuthService auth, Action onSuccess)
    {
        _api       = api;
        _auth      = auth;
        _onSuccess = onSuccess;
        LoginCommand = new AsyncRelayCommand(ExecuteLogin, _ => !IsBusy);
    }

    private async Task ExecuteLogin(object? param)
    {
        // PasswordBox ne supporte pas le binding → passé via CommandParameter
        var pwd = (param as PasswordBox)?.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(pwd))
        {
            ErrorMessage = "Veuillez saisir votre identifiant et votre mot de passe.";
            return;
        }

        IsBusy       = true;
        ErrorMessage = string.Empty;

        try
        {
            var resp = await _api.LoginAsync(Login, pwd);
            if (resp is null)
            {
                ErrorMessage = "Identifiants invalides. Veuillez réessayer.";
                return;
            }

            _auth.SetSession(resp);
            _onSuccess();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur de connexion au serveur : {ex.Message}";
        }
        finally { IsBusy = false; }
    }
}
