using System.Windows;
using System.Windows.Controls;
using CRM.Frontend.Services;
using CRM.Frontend.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Frontend.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow()
    {
        InitializeComponent();
        _vm = new LoginViewModel(
            App.Services.GetRequiredService<ApiClient>(),
            App.Services.GetRequiredService<AuthService>(),
            OnLoginSuccess);
        DataContext = _vm;
    }

    private void OnLoginSuccess()
    {
        var main = new MainWindow();
        main.Show();
        App.Current.MainWindow = main;
        Close();
    }

    private bool _isSyncing;

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isSyncing) return;
        _isSyncing = true;
        TxtPwdVisible.Text = PwdBox.Password;
        _isSyncing = false;
    }

    private void OnPwdVisibleChanged(object sender, TextChangedEventArgs e)
    {
        if (_isSyncing) return;
        _isSyncing = true;
        PwdBox.Password = TxtPwdVisible.Text;
        _isSyncing = false;
    }

    private void OnTogglePassword(object sender, RoutedEventArgs e)
    {
        if (TxtPwdVisible.Visibility == Visibility.Visible)
        {
            TxtPwdVisible.Visibility = Visibility.Collapsed;
            PwdBox.Visibility = Visibility.Visible;
            PwdBox.Focus();
        }
        else
        {
            TxtPwdVisible.Visibility = Visibility.Visible;
            PwdBox.Visibility = Visibility.Collapsed;
            TxtPwdVisible.Focus();
            TxtPwdVisible.CaretIndex = TxtPwdVisible.Text.Length;
        }
    }
}
