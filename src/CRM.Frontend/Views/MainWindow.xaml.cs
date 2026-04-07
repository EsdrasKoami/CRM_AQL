using System.Windows;
using CRM.Frontend.Services;
using CRM.Frontend.ViewModels;
using CRM.Frontend.Views.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Frontend.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly AuthService   _auth;
    private readonly ApiClient     _api;

    public MainWindow()
    {
        InitializeComponent();
        _auth = App.Services.GetRequiredService<AuthService>();
        _api  = App.Services.GetRequiredService<ApiClient>();

        var nav = App.Services.GetRequiredService<NavigationService>();
        nav.Initialize(ContentFrame);

        _vm = new MainViewModel(_auth, nav,
            () => new ClientsPage(_api, _auth),
            () => new ContratsPage(_api, _auth),
            () => new TableauDeBordPage(_api, _auth));

        DataContext = _vm;
        _vm.NavClientsCommand.Execute(null);  // page par défaut
    }
}
