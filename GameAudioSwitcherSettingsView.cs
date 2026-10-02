using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GameAudioSwitcher
{
    public class GameAudioSwitcherSettingsView : UserControl
    {
        private readonly GameAudioSwitcherPlugin plugin;

        public GameAudioSwitcherSettingsView(GameAudioSwitcherPlugin plugin)
        {
            this.plugin = plugin;
            Content = BuildUi();
        }

        private UIElement BuildUi()
        {
            var root = new StackPanel { Margin = new Thickness(10) };

            root.Children.Add(new TextBlock
            {
                Text = "Game Audio Switcher uses NirSoft SoundVolumeView to change the Windows HDMI speaker layout before Playnite launches a game.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });

            root.Children.Add(MakeLabel("SoundVolumeView.exe"));
            var pathRow = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
            var browse = new Button { Content = "Browse…", MinWidth = 80, Margin = new Thickness(8, 0, 0, 0) };
            DockPanel.SetDock(browse, Dock.Right);
            browse.Click += Browse_Click;
            pathRow.Children.Add(browse);
            var pathBox = new TextBox();
            pathBox.SetBinding(TextBox.TextProperty, TwoWay("SoundVolumeViewPath"));
            pathRow.Children.Add(pathBox);
            root.Children.Add(pathRow);

            root.Children.Add(MakeLabel("Audio device"));
            root.Children.Add(MakeBoundTextBox("AudioDevice", "DefaultRenderDevice is recommended", 10));

            root.Children.Add(MakeLabel("Dolby spatial provider name"));
            root.Children.Add(MakeBoundTextBox("AtmosFormatName", "Dolby Atmos for home theater", 14));

            root.Children.Add(MakeSectionHeader("Speaker configurations"));
            root.Children.Add(new TextBlock
            {
                Text = "The defaults are standard layouts. For maximum reliability, configure Windows manually to the desired layout once, then use Capture current.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });

            root.Children.Add(MakeConfigRow("Stereo", "StereoSpeakerConfig", AudioMode.Stereo));
            root.Children.Add(MakeConfigRow("5.1 PCM", "Surround51SpeakerConfig", AudioMode.Surround51));
            root.Children.Add(MakeConfigRow("7.1 PCM", "Surround71SpeakerConfig", AudioMode.Surround71));

            var atmosTest = new Button { Content = "Test Dolby Atmos", Margin = new Thickness(0, 4, 0, 14), MinWidth = 140, HorizontalAlignment = HorizontalAlignment.Left };
            atmosTest.Click += (s, e) => Test(AudioMode.Atmos);
            root.Children.Add(atmosTest);

            root.Children.Add(MakeSectionHeader("Automatic behaviour"));
            root.Children.Add(MakeLabel("Mode for games with no per-game override"));
            root.Children.Add(MakeModeCombo("DefaultGameMode", includeDefault: false));

            var restore = new CheckBox { Content = "Restore a desktop audio mode when the last running Playnite game closes", Margin = new Thickness(0, 10, 0, 8) };
            restore.SetBinding(CheckBox.IsCheckedProperty, TwoWay("RestoreAfterGame"));
            root.Children.Add(restore);

            root.Children.Add(MakeLabel("Desktop restore mode"));
            root.Children.Add(MakeModeCombo("DesktopMode", includeDefault: false));

            var notifications = new CheckBox { Content = "Show a Playnite notification when the audio mode changes", Margin = new Thickness(0, 10, 0, 0) };
            notifications.SetBinding(CheckBox.IsCheckedProperty, TwoWay("ShowNotifications"));
            root.Children.Add(notifications);

            return new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = root
            };
        }

        private FrameworkElement MakeConfigRow(string label, string bindingPath, AudioMode mode)
        {
            var panel = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var name = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(name, 0);
            panel.Children.Add(name);

            var box = new TextBox { Margin = new Thickness(8, 0, 8, 0), MinWidth = 180 };
            box.SetBinding(TextBox.TextProperty, TwoWay(bindingPath));
            Grid.SetColumn(box, 1);
            panel.Children.Add(box);

            var capture = new Button { Content = "Capture current", Margin = new Thickness(0, 0, 8, 0), Tag = bindingPath };
            capture.Click += Capture_Click;
            Grid.SetColumn(capture, 2);
            panel.Children.Add(capture);

            var test = new Button { Content = "Test", Tag = mode, MinWidth = 60 };
            test.Click += (s, e) => Test((AudioMode)((Button)s).Tag);
            Grid.SetColumn(test, 3);
            panel.Children.Add(test);

            return panel;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var settings = DataContext as GameAudioSwitcherSettings;
            if (settings == null)
            {
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = "Select SoundVolumeView.exe",
                Filter = "SoundVolumeView|SoundVolumeView.exe|Executable files|*.exe|All files|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                settings.SoundVolumeViewPath = dialog.FileName;
            }
        }

        private void Capture_Click(object sender, RoutedEventArgs e)
        {
            var settings = DataContext as GameAudioSwitcherSettings;
            var button = sender as Button;
            if (settings == null || button == null)
            {
                return;
            }

            var current = plugin.TryGetCurrentSpeakerConfig(out var error);
            if (string.IsNullOrWhiteSpace(current))
            {
                plugin.PlayniteApi.Dialogs.ShowErrorMessage(error ?? "Could not read the current speaker configuration.", "Game Audio Switcher");
                return;
            }

            var property = typeof(GameAudioSwitcherSettings).GetProperty(button.Tag as string);
            property?.SetValue(settings, current);
        }

        private void Test(AudioMode mode)
        {
            if (!plugin.ApplyAudioMode(mode, out var error))
            {
                plugin.PlayniteApi.Dialogs.ShowErrorMessage(error ?? "Audio mode switch failed.", "Game Audio Switcher");
            }
        }

        private static TextBlock MakeLabel(string text)
        {
            return new TextBlock { Text = text, Margin = new Thickness(0, 0, 0, 3) };
        }

        private static TextBlock MakeSectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 8, 0, 8)
            };
        }

        private static TextBox MakeBoundTextBox(string path, string tooltip, double bottomMargin)
        {
            var box = new TextBox { ToolTip = tooltip, Margin = new Thickness(0, 0, 0, bottomMargin) };
            box.SetBinding(TextBox.TextProperty, TwoWay(path));
            return box;
        }

        private static ComboBox MakeModeCombo(string path, bool includeDefault)
        {
            var combo = new ComboBox { Margin = new Thickness(0, 0, 0, 4), MinWidth = 180, HorizontalAlignment = HorizontalAlignment.Left };
            if (includeDefault)
            {
                combo.ItemsSource = new[] { AudioMode.Default, AudioMode.Stereo, AudioMode.Surround51, AudioMode.Surround71, AudioMode.Atmos };
            }
            else
            {
                combo.ItemsSource = new[] { AudioMode.Stereo, AudioMode.Surround51, AudioMode.Surround71, AudioMode.Atmos };
            }

            combo.SetBinding(ComboBox.SelectedItemProperty, TwoWay(path));
            return combo;
        }

        private static Binding TwoWay(string path)
        {
            return new Binding(path)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
        }
    }
}
