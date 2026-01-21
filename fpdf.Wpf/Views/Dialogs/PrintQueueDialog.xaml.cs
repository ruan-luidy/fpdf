using fpdf.Wpf.ViewModels;

namespace fpdf.Wpf.Views.Dialogs;

public partial class PrintQueueDialog : HandyControl.Controls.Window
{
    public PrintQueueDialog(PrintQueueViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
