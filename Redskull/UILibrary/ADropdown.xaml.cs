using Redskull.Class;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;

namespace UILibrary
{
    /// <summary>
    /// Interaction logic for ADropdown.xaml
    /// </summary>
    public partial class ADropdown : UserControl
    {
        private string main_dictionary_path { get; set; }
        public string DictionaryPath => main_dictionary_path;

        public ADropdown(string title, string dictionary_path, string? tooltip = null)
        {
            InitializeComponent();
            DropdownTitle.Content = UILocalization.Translate(title);
            main_dictionary_path = dictionary_path;

            if (!string.IsNullOrEmpty(tooltip))
            {
                var tt = new System.Windows.Controls.ToolTip { Content = UILocalization.Translate(tooltip) };
                if (TryFindResource("Tooltip") is System.Windows.Style style)
                    tt.Style = style;
                ToolTip = tt;
            }
        }

        private void DropdownBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedComboBoxItem = DropdownBox.SelectedItem as ComboBoxItem;
            var selectedItemContent = selectedComboBoxItem?.Tag?.ToString()
                                      ?? selectedComboBoxItem?.Content?.ToString();
            if (selectedItemContent != null)
            {
                Dictionary.dropdownState[main_dictionary_path] = selectedItemContent;
            }
        }
    }
}

