using System.Windows;
using System.Windows.Controls;
using fpdf.Core.Models;
using fpdf.Wpf.ViewModels;

namespace fpdf.Wpf.Views.Controls;

public partial class FolderTreeControl : UserControl
{
    public FolderTreeControl()
    {
        InitializeComponent();
    }

    private FolderTreeViewModel? ViewModel => DataContext as FolderTreeViewModel;

    private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem { DataContext: NetworkFolder folder })
        {
            if (ViewModel != null)
            {
                await ViewModel.ExpandFolderCommand.ExecuteAsync(folder);
            }
        }
    }

    private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem { DataContext: NetworkFolder folder })
        {
            if (ViewModel != null)
            {
                ViewModel.SelectFolderCommand.Execute(folder);
            }
        }
    }
}
