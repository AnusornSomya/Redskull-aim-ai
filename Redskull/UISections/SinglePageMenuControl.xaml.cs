using Redskull.Class;
using Redskull.MouseMovementLibraries.GHubSupport;
using Redskull.UILibrary;
using Class;
using InputLogic;
using Microsoft.Win32;
using MouseMovementLibraries.ddxoftSupport;
using MouseMovementLibraries.RazerSupport;
using Other;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UILibrary;
using Visuality;

namespace Redskull.Controls
{
    public partial class SinglePageMenuControl : UserControl
    {
        private const string DefaultModelName = "1.5kR6.onnx";

        private MainWindow? _mainWindow;
        private ModelMenuControl? _modelMenu;
        private INotifyCollectionChanged? _modelItemsCollection;
        private bool _isInitialized;
        private bool _isSyncingModelSelection;

        public ScrollViewer MainScrollViewer => SinglePageScrollViewerControl;

        public SinglePageMenuControl()
        {
            InitializeComponent();
        }

        public void Initialize(MainWindow mainWindow, ModelMenuControl modelMenu)
        {
            if (_isInitialized)
                return;

            _mainWindow = mainWindow;
            _modelMenu = modelMenu;
            _isInitialized = true;

            BuildSections();
            HookModelSelection();
            RefreshModelSelector();
            UpdateCurrentModelBadge();
        }

        public void EnsureInitialModelLoaded()
        {
            if (_modelMenu == null)
                return;

            RefreshModelSelector();

            var availableModels = ModelSelector.Items.Cast<string>().ToList();
            if (availableModels.Count == 0)
            {
                UpdateCurrentModelBadge();
                return;
            }

            string preferredModel;
            if (Dictionary.lastLoadedModel != "N/A" && availableModels.Contains(Dictionary.lastLoadedModel))
            {
                preferredModel = Dictionary.lastLoadedModel;
            }
            else if (availableModels.Contains(DefaultModelName))
            {
                preferredModel = DefaultModelName;
            }
            else
            {
                preferredModel = availableModels[0];
            }

            if (_modelMenu.ModelListBoxControl.SelectedItem?.ToString() != preferredModel)
            {
                _modelMenu.ModelListBoxControl.SelectedItem = preferredModel;
            }

            SelectModelInSelector(preferredModel);
            UpdateCurrentModelBadge();
        }

        public void Dispose()
        {
            if (_modelMenu != null)
            {
                _modelMenu.ModelListBoxControl.SelectionChanged -= HiddenModelListBox_SelectionChanged;
            }

            if (_modelItemsCollection != null)
            {
                _modelItemsCollection.CollectionChanged -= HiddenModelItems_CollectionChanged;
            }

            ModelSelector.SelectionChanged -= ModelSelector_SelectionChanged;
        }

        private void BuildSections()
        {
            LoadAimAssist();
            LoadPrediction();
            LoadAimConfig();
        }

        private void HookModelSelection()
        {
            if (_modelMenu == null)
                return;

            _modelMenu.ModelListBoxControl.SelectionChanged += HiddenModelListBox_SelectionChanged;

            _modelItemsCollection = _modelMenu.ModelListBoxControl.Items;
            _modelItemsCollection.CollectionChanged += HiddenModelItems_CollectionChanged;
        }

        private void HiddenModelItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                RefreshModelSelector();
                UpdateCurrentModelBadge();
            });
        }

        private void HiddenModelListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_modelMenu?.ModelListBoxControl.SelectedItem is not string selectedModel)
                return;

            SelectModelInSelector(selectedModel);
            UpdateCurrentModelBadge();
        }

        private void RefreshModelSelector()
        {
            if (_modelMenu == null)
                return;

            var currentSelection = ModelSelector.SelectedItem as string
                                   ?? _modelMenu.ModelListBoxControl.SelectedItem?.ToString()
                                   ?? (Dictionary.lastLoadedModel != "N/A" ? Dictionary.lastLoadedModel : null);

            _isSyncingModelSelection = true;
            ModelSelector.Items.Clear();

            foreach (var modelName in _modelMenu.ModelListBoxControl.Items.Cast<object>()
                         .Select(item => item?.ToString())
                         .Where(item => !string.IsNullOrWhiteSpace(item)))
            {
                ModelSelector.Items.Add(modelName!);
            }

            _isSyncingModelSelection = false;

            if (!string.IsNullOrWhiteSpace(currentSelection) &&
                ModelSelector.Items.Cast<string>().Contains(currentSelection))
            {
                SelectModelInSelector(currentSelection);
            }
        }

        private void SelectModelInSelector(string modelName)
        {
            _isSyncingModelSelection = true;
            ModelSelector.SelectedItem = modelName;
            _isSyncingModelSelection = false;
        }

        private void UpdateCurrentModelBadge()
        {
            var loadedModel = Dictionary.lastLoadedModel != "N/A"
                ? Dictionary.lastLoadedModel
                : "ยังไม่ได้โหลดโมเดล";

            CurrentModelText.Text = loadedModel;
        }

        private void LoadAimAssist()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, LeftColumn);

            builder
                .AddTitle("Aim Assist")
                .AddToggle("Aim Assist", t =>
                {
                    uiManager.T_AimAligner = t;
                    t.Reader.Click += (s, e) =>
                    {
                        if (Dictionary.toggleState["Aim Assist"] && Dictionary.lastLoadedModel == "N/A")
                        {
                            Dictionary.toggleState["Aim Assist"] = false;
                            _mainWindow.UpdateToggleUI(t, false);
                            LogManager.Log(LogManager.LogLevel.Warning, "Please load a model first", true);
                        }
                    };
                }, "Turn aim assist on or off. You must load a model first.")
                .AddToggle("Sticky Aim", t => uiManager.T_StickyAim = t,
                    "Lock onto a target until it moves out of range instead of switching targets.")
                .AddToggle("Auto Trigger", t => uiManager.T_AutoTrigger = t,
                    "Automatically click when a target is detected in your crosshair area.")
                .AddSlider("Sticky Aim Threshold", "Pixels", 1, 1, 0, 100, s =>
                {
                    uiManager.S_StickyAimThreshold = s;
                    s.Visibility = Dictionary.toggleState["Sticky Aim"]
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }, "How far a target must move before switching to a new one. Higher = stays locked longer.")
                .AddKeyChanger("Aim Keybind", k => uiManager.C_Keybind = k,
                    tooltip: "The key you hold to activate aim assist.")
                .AddKeyChanger("Second Aim Keybind",
                    tooltip: "An alternate key to activate aim assist.")
                .AddSeparator();
        }

        private void LoadPrediction()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, LeftColumn);

            builder
                .AddTitle("Predictions")
                .AddToggle("Predictions", t => uiManager.T_Predictions = t,
                    "Predict where a moving target will be. Helps track fast-moving targets.")
                .AddDropdown("Prediction Method", d =>
                {
                    d.DropdownBox.SelectedIndex = -1;
                    uiManager.D_PredictionMethod = d;

                    _mainWindow.AddDropdownItem(d, "Kalman Filter");
                    _mainWindow.AddDropdownItem(d, "Shall0e's Prediction");
                    _mainWindow.AddDropdownItem(d, "wisethef0x's EMA Prediction");

                    d.DropdownBox.SelectionChanged += (s, e) => _mainWindow.UpdatePredictionSliderVisibility();
                }, "The algorithm used to predict target movement. Try different ones to see what works best.")
                .AddSlider("Kalman Lead Time", "Seconds", 0.01, 0.01, 0.02, 0.30, s =>
                {
                    uiManager.S_KalmanLeadTime = s;
                    s.Visibility = Visibility.Collapsed;
                }, "How far ahead to predict target position. Higher = more prediction, may overshoot.")
                .AddSlider("WiseTheFox Lead Time", "Seconds", 0.01, 0.01, 0.02, 0.30, s =>
                {
                    uiManager.S_WiseTheFoxLeadTime = s;
                    s.Visibility = Visibility.Collapsed;
                }, "How far ahead to predict target position. Higher = more prediction, may overshoot.")
                .AddSlider("Shalloe Lead Multiplier", "Frames", 0.5, 0.5, 1, 10, s =>
                {
                    uiManager.S_ShalloeLeadMultiplier = s;
                    s.Visibility = Visibility.Collapsed;
                }, "How many frames ahead to predict. Higher = more prediction, may overshoot.")
                .AddToggle("EMA Smoothening", t => uiManager.T_EMASmoothing = t,
                    "Smooth out aim movements to reduce jitter and make tracking steadier.")
                .AddSlider("EMA Smoothening", "Amount", 0.01, 0.01, 0.01, 1, s =>
                {
                    uiManager.S_EMASmoothing = s;
                    s.Slider.ValueChanged += (sender, e) =>
                    {
                        if (Dictionary.toggleState["EMA Smoothening"])
                        {
                            MouseManager.smoothingFactor = s.Slider.Value;
                        }
                    };
                }, "How much smoothing to apply. Lower = smoother but slower, higher = faster but jittery.")
                .AddSeparator();
        }

        private void LoadAimConfig()
        {
            var uiManager = _mainWindow!.uiManager;
            var builder = new SectionBuilder(this, RightColumn);

            Dictionary.EnforceLockedSelections();
            uiManager.D_DetectionAreaType = null;
            uiManager.D_AimingBoundariesAlignment = null;
            uiManager.DDI_ClosestToCenterScreen = null;

            builder
                .AddTitle("Aim Config")
                .AddDropdown("Mouse Movement Method", d =>
                {
                    uiManager.D_MouseMovementMethod = d;
                    d.DropdownBox.SelectedIndex = -1;

                    _mainWindow.AddDropdownItem(d, "Mouse Event");
                    _mainWindow.AddDropdownItem(d, "SendInput");
                    uiManager.DDI_LGHUB = _mainWindow.AddDropdownItem(d, "LG HUB");
                    uiManager.DDI_RazerSynapse = _mainWindow.AddDropdownItem(d, "Razer Synapse (Require Razer Peripheral)");
                    uiManager.DDI_ddxoft = _mainWindow.AddDropdownItem(d, "ddxoft Virtual Input Driver");

                    uiManager.DDI_LGHUB.Selected += async (s, e) =>
                    {
                        if (!new LGHubMain().Load())
                        {
                            await ResetToMouseEvent();
                        }
                    };

                    uiManager.DDI_RazerSynapse.Selected += async (s, e) =>
                    {
                        if (!await RZMouse.Load())
                        {
                            await ResetToMouseEvent();
                        }
                    };

                    uiManager.DDI_ddxoft.Selected += async (s, e) =>
                    {
                        if (!await DdxoftMain.Load())
                        {
                            await ResetToMouseEvent();
                        }
                    };
                }, "How mouse movements are sent. Try different options if aim assist isn't working.")
                .AddToggle("Show Detected Player", t => uiManager.T_ShowDetectedPlayer = t,
                    "Draw a box around detected targets on screen.")
                .AddToggle("Show AI Confidence", t => uiManager.T_ShowAIConfidence = t,
                    "Display how confident the AI is about each detection (0-100%).")
                .AddToggle("Show Tracers", t => uiManager.T_ShowTracers = t,
                    "Draw lines from screen edge to detected targets.")
                .AddSlider("Mouse Sensitivity (+/-)", "Sensitivity", 0.01, 0.01, 0.01, 1, s =>
                {
                    uiManager.S_MouseSensitivity = s;
                    s.Slider.PreviewMouseLeftButtonUp += (sender, e) =>
                    {
                        var value = s.Slider.Value;
                        if (value >= 0.98)
                        {
                            LogManager.Log(LogManager.LogLevel.Warning,
                                "The Mouse Sensitivity you have set can cause REDSKULL AIM AI to be unable to aim, please decrease if you suffer from this problem", true);
                        }
                        else if (value <= 0.1)
                        {
                            LogManager.Log(LogManager.LogLevel.Warning,
                                "The Mouse Sensitivity you have set can cause REDSKULL AIM AI to be unstable to aim, please increase if you suffer from this problem", true);
                        }
                    };
                }, "How fast the aim moves. Lower = faster and snappier, higher = slower and smoother.")
                .AddSlider("Mouse Jitter", "Jitter", 1, 1, 0, 15, s => uiManager.S_MouseJitter = s,
                    "Adds random small movements to make aim look more human-like.")
                .AddToggle("Y Axis Percentage Adjustment", t => uiManager.T_YAxisPercentageAdjustment = t,
                    "Enable the Y Offset (%) slider to adjust aim vertically by percentage.")
                .AddToggle("X Axis Percentage Adjustment", t => uiManager.T_XAxisPercentageAdjustment = t,
                    "Enable the X Offset (%) slider to adjust aim horizontally by percentage.")
                .AddSlider("Y Offset (Up/Down)", "Offset", 1, 1, -150, 150, s =>
                {
                    uiManager.S_YOffset = s;
                    s.Visibility = Dictionary.toggleState["Y Axis Percentage Adjustment"]
                        ? Visibility.Collapsed
                        : Visibility.Visible;
                }, "Move aim point up (negative) or down (positive) in pixels.")
                .AddSlider("Y Offset (%)", "Percent", 1, 1, 0, 100, s =>
                {
                    uiManager.S_YOffsetPercent = s;
                    s.Visibility = Dictionary.toggleState["Y Axis Percentage Adjustment"]
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }, "Move aim point up or down as a percentage of the target box height.")
                .AddSlider("X Offset (Left/Right)", "Offset", 1, 1, -150, 150, s =>
                {
                    uiManager.S_XOffset = s;
                    s.Visibility = Dictionary.toggleState["X Axis Percentage Adjustment"]
                        ? Visibility.Collapsed
                        : Visibility.Visible;
                }, "Move aim point left (negative) or right (positive) in pixels.")
                .AddSlider("X Offset (%)", "Percent", 1, 1, 0, 100, s =>
                {
                    uiManager.S_XOffsetPercent = s;
                    s.Visibility = Dictionary.toggleState["X Axis Percentage Adjustment"]
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }, "Move aim point left or right as a percentage of the target box width.")
                .AddSeparator();
        }

        private async Task ResetToMouseEvent()
        {
            await Task.Delay(500);
            _mainWindow!.uiManager.D_MouseMovementMethod!.DropdownBox.SelectedIndex = 0;
        }

        private void ImportModelButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "ONNX Model (*.onnx)|*.onnx",
                Multiselect = false,
                Title = "เลือกไฟล์โมเดล"
            };

            if (dialog.ShowDialog() == true)
            {
                ImportModel(dialog.FileName);
            }
        }

        private void OpenModelsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var modelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "models");
                Directory.CreateDirectory(modelsPath);
                Process.Start("explorer.exe", modelsPath);
            }
            catch (Exception ex)
            {
                new NoticeBar($"เปิดโฟลเดอร์โมเดลไม่สำเร็จ: {ex.Message}", 4000).Show();
            }
        }

        private void ModelDropZone_DragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            e.Effects = files.All(file => Path.GetExtension(file).Equals(".onnx", StringComparison.OrdinalIgnoreCase))
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void ModelDropZone_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var firstModel = files.FirstOrDefault(file => Path.GetExtension(file).Equals(".onnx", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(firstModel))
            {
                ImportModel(firstModel);
            }
        }

        private void ImportModel(string sourceFile)
        {
            try
            {
                if (!Path.GetExtension(sourceFile).Equals(".onnx", StringComparison.OrdinalIgnoreCase))
                {
                    new NoticeBar("รองรับเฉพาะไฟล์ .onnx", 3000).Show();
                    return;
                }

                var destinationDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "models");
                Directory.CreateDirectory(destinationDirectory);

                var fileName = Path.GetFileName(sourceFile);
                var destinationPath = Path.Combine(destinationDirectory, fileName);

                if (!Path.GetFullPath(sourceFile).Equals(Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(sourceFile, destinationPath, true);
                }

                _mainWindow?.fileManager.LoadModelsIntoListBox(null, null);
                RefreshModelSelector();

                if (_modelMenu != null)
                {
                    _modelMenu.ModelListBoxControl.SelectedItem = fileName;
                }

                new NoticeBar($"นำเข้าโมเดลแล้ว: {fileName}", 3000).Show();
            }
            catch (Exception ex)
            {
                new NoticeBar($"นำเข้าโมเดลไม่สำเร็จ: {ex.Message}", 4000).Show();
            }
        }

        private void ModelSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isSyncingModelSelection || _modelMenu == null)
                return;

            if (ModelSelector.SelectedItem is not string selectedModel)
                return;

            _modelMenu.ModelListBoxControl.SelectedItem = selectedModel;
            UpdateCurrentModelBadge();
        }

        private class SectionBuilder
        {
            private readonly SinglePageMenuControl _parent;
            private readonly StackPanel _panel;

            public SectionBuilder(SinglePageMenuControl parent, StackPanel panel)
            {
                _parent = parent;
                _panel = panel;
            }

            public SectionBuilder AddTitle(string title)
            {
                _panel.Children.Add(new ATitle(title));
                return this;
            }

            public SectionBuilder AddToggle(string title, Action<AToggle>? configure = null, string? tooltip = null)
            {
                var toggle = _parent.CreateToggle(title, tooltip);
                configure?.Invoke(toggle);
                _panel.Children.Add(toggle);
                return this;
            }

            public SectionBuilder AddKeyChanger(string title, Action<AKeyChanger>? configure = null, string? defaultKey = null, string? tooltip = null)
            {
                var key = defaultKey ?? Dictionary.bindingSettings[title];
                var keyChanger = _parent.CreateKeyChanger(title, key, tooltip);
                configure?.Invoke(keyChanger);
                _panel.Children.Add(keyChanger);
                return this;
            }

            public SectionBuilder AddSlider(string title, string label, double frequency, double buttonSteps,
                double min, double max, Action<ASlider>? configure = null, string? tooltip = null)
            {
                var slider = _parent.CreateSlider(title, label, frequency, buttonSteps, min, max, tooltip);
                configure?.Invoke(slider);
                _panel.Children.Add(slider);
                return this;
            }

            public SectionBuilder AddDropdown(string title, Action<ADropdown>? configure = null, string? tooltip = null)
            {
                var dropdown = _parent.CreateDropdown(title, tooltip);
                configure?.Invoke(dropdown);
                _panel.Children.Add(dropdown);
                return this;
            }

            public SectionBuilder AddSeparator()
            {
                _panel.Children.Add(new ARectangleBottom());
                _panel.Children.Add(new ASpacer());
                return this;
            }
        }

        private AToggle CreateToggle(string title, string? tooltip = null)
        {
            var toggle = new AToggle(title, tooltip);
            _mainWindow!.toggleInstances[title] = toggle;

            if (Dictionary.toggleState[title])
            {
                toggle.EnableSwitch();
            }
            else
            {
                toggle.DisableSwitch();
            }

            toggle.Reader.Click += (sender, e) =>
            {
                Dictionary.toggleState[title] = !Dictionary.toggleState[title];
                _mainWindow.UpdateToggleUI(toggle, Dictionary.toggleState[title]);
                _mainWindow.Toggle_Action(title);
            };

            return toggle;
        }

        private AKeyChanger CreateKeyChanger(string title, string keybind, string? tooltip = null)
        {
            var keyChanger = new AKeyChanger(title, keybind, tooltip);

            keyChanger.Reader.Click += (sender, e) =>
            {
                keyChanger.KeyNotifier.Content = "...";
                _mainWindow!.bindingManager.StartListeningForBinding(title);

                Action<string, string>? bindingSetHandler = null;
                bindingSetHandler = (bindingId, key) =>
                {
                    if (bindingId == title)
                    {
                        keyChanger.KeyNotifier.Content = KeybindNameManager.ConvertToRegularKey(key);
                        Dictionary.bindingSettings[bindingId] = key;
                        _mainWindow.bindingManager.OnBindingSet -= bindingSetHandler;
                    }
                };

                _mainWindow.bindingManager.OnBindingSet += bindingSetHandler;
            };

            return keyChanger;
        }

        private ASlider CreateSlider(string title, string label, double frequency, double buttonSteps,
            double min, double max, string? tooltip = null)
        {
            var slider = new ASlider(title, label, buttonSteps, tooltip)
            {
                Slider = { Minimum = min, Maximum = max, TickFrequency = frequency }
            };

            slider.Slider.Value = Dictionary.sliderSettings.TryGetValue(title, out var value)
                ? Convert.ToDouble(value)
                : min;
            slider.Slider.ValueChanged += (s, e) => Dictionary.sliderSettings[title] = slider.Slider.Value;

            return slider;
        }

        private ADropdown CreateDropdown(string title, string? tooltip = null) => new(title, title, tooltip);
    }
}

