using System.Windows.Controls;
using CRM.Frontend.Services;
using CRM.Frontend.ViewModels.Pages;

namespace CRM.Frontend.Views.Pages;

public partial class ClientsPage : Page
{
    public ClientsPage(ApiClient api, AuthService auth)
    {
        InitializeComponent();
        var vm = new ClientsViewModel(api, auth);
        DataContext = vm;
        _ = vm.LoadAsync();
    }
}
