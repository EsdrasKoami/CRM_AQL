using System.Windows.Controls;
using CRM.Frontend.Services;
using CRM.Frontend.ViewModels.Pages;

namespace CRM.Frontend.Views.Pages;

public partial class ContratsPage : Page
{
    public ContratsPage(ApiClient api, AuthService auth)
    {
        InitializeComponent();
        DataContext = new ContratsViewModel(api, auth);
    }
}
