using Avalonia.Controls;
using Avalonia.Interactivity;
using SMDB.Core;

namespace SMDB.Avalonia;

public partial class MainWindow : Window
{
    private readonly DatabaseEngine _engine = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void ClearQuery(object? sender, RoutedEventArgs e)
    {
        QueryBox.Text = "";
    }

    private void ExecuteQuery(object? sender, RoutedEventArgs e)
    {
        OutputBox.Text = _engine.Execute(QueryBox.Text ?? "");
    }
}