// CineSplashSettingsView.xaml.cs
// Originally authored by: Artzox (https://github.com/artzox/Playnite-Splash-Addon)
// Fork: VibeSplash by EvoShot (https://github.com/EvoShot/Playnite-Splash-Addon-VibeSplash)
// Fork: CineSplash with video splash support

using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using CineSplash;

namespace CineSplash
{
    public partial class CineSplashSettingsView : UserControl
    {
        public CineSplashSettingsView()
        {
            InitializeComponent();
            this.Unloaded += UserControl_Unloaded;
        }

        private bool _isRecording = false;

        private void RecordHotkeyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isRecording) return;
            
            var window = Window.GetWindow(this);
            if (window == null) return;

            _isRecording = true;
            RecordHotkeyBtn.Content = "Listening...";
            
            window.PreviewKeyDown -= Window_PreviewKeyDown;
            window.PreviewKeyDown += Window_PreviewKeyDown;
        }



        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_isRecording) return;
            
            var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
            
            // Ignore modifier keys by themselves
            if (key == System.Windows.Input.Key.LeftCtrl || key == System.Windows.Input.Key.RightCtrl ||
                key == System.Windows.Input.Key.LeftAlt || key == System.Windows.Input.Key.RightAlt ||
                key == System.Windows.Input.Key.LeftShift || key == System.Windows.Input.Key.RightShift ||
                key == System.Windows.Input.Key.LWin || key == System.Windows.Input.Key.RWin)
            {
                return;
            }
            
            e.Handled = true;

            if (DataContext is CineSplashSettings settings)
            {
                if (key == System.Windows.Input.Key.Back || key == System.Windows.Input.Key.Delete)
                {
                    settings.SkipHotkey = System.Windows.Input.Key.None;
                    settings.SkipHotkeyModifiers = System.Windows.Input.ModifierKeys.None;
                }
                else
                {
                    settings.SkipHotkey = key;
                    settings.SkipHotkeyModifiers = System.Windows.Input.Keyboard.Modifiers;
                }
            }
            
            StopRecording();
        }
        
        private void StopRecording()
        {
            _isRecording = false;
            RecordHotkeyBtn.Content = "Record Hotkey";
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewKeyDown -= Window_PreviewKeyDown;
            }
        }

        // ── Calibration hotkey recording ──────────────────────────────
        private bool _isRecordingCalibration = false;

        private void RecordCalibrationHotkeyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isRecordingCalibration) return;
            var window = Window.GetWindow(this);
            if (window == null) return;

            _isRecordingCalibration = true;
            RecordCalibrationHotkeyBtn.Content = "Listening...";
            window.PreviewKeyDown += Window_CalibrationKeyDown;
        }

        private void Window_CalibrationKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_isRecordingCalibration) return;

            var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;

            // Ignore modifier keys by themselves
            if (key == System.Windows.Input.Key.LeftCtrl || key == System.Windows.Input.Key.RightCtrl ||
                key == System.Windows.Input.Key.LeftAlt || key == System.Windows.Input.Key.RightAlt ||
                key == System.Windows.Input.Key.LeftShift || key == System.Windows.Input.Key.RightShift ||
                key == System.Windows.Input.Key.LWin || key == System.Windows.Input.Key.RWin)
                return;

            e.Handled = true;

            if (DataContext is CineSplashSettings settings)
            {
                if (key == System.Windows.Input.Key.Back || key == System.Windows.Input.Key.Delete)
                {
                    settings.CalibrationHotkey = System.Windows.Input.Key.None;
                    settings.CalibrationHotkeyModifiers = System.Windows.Input.ModifierKeys.None;
                }
                else
                {
                    settings.CalibrationHotkey = key;
                    settings.CalibrationHotkeyModifiers = System.Windows.Input.Keyboard.Modifiers;
                }
            }

            _isRecordingCalibration = false;
            RecordCalibrationHotkeyBtn.Content = "Record Hotkey";
            var window = Window.GetWindow(this);
            if (window != null)
                window.PreviewKeyDown -= Window_CalibrationKeyDown;
        }

        // ── Table buttons ─────────────────────────────────────────────
        private void Recalibrate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string gameId &&
                DataContext is CineSplashSettings settings)
            {
                settings.RequestRecalibration(gameId);
            }
        }

        private void ClearCalibration_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string gameId &&
                DataContext is CineSplashSettings settings)
            {
                settings.ClearCalibration(gameId);
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StopRecording();
            
            if (_isRecordingCalibration)
            {
                _isRecordingCalibration = false;
                RecordCalibrationHotkeyBtn.Content = "Record Hotkey";
                var window = Window.GetWindow(this);
                if (window != null)
                    window.PreviewKeyDown -= Window_CalibrationKeyDown;
            }
        }
    }

    /// <summary>
    /// Converts a <see cref="VideoSourcePreference"/> enum value to a human-readable
    /// string for display in the settings ComboBox.
    /// </summary>
    public class VideoSourceDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is VideoSourcePreference pref)
            {
                if (pref == VideoSourcePreference.TrailerWithMicroFallback)       return "Trailer  \u2192  fallback: Microtrailer";
                if (pref == VideoSourcePreference.MicrotrailerWithTrailerFallback) return "Microtrailer  \u2192  fallback: Trailer";
                if (pref == VideoSourcePreference.TrailerOnly)                    return "Trailer only (no fallback)";
                if (pref == VideoSourcePreference.MicrotrailerOnly)               return "Microtrailer only (no fallback)";
                return pref.ToString();
            }
            return value != null ? value.ToString() : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
