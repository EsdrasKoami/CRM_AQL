using System.Windows.Controls;
using CRM.Frontend.Services;
using CRM.Frontend.ViewModels.Pages;

namespace CRM.Frontend.Views.Pages;

public partial class TableauDeBordPage : Page
{
    public TableauDeBordPage(ApiClient api, AuthService auth)
    {
        InitializeComponent();
        DataContext = new TableauDeBordViewModel(api, auth);
    }
}
