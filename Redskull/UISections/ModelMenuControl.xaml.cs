using Redskull.Class;
using Other;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Visuality;

namespace Redskull.Controls
{
    public partial class ModelMenuControl : UserControl
    {
        private bool _isInitialized;

        public ModelMenuControl()
        {
            InitializeComponent();
        }

        public void Initialize(MainWindow mainWindow)
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
        }

        public ListBox ModelListBoxControl => ModelListBox;
        public Label SelectedModelNotifierControl => SelectedModelNotifier;
        public ListBox ConfigsListBoxControl => ConfigsListBox;
        public Label SelectedConfigNotifierControl => SelectedConfigNotifier;
        public ScrollViewer ModelMenuScrollViewer => ModelMenu;

        private void OpenFolderB_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedButton || clickedButton.Tag == null)
            {
                return;
            }

            try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "bin", clickedButton.Tag.ToString()!);
                Directory.CreateDirectory(path);
                Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                new NoticeBar(UILocalization.Translate($"Failed to open folder: {ex.Message}"), 5000).Show();
            }
        }

        private void LocalModelSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterListBoxBySearch(ModelListBox, (TextBox)sender);
        }

        private void LocalConfigSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterListBoxBySearch(ConfigsListBox, (TextBox)sender);
        }

        private static void FilterListBoxBySearch(ListBox listBox, TextBox searchBox)
        {
            var searchText = searchBox.Text ?? string.Empty;

            foreach (var item in listBox.Items)
            {
                if (listBox.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem listBoxItem)
                {
                    var contentText = item?.ToString() ?? string.Empty;
                    listBoxItem.Visibility = contentText.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
            }
        }

        private void ModelListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = HasOnlyExpectedExtension(e, ".onnx")
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void ConfigsListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
            e.Effects = HasOnlyExpectedExtension(e, ".cfg")
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void ModelListBox_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            ImportFiles(files, ".onnx", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "models"), "Model");
        }

        private void ConfigsListBox_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            ImportFiles(files, ".cfg", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "configs"), "Config");
        }

        private static bool HasOnlyExpectedExtension(DragEventArgs e, string expectedExtension)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return false;
            }

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            return files.All(file => Path.GetExtension(file).Equals(expectedExtension, StringComparison.OrdinalIgnoreCase));
        }

        private static void ImportFiles(IEnumerable<string> files, string extension, string destinationDirectory, string typeLabel)
        {
            Directory.CreateDirectory(destinationDirectory);

            foreach (var file in files)
            {
                if (!Path.GetExtension(file).Equals(extension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    var sourcePath = Path.GetFullPath(file);
                    var targetPath = GetUniqueDestinationPath(destinationDirectory, Path.GetFileName(file));

                    if (!string.Equals(sourcePath, targetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(sourcePath, targetPath, false);
                    }

                    new NoticeBar(UILocalization.Translate($"{typeLabel} imported: {Path.GetFileName(targetPath)}"), 3000).Show();
                }
                catch (Exception ex)
                {
                    new NoticeBar(UILocalization.Translate($"Error importing {typeLabel.ToLowerInvariant()}: {ex.Message}"), 5000).Show();
                }
            }
        }

        private static string GetUniqueDestinationPath(string destinationDirectory, string fileName)
        {
            var targetPath = Path.Combine(destinationDirectory, fileName);
            if (!File.Exists(targetPath))
            {
                return targetPath;
            }

            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var suffix = 1;

            do
            {
                targetPath = Path.Combine(destinationDirectory, $"{baseName}-IMPORTED-{suffix}{extension}");
                suffix++;
            }
            while (File.Exists(targetPath));

            return targetPath;
        }
    }
}

