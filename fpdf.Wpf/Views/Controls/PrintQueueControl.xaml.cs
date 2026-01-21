using System.Windows;
using System.Windows.Controls;
using fpdf.Wpf.ViewModels;
using fpdf.Wpf.Views.Dialogs;

namespace fpdf.Wpf.Views.Controls;

public partial class PrintQueueControl : UserControl
{
    public PrintQueueControl()
    {
        InitializeComponent();
    }

    private void OpenQueueDialog_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is PrintQueueViewModel vm)
        {
            var dialog = new PrintQueueDialog(vm)
            {
                Owner = Window.GetWindow(this)
            };
            dialog.ShowDialog();
        }
    }
}
