using fpdf.Wpf.ViewModels;

namespace fpdf.Wpf.Views.Dialogs;

public partial class PrintHistoryDialog : HandyControl.Controls.Window
{
    public PrintHistoryDialog(PrintHistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
