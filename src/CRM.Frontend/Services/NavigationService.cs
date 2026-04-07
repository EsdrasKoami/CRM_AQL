using System.Windows.Controls;

namespace CRM.Frontend.Services;

/// <summary>Simple service de navigation entre les pages de la MainWindow.</summary>
public class NavigationService
{
    private Frame? _frame;

    public void Initialize(Frame frame) => _frame = frame;

    public void NavigateTo(Page page)
        => _frame?.Navigate(page);
}
