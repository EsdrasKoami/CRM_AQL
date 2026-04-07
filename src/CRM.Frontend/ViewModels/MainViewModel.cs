using System.Windows;
using CRM.Frontend.Services;

namespace CRM.Frontend.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly AuthService      _auth;
    private readonly NavigationService _nav;
    private readonly Func<System.Windows.Controls.Page> _clientsFactory;
    private readonly Func<System.Windows.Controls.Page> _contratsFactory;
    private readonly Func<System.Windows.Controls.Page> _dashFactory;

    public string UserDisplay
        => $"{_auth.Prenom} {_auth.Nom}";

    public string RoleBadge
        => _auth.Role == "DirecteurFinances" ? "Directeur Finances" : "Agent";

    public RelayCommand NavClientsCommand   { get; }
    public RelayCommand NavContratsCommand  { get; }
    public RelayCommand NavDashboardCommand { get; }
    public RelayCommand LogoutCommand       { get; }

    public MainViewModel(
        AuthService auth,
        NavigationService nav,
        Func<System.Windows.Controls.Page> clientsFactory,
        Func<System.Windows.Controls.Page> contratsFactory,
        Func<System.Windows.Controls.Page> dashFactory)
    {
        _auth            = auth;
        _nav             = nav;
        _clientsFactory  = clientsFactory;
        _contratsFactory = contratsFactory;
        _dashFactory     = dashFactory;

        NavClientsCommand   = new RelayCommand(_ => _nav.NavigateTo(_clientsFactory()));
        NavContratsCommand  = new RelayCommand(_ => _nav.NavigateTo(_contratsFactory()));
        NavDashboardCommand = new RelayCommand(_ => _nav.NavigateTo(_dashFactory()));

        LogoutCommand = new RelayCommand(_ =>
        {
            _auth.Logout();
            var login = new Views.LoginWindow();
            login.Show();
            Application.Current.MainWindow = login;
            foreach (Window w in Application.Current.Windows)
                if (w is not Views.LoginWindow) w.Close();
        });
    }
}
