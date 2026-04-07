using System.Windows;
using System.Net.Http;
using CRM.Frontend.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Frontend;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        // HttpClient pointant sur le backend
        services.AddHttpClient<ApiClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:5000/api/");
            client.Timeout     = TimeSpan.FromSeconds(30);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            // En dev : accepte le certificat auto-signé localhost
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        services.AddSingleton<AuthService>();
        services.AddSingleton<NavigationService>();

        Services = services.BuildServiceProvider();

        var login = new Views.LoginWindow();
        login.Show();
        MainWindow = login;
    }
}
