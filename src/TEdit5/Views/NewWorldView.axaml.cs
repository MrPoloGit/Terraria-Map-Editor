using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TEdit5.Views;

public partial class NewWorldView : Window
{
    public bool Confirmed { get; private set; }

    public NewWorldView()
    {
        InitializeComponent();
    }

    private void OkClick(object? sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void CancelClick(object? sender, RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }
}
