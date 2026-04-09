using Redskull.Class;
using Redskull.Theme;
using RedskullWPF.Class;
using Class;
using Other;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;

namespace Visuality
{
    /// <summary>
    /// Interaction logic for ConfigSaver.xaml
    /// </summary>
    public partial class ConfigSaver : Window
    {
        private Color ThemeGradientColor => ThemeManager.ThemeColorDark;

        public ConfigSaver()
        {
            InitializeComponent();

            //Every .xaml with a border named "MainBorder" gets changed as long as this is visible, so double check!
            ThemeManager.TrackWindow(this);

            // Initialize theme colors
            UpdateThemeColors();

            // Subscribe to theme changes
            ThemeManager.RegisterElement(this);
            ThemeManager.ThemeChanged += OnThemeChanged;
        }

        private void OnThemeChanged(object sender, Color newColor)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateThemeColors();
            });
        }

        private void UpdateThemeColors()
        {
            ThemeGradientStop.Color = ThemeGradientColor;
        }

        private void WriteJSON()
        {
            SaveDictionary.WriteJSON(Dictionary.sliderSettings
                                    .Concat(Dictionary.dropdownState)
                                    .Where(kvp => kvp.Key != "Screen Capture Method")
                                    .GroupBy(kvp => kvp.Key)
                                    .ToDictionary(g => g.Key, g => g
                                    .First().Value), $"bin\\configs\\{ConfigNameTextbox.Text}.cfg", RecommendedModelNameTextBox.Text);
            LogManager.Log(LogManager.LogLevel.Info, "บันทึกคอนฟิกไปที่ bin/configs แล้ว", true);
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists($"bin/configs/{ConfigNameTextbox.Text}.cfg") ||
                MessageBox.Show("มีคอนฟิกชื่อซ้ำอยู่แล้ว ต้องการเขียนทับหรือไม่?",
                    $"{Title} - บันทึกคอนฟิก", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                WriteJSON();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unregister from theme manager
            ThemeManager.ThemeChanged -= OnThemeChanged;
            ThemeManager.UnregisterElement(this);
            base.OnClosed(e);
        }

        #region Window Controls

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private double currentGradientAngle = 0;

        private void Main_Background_Gradient(object sender, MouseEventArgs e)
        {
            if (Dictionary.toggleState["Mouse Background Effect"])
            {
                var CurrentMousePos = WinAPICaller.GetCursorPosition();
                var translatedMousePos = PointFromScreen(new Point(CurrentMousePos.X, CurrentMousePos.Y));
                double targetAngle = Math.Atan2(translatedMousePos.Y - (MainBorder.ActualHeight * 0.5), translatedMousePos.X - (MainBorder.ActualWidth * 0.5)) * (180 / Math.PI);

                double angleDifference = (targetAngle - currentGradientAngle + 360) % 360;
                if (angleDifference > 180)
                {
                    angleDifference -= 360;
                }

                angleDifference = Math.Max(Math.Min(angleDifference, 1), -1); // Clamp the angle difference between -1 and 1 (smoothing)
                currentGradientAngle = (currentGradientAngle + angleDifference + 360) % 360;
                RotaryGradient.Angle = currentGradientAngle;
            }
        }

        #endregion Window Controls
    }
}

