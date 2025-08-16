using Windows.Storage.Pickers;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Input;

namespace Skriva;

public class FileSystemItem
{
    public required string Name { get; set; }
    public required string FullPath { get; set; }
    public ObservableCollection<FileSystemItem> Children { get; set; } = [];
}

public sealed partial class MainPage : Page
{
    private double _fileTreeColumnWidth;
    private double _fileTreeColumnMinWidth;

    public MainPage()
    {
        this.InitializeComponent();
        _fileTreeColumnWidth = MainGrid.ColumnDefinitions[1].Width.Value;
        _fileTreeColumnMinWidth = MainGrid.ColumnDefinitions[1].MinWidth;
    }

    private void PopulateTreeView(string path)
    {
        var rootItems = new ObservableCollection<FileSystemItem>();
        var rootItem = new FileSystemItem
        {
            Name = Path.GetFileName(path),
            FullPath = path,
        };
        rootItems.Add(rootItem);
        AddDirectoryNodes(rootItem, path);
        FileTreeView.ItemsSource = rootItems;
    }

    private void AddDirectoryNodes(FileSystemItem parentItem, string path)
    {
        foreach (var directoryPath in Directory.GetDirectories(path))
        {
            var directoryItem = new FileSystemItem
            {
                Name = Path.GetFileName(directoryPath),
                FullPath = directoryPath,
            };
            parentItem.Children.Add(directoryItem);
            AddDirectoryNodes(directoryItem, directoryPath);
        }

        foreach (var filePath in Directory.GetFiles(path))
        {
            var fileItem = new FileSystemItem
            {
                Name = Path.GetFileName(filePath),
                FullPath = filePath
            };
            parentItem.Children.Add(fileItem);
        }
    }

    private void FileTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is FileSystemItem item && item.Children.Count == 0)
        {
            var path = item.FullPath;
            foreach (TabViewItem openTab in EditorTabView.TabItems.Cast<TabViewItem>())
            {
                if (openTab.Tag is string tabPath && tabPath == path)
                {
                    EditorTabView.SelectedItem = openTab;
                    return;
                }
            }
            OpenFileInTab(path);
        }
    }

    private void OpenFileInTab(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }
        var fileContent = File.ReadAllText(path);
        var textBox = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Text = fileContent,
            Margin = new Thickness(10),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 14,
        };
        textBox.TextChanged += TextBox_TextChanged;

        var newTab = new TabViewItem
        {
            Header = Path.GetFileName(path),
            Tag = path,
            Content = textBox
        };
        EditorTabView.TabItems.Add(newTab);
        EditorTabView.SelectedItem = newTab;
    }

    private void EditorTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        sender.TabItems.Remove(args.Tab);
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Exit();
    }

    private async void MenuFile_OpenProject(object sender, RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder
        };

        StorageFolder pickedFolder = await folderPicker.PickSingleFolderAsync();
        if (pickedFolder != null)
        {
            PopulateTreeView(pickedFolder.Path);
        }
    }
    
    private async void MenuFile_OpenFile(object sender, RoutedEventArgs e)
    {
        var filePicker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder
        };
        filePicker.FileTypeFilter.Add("*");
        
        StorageFile pickedFile = await filePicker.PickSingleFileAsync();
        if (pickedFile != null)
        {
            OpenFileInTab(pickedFile.Path);
        }
    }

    private void MenuFile_Save(object sender, RoutedEventArgs e)
    {
        if (EditorTabView.SelectedItem is TabViewItem selectedTab)
        {
            if (selectedTab.Tag is not string path)
            {
                return;
            }
            if (selectedTab.Content is not TextBox textBox)
            {
                return;
            }
            File.WriteAllText(path, textBox.Text);
        }
    }

    private void UpdateCounts(string text)
    {
        var wordCount = text.Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Length;
        var charCount = text.Length;

        WordCountTextBlock.Text = $"Words: {wordCount}";
        CharCountTextBlock.Text = $"Characters: {charCount}";
    }

    private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            UpdateCounts(textBox.Text);
        }
    }

    private void EditorTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EditorTabView.SelectedItem is TabViewItem selectedTab && selectedTab.Content is TextBox textBox)
        {
            UpdateCounts(textBox.Text);
        }
        else
        {
            UpdateCounts("");
        }
    }

    private void GridSplitter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        var fileTreeColumn = MainGrid.ColumnDefinitions[1];
        var newWidth = fileTreeColumn.ActualWidth + e.Delta.Translation.X;

        if (newWidth < fileTreeColumn.MinWidth)
        {
            newWidth = fileTreeColumn.MinWidth;
        }
        if (newWidth > fileTreeColumn.MaxWidth)
        {
            newWidth = fileTreeColumn.MaxWidth;
        }

        fileTreeColumn.Width = new GridLength(newWidth);
    }

    private void ToggleFileTreeButton_Click(object sender, RoutedEventArgs e)
    {
        var fileTreeColumn = MainGrid.ColumnDefinitions[1];
        if (FileTreeView.Visibility == Visibility.Visible)
        {
            _fileTreeColumnWidth = fileTreeColumn.ActualWidth;
            fileTreeColumn.MinWidth = 0;
            fileTreeColumn.Width = new GridLength(0);
            FileTreeView.Visibility = Visibility.Collapsed;
            GridSplitter.Visibility = Visibility.Collapsed;
        }
        else
        {
            fileTreeColumn.Width = new GridLength(_fileTreeColumnWidth);
            fileTreeColumn.MinWidth = _fileTreeColumnMinWidth;
            FileTreeView.Visibility = Visibility.Visible;
            GridSplitter.Visibility = Visibility.Visible;
        }
    }

    private void ToggleStatusBarItem_Click(object sender, RoutedEventArgs e)
    {
        WordCountTextBlock.Visibility = ToggleWordCountMenuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;
        CharCountTextBlock.Visibility = ToggleCharCountMenuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;
    }
}

