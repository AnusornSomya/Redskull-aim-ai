using Other;

using Redskull.Class;

namespace Redskull.UILibrary
{
    /// <summary>
    /// Interaction logic for AKeyChanger.xaml
    /// </summary>
    public partial class AKeyChanger : System.Windows.Controls.UserControl
    {
        public AKeyChanger(string Text, string Keybind, string? tooltip = null)
        {
            InitializeComponent();
            KeyChangerTitle.Content = UILocalization.Translate(Text);

            if (!string.IsNullOrEmpty(tooltip))
            {
                var tt = new System.Windows.Controls.ToolTip { Content = UILocalization.Translate(tooltip) };
                if (TryFindResource("Tooltip") is System.Windows.Style style)
                    tt.Style = style;
                ToolTip = tt;
            }

            KeyNotifier.Content = KeybindNameManager.ConvertToRegularKey(Keybind);
        }
    }
}

