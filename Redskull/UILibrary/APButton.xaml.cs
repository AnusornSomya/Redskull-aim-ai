using Redskull.Class;

namespace Redskull.UILibrary
{
    /// <summary>
    /// Interaction logic for APButton.xaml
    /// </summary>
    public partial class APButton : System.Windows.Controls.UserControl
    {
        public APButton(string Text, string? tooltip = null)
        {
            InitializeComponent();
            ButtonTitle.Content = UILocalization.Translate(Text);

            if (!string.IsNullOrEmpty(tooltip))
            {
                var tt = new System.Windows.Controls.ToolTip { Content = UILocalization.Translate(tooltip) };
                if (TryFindResource("Tooltip") is System.Windows.Style style)
                    tt.Style = style;
                ToolTip = tt;
            }
        }
    }
}


