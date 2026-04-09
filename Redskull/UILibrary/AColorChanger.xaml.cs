using Redskull.Class;

namespace Redskull.UILibrary
{
    /// <summary>
    /// Interaction logic for AColorChanger.xaml
    /// </summary>
    public partial class AColorChanger : System.Windows.Controls.UserControl
    {
        public AColorChanger(string title)
        {
            InitializeComponent();
            ColorChangerTitle.Content = UILocalization.Translate(title);
        }
    }
}

