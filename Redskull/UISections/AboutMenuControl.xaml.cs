using System.Windows.Controls;

namespace Redskull.Controls
{
    public partial class AboutMenuControl : UserControl
    {
        private bool _isInitialized;

        public Label AboutSpecsControl => AboutSpecs;
        public ScrollViewer AboutMenuScrollViewer => AboutMenu;

        public AboutMenuControl()
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
    }
}

