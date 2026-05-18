// CineSplashSettingsView.xaml.cs
// Originally authored by: Artzox (https://github.com/artzox/Playnite-Splash-Addon)
// Fork: VibeSplash by EvoShot (https://github.com/EvoShot/Playnite-Splash-Addon-VibeSplash)
// Fork: CineSplash with video splash support

using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using CineSplash;

namespace CineSplash
{
    public partial class CineSplashSettingsView : UserControl
    {
        public CineSplashSettingsView()
        {
            InitializeComponent();
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
