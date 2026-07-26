// CineSplash.cs
// Originally authored by: Artzox (https://github.com/artzox/Playnite-Splash-Addon)
// Fork: VibeSplash by EvoShot (https://github.com/EvoShot/Playnite-Splash-Addon-VibeSplash)
// Fork: CineSplash with video splash support
// ⚠️ VIBE CODED — written with AI assistance. May contain vibes. Use at your own risk.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace CineSplash
{
    public class CineSplashPlugin : GenericPlugin
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private CineSplashSettings _settings;
        private DateTime _gameStartTimestamp;

        private Game _currentGame;
        private Window _currentSplashWindow;
        private System.Windows.Threading.DispatcherTimer _windowPollTimer;
        private System.Windows.Threading.DispatcherTimer _maxTimeoutTimer;
        private System.Windows.Threading.DispatcherTimer _elapsedTimer;
        private DateTime _splashOpenTimestamp;
        private bool _isManualCalibrationMode;
        private HashSet<uint> _preExistingPids;

        private enum CalibrationMode
        {
            None,
            Auto,
            Manual
        }

        // Static instance to allow Settings View to access settings if needed
        public static CineSplashPlugin Instance { get; private set; }

        public override Guid Id { get; } = Guid.Parse("7cdfb7ff-6328-4b71-962d-53f95593b7a9");

        public CineSplashPlugin(IPlayniteAPI api) : base(api)
        {
            Instance = this;
            _settings = LoadPluginSettings<CineSplashSettings>() ?? new CineSplashSettings();
            Properties = new GenericPluginProperties { HasSettings = true };
        }

        public override ISettings GetSettings(bool firstRunSettings) => _settings;

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            var view = new CineSplashSettingsView();
            view.DataContext = _settings;
            return view;
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            SavePluginSettings(_settings);
            base.OnApplicationStopped(args);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the root folder used by Extra Metadata Loader, accounting for
        /// portable vs. installed mode.
        /// Portable:     {PlayniteDir}\ExtraMetadata
        /// Non-portable: %AppData%\Playnite\ExtraMetadata
        /// </summary>
        private string GetExtraMetadataRoot()
        {
            string basePath = PlayniteApi.Paths.IsPortable
                ? PlayniteApi.Paths.ApplicationPath
                : PlayniteApi.Paths.ConfigurationPath;
            return Path.Combine(basePath, "ExtraMetadata");
        }

        private string GetExtraMetadataGameDir(Guid gameId)
            => Path.Combine(GetExtraMetadataRoot(), "Games", gameId.ToString());

        private bool IsSplashBlockedByMode()
        {
            bool isFullscreen = PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen;
            if (isFullscreen && _settings.DisableInFullscreen) return true;
            if (!isFullscreen && _settings.DisableInDesktop) return true;
            return false;
        }

        // ─── Event handlers ───────────────────────────────────────────────────────
        
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            return new List<GameMenuItem>
            {
                new GameMenuItem
                {
                    Description = "Recalibrate Window Detection",
                    MenuSection = "CineSplash",
                    Action = a =>
                    {
                        foreach (var game in args.Games)
                        {
                            _settings.RequestRecalibration(game.Id.ToString(), game.Name);
                        }
                        SavePluginSettings(_settings);
                        PlayniteApi.Dialogs.ShowMessage("The next time you launch the selected game(s), CineSplash will open in manual calibration mode.", "CineSplash Calibration");
                    }
                }
            };
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            _gameStartTimestamp = DateTime.Now;
            _splashOpenTimestamp = DateTime.Now;
            _currentGame = args.Game;

            _preExistingPids = new HashSet<uint>();
            foreach (var proc in System.Diagnostics.Process.GetProcesses())
            {
                try { _preExistingPids.Add((uint)proc.Id); } catch { }
            }

            if (IsSplashBlockedByMode()) return;

            string gameId = args.Game.Id.ToString();

            if (_settings.EnableWindowDetection)
            {
                if (_settings.IsPendingRecalibration(gameId))
                {
                    _isManualCalibrationMode = true;
                    ShowSplashScreen(args.Game, 0, false, CalibrationMode.Manual);
                    StartMaxTimeout(_currentSplashWindow);
                }
                else
                {
                    string savedTitle = _settings.GetWindowTitleForGame(gameId);
                    if (savedTitle != null)
                    {
                        _isManualCalibrationMode = false;
                        ShowSplashScreen(args.Game, 0, false, CalibrationMode.None);
                        StartWindowTitlePolling(savedTitle, _currentSplashWindow);
                        StartMaxTimeout(_currentSplashWindow);
                    }
                    else
                    {
                        _isManualCalibrationMode = false;
                        ShowSplashScreen(args.Game, 0, false, CalibrationMode.Auto);
                        StartMaxTimeout(_currentSplashWindow);
                    }
                }
            }
            else
            {
                _isManualCalibrationMode = false;
                if (_settings.UseGameStartedTimer)
                    ShowSplashScreen(args.Game, 0, false);
                else
                    ShowSplashScreen(args.Game,
                        _settings.GetDurationForGame(args.Game.Id.ToString(),
                            args.Game.Platforms?.FirstOrDefault()?.Name ?? string.Empty),
                        true);
            }
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            if (IsSplashBlockedByMode()) return;

            string gameId = args.Game.Id.ToString();

            if (_settings.EnableWindowDetection)
            {
                if (_settings.IsPendingRecalibration(gameId))
                {
                    // Manual recalibration - wait for user input
                }
                else if (_settings.GetWindowTitleForGame(gameId) == null)
                {
                    StartForegroundHookWithAutoSave(_currentSplashWindow, args.Game);
                }
            }
            else if (_settings.UseGameStartedTimer)
            {
                TimeSpan elapsed = DateTime.Now - _gameStartTimestamp;
                int remaining = _settings.GetDurationForGame(
                    args.Game.Id.ToString(),
                    args.Game.Platforms?.FirstOrDefault()?.Name ?? string.Empty)
                    - (int)elapsed.TotalSeconds;

                SetCloseTimer(remaining > 0 ? remaining : 0);
            }
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            if (_settings.ShowSplashOnGameClose && !IsSplashBlockedByMode())
            {
                ShowSplashScreen(args.Game,
                    _settings.GetDurationForGame(
                        args.Game.Id.ToString(),
                        args.Game.Platforms?.FirstOrDefault()?.Name ?? string.Empty),
                    true);
            }
            base.OnGameStopped(args);
        }

        // ─── Timer / close helpers ────────────────────────────────────────────────

        private void SetCloseTimer(int durationInSeconds)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var win = Application.Current.Windows
                    .OfType<Window>()
                    .FirstOrDefault(w => w.Title == "CineSplashScreen");
                if (win == null) return;

                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(durationInSeconds)
                };
                timer.Tick += (s, e) => { timer.Stop(); StopAllDetection(); FadeAndClose(win); };

                if (durationInSeconds > 0)
                    timer.Start();
                else
                {
                    StopAllDetection();
                    FadeAndClose(win);
                }
            });
        }

        private void FadeAndClose(Window window)
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1, To = 0,
                Duration = TimeSpan.FromSeconds(1)
            };
            Storyboard.SetTarget(fadeOut, window);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(Window.OpacityProperty));

            var sb = new Storyboard();
            sb.Children.Add(fadeOut);
            sb.Completed += (s, e) => { try { window.Close(); } catch { } };
            sb.Begin();
        }

        // ─── Detection Helpers ────────────────────────────────────────────────────
        
        private void StartWindowTitlePolling(string targetTitle, Window splashWindow)
        {
            _windowPollTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _windowPollTimer.Tick += (s, e) =>
            {
                if (WindowDetector.FindWindowByTitle(targetTitle, out _))
                {
                    _windowPollTimer.Stop();
                    StopAllDetection();
                    FadeAndClose(splashWindow);
                }
            };
            _windowPollTimer.Start();
        }

        private void StartForegroundHookWithAutoSave(Window splashWindow, Game game)
        {
            WindowDetector.StartForegroundHook(windowTitle =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    int elapsed = (int)(DateTime.Now - _splashOpenTimestamp).TotalSeconds;
                    _settings.SaveCalibration(game.Id.ToString(), game.Name, windowTitle, elapsed);
                    SavePluginSettings(_settings);
                    Logger.Info($"CineSplash: Auto-calibrated '{game.Name}' -> \"{windowTitle}\", {elapsed}s");

                    StopAllDetection();
                    FadeAndClose(splashWindow);
                });
            }, _preExistingPids);
        }

        private void StartMaxTimeout(Window splashWindow)
        {
            _maxTimeoutTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(_settings.MaxSplashDuration) };
            _maxTimeoutTimer.Tick += (s, e) =>
            {
                _maxTimeoutTimer.Stop();
                StopAllDetection();
                FadeAndClose(splashWindow);
                Logger.Warn("CineSplash: Max timeout reached, closing splash.");
            };
            _maxTimeoutTimer.Start();
        }

        private void StopAllDetection()
        {
            _windowPollTimer?.Stop();
            _maxTimeoutTimer?.Stop();
            _elapsedTimer?.Stop();
            WindowDetector.StopForegroundHook();
        }

        // ─── Core splash builder ──────────────────────────────────────────────────

        private void ShowSplashScreen(Game game, int durationInSeconds, bool startTimerImmediately, CalibrationMode calibrationMode = CalibrationMode.None)
        {
            if (_settings.ExcludedGameIds.Any(id => id.Trim() == game.Id.ToString()))
                return;

            string platformName = game.Platforms?.FirstOrDefault()?.Name ?? string.Empty;
            int duration = _settings.GetDurationForGame(game.Id.ToString(), platformName);
            if (duration <= 0)
            {
                duration = _settings.SplashScreenDuration;
                if (duration <= 0) duration = 1;
            }

            // ── Background image ─────────────────────────────────────────────────
            string resolvedBgPath = null;
            if (!string.IsNullOrEmpty(game.BackgroundImage))
            {
                try
                {
                    if (game.BackgroundImage.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedBgPath = game.BackgroundImage;
                    }
                    else
                    {
                        string playniteDataDir = PlayniteApi.Paths.IsPortable
                            ? PlayniteApi.Paths.ApplicationPath
                            : PlayniteApi.Paths.ConfigurationPath;

                        string candidate = Path.IsPathRooted(game.BackgroundImage)
                            ? game.BackgroundImage
                            : Path.Combine(playniteDataDir, "library", "files", game.BackgroundImage);

                        if (File.Exists(candidate))
                            resolvedBgPath = candidate;
                    }
                }
                catch { }
            }

            // ── Extra Metadata paths (portable-aware) ────────────────────────────
            string gameMetaDir = GetExtraMetadataGameDir(game.Id);

            // Logo (fix: was previously always using ConfigurationPath)
            string logoPath = null;
            try
            {
                string candidate = Path.Combine(gameMetaDir, "Logo.png");
                if (File.Exists(candidate)) logoPath = candidate;
            }
            catch { }

            // Video — file chosen according to the VideoSource setting
            string videoPath = null;
            if (_settings.EnableVideoSplash)
            {
                try
                {
                    string trailer = Path.Combine(gameMetaDir, "VideoTrailer.mp4");
                    string micro   = Path.Combine(gameMetaDir, "VideoMicrotrailer.mp4");

                    if (_settings.VideoSource == VideoSourcePreference.TrailerWithMicroFallback)
                    {
                        videoPath = File.Exists(trailer) ? trailer : File.Exists(micro) ? micro : null;
                    }
                    else if (_settings.VideoSource == VideoSourcePreference.MicrotrailerWithTrailerFallback)
                    {
                        videoPath = File.Exists(micro) ? micro : File.Exists(trailer) ? trailer : null;
                    }
                    else if (_settings.VideoSource == VideoSourcePreference.TrailerOnly)
                    {
                        videoPath = File.Exists(trailer) ? trailer : null;
                    }
                    else if (_settings.VideoSource == VideoSourcePreference.MicrotrailerOnly)
                    {
                        videoPath = File.Exists(micro) ? micro : null;
                    }
                }
                catch { }
            }

            // ── Build window ─────────────────────────────────────────────────────
            var splashWindow = new Window
            {
                Title          = "CineSplashScreen",
                WindowStyle    = WindowStyle.None,
                Topmost        = true,
                Background     = Brushes.Black,
                ResizeMode     = ResizeMode.NoResize,
                ShowInTaskbar  = false,
                Opacity        = 0
            };

            if (calibrationMode == CalibrationMode.Manual)
            {
                splashWindow.WindowState = WindowState.Normal;
                splashWindow.Width = SystemParameters.PrimaryScreenWidth * 0.8;
                splashWindow.Height = SystemParameters.PrimaryScreenHeight * 0.8;
                splashWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                splashWindow.WindowState = WindowState.Maximized;
            }

            var grid = new Grid();

            // ── Background layer: video or image ─────────────────────────────────
            if (videoPath != null)
            {
                var media = new MediaElement
                {
                    Source              = new Uri(videoPath, UriKind.Absolute),
                    Stretch             = Stretch.UniformToFill,
                    LoadedBehavior      = MediaState.Manual,
                    UnloadedBehavior    = MediaState.Close,
                    IsMuted             = _settings.VideoMuteAudio,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment   = VerticalAlignment.Stretch
                };

                // Loop: when the video ends, restart it
                media.MediaEnded += (s, e) =>
                {
                    media.Position = TimeSpan.Zero;
                    media.Play();
                };

                // Start playback once the element is ready
                media.Loaded += (s, e) => media.Play();

                grid.Children.Add(media);
            }
            else
            {
                // Fallback: static background image (original behaviour)
                Image bgImage = new Image { Stretch = Stretch.UniformToFill };
                if (!string.IsNullOrEmpty(resolvedBgPath))
                {
                    try
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(resolvedBgPath, UriKind.RelativeOrAbsolute);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        bgImage.Source = bmp;
                    }
                    catch { }
                }
                grid.Children.Add(bgImage);
            }

            // ── Logo overlay ─────────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(logoPath))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(logoPath, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();

                    grid.Children.Add(new Image
                    {
                        Source              = bmp,
                        Stretch             = Stretch.Uniform,
                        Width               = _settings.LogoSize,
                        Height              = double.NaN,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment   = VerticalAlignment.Bottom,
                        Margin              = new Thickness(20, 0, 0, 20)
                    });
                }
                catch { }
            }
            
            // ── Elapsed time overlay (bottom-right) ───────────────────────
            if (_settings.ShowElapsedTime)
            {
                var elapsedText = new TextBlock
                {
                    Text = "0.0s",
                    Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                    FontSize = 14,
                    FontFamily = new FontFamily("Consolas"),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 20, 20)
                };
                grid.Children.Add(elapsedText);

                _elapsedTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                _elapsedTimer.Tick += (s, e) =>
                {
                    double elapsed = (DateTime.Now - _splashOpenTimestamp).TotalSeconds;
                    elapsedText.Text = $"{elapsed:F1}s";
                };
                _elapsedTimer.Start();
            }

            // ── Manual recalibration prompt with window picker ────────
            if (calibrationMode == CalibrationMode.Manual)
            {
                var promptBg = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(30, 20, 30, 20),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 40, 0, 0)
                };

                var promptPanel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                promptPanel.Children.Add(new TextBlock
                {
                    Text = "\U0001F3AE Calibration Mode",
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 50)),
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 12)
                });
                promptPanel.Children.Add(new TextBlock
                {
                    Text = "Select the game window from the dropdown below,\nthen click Save.",
                    Foreground = Brushes.White,
                    FontSize = 16,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 16)
                });

                // ── Window picker dropdown ──
                var windowCombo = new ComboBox
                {
                    Width = 450,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 8),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                foreach (var t in WindowDetector.GetAllVisibleWindowTitles())
                    windowCombo.Items.Add(t);
                promptPanel.Children.Add(windowCombo);

                // ── Refresh button ──
                var refreshBtn = new Button
                {
                    Content = "\U0001F504 Refresh List",
                    FontSize = 13,
                    Padding = new Thickness(12, 4, 12, 4),
                    Margin = new Thickness(0, 0, 0, 16),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                refreshBtn.Click += (s, e) =>
                {
                    windowCombo.Items.Clear();
                    foreach (var t in WindowDetector.GetAllVisibleWindowTitles())
                        windowCombo.Items.Add(t);
                };
                promptPanel.Children.Add(refreshBtn);

                // ── Save button ──
                var saveBtn = new Button
                {
                    Content = "\U0001F4BE  Save Calibration",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(20, 8, 20, 8),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 12)
                };
                saveBtn.Click += (s, e) =>
                {
                    if (windowCombo.SelectedItem is string selectedTitle)
                    {
                        int elapsed = (int)(DateTime.Now - _splashOpenTimestamp).TotalSeconds;
                        _settings.SaveCalibration(
                            _currentGame.Id.ToString(),
                            _currentGame.Name,
                            selectedTitle,
                            elapsed);
                        SavePluginSettings(_settings);
                        Logger.Info($"CineSplash: Manual calibration '{_currentGame.Name}' -> \"{selectedTitle}\", {elapsed}s");
                        StopAllDetection();
                        FadeAndClose(splashWindow);
                    }
                };
                promptPanel.Children.Add(saveBtn);

                // ── Hotkey fallback hint ──
                promptPanel.Children.Add(new TextBlock
                {
                    Text = $"Or press  [ {_settings.CalibrationHotkeyText} ]  to capture the current foreground window.",
                    Foreground = new SolidColorBrush(Color.FromArgb(160, 200, 200, 200)),
                    FontSize = 12,
                    FontFamily = new FontFamily("Consolas"),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                promptBg.Child = promptPanel;
                grid.Children.Add(promptBg);
            }

            _currentSplashWindow = splashWindow;
            splashWindow.Content = grid;

            // ── Input polling (bypasses window focus and Playnite game limits) ────
            var inputTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33) // ~30 FPS
            };
            inputTimer.Tick += (s, e) =>
            {
                bool shouldClose = false;

                // 1. Check Keyboard Hotkey
                if (_settings.SkipHotkey != Key.None)
                {
                    if (InputPoller.IsKeyPressed(_settings.SkipHotkey) && 
                        InputPoller.AreModifiersPressed(_settings.SkipHotkeyModifiers))
                    {
                        shouldClose = true;
                    }
                }

                // 2. Check Controller Hotkey(s)
                if (!shouldClose)
                {
                    bool requiresCombo = _settings.SkipControllerButton != Playnite.SDK.Events.ControllerInput.None && _settings.SkipControllerButton2 != Playnite.SDK.Events.ControllerInput.None;
                    
                    bool button1Pressed = _settings.SkipControllerButton != Playnite.SDK.Events.ControllerInput.None && InputPoller.IsControllerButtonPressed(_settings.SkipControllerButton);
                    bool button2Pressed = _settings.SkipControllerButton2 != Playnite.SDK.Events.ControllerInput.None && InputPoller.IsControllerButtonPressed(_settings.SkipControllerButton2);

                    if (requiresCombo)
                    {
                        if (button1Pressed && button2Pressed) shouldClose = true;
                    }
                    else if (button1Pressed || button2Pressed)
                    {
                        shouldClose = true;
                    }
                }

                if (shouldClose)
                {
                    inputTimer.Stop();
                    StopAllDetection();
                    FadeAndClose(splashWindow);
                    return;
                }

                // 3. Check Calibration Hotkey
                if (_settings.EnableWindowDetection &&
                    _settings.CalibrationHotkey != Key.None &&
                    InputPoller.IsKeyPressed(_settings.CalibrationHotkey) &&
                    InputPoller.AreModifiersPressed(_settings.CalibrationHotkeyModifiers))
                {
                    string title = WindowDetector.GetForegroundWindowTitle();
                    if (!string.IsNullOrEmpty(title) && title != "CineSplashScreen" && title != "Playnite")
                    {
                        int elapsed = (int)(DateTime.Now - _splashOpenTimestamp).TotalSeconds;
                        _settings.SaveCalibration(_currentGame.Id.ToString(), _currentGame.Name, title, elapsed);
                        SavePluginSettings(_settings);
                        Logger.Info($"CineSplash: Manual calibration '{_currentGame.Name}' -> \"{title}\", {elapsed}s");
                    }
                    inputTimer.Stop();
                    StopAllDetection();
                    FadeAndClose(splashWindow);
                    return;
                }
            };
            
            splashWindow.Closed += (s, e) => { inputTimer.Stop(); StopAllDetection(); };
            inputTimer.Start();

            // ── Fade-in animation ─────────────────────────────────────────────────
            var fadeIn = new DoubleAnimation
            {
                From = 0, To = 1,
                Duration = TimeSpan.FromSeconds(1)
            };
            Storyboard.SetTarget(fadeIn, splashWindow);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(Window.OpacityProperty));

            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeIn);

            if (startTimerImmediately)
            {
                var closeTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(duration)
                };
                closeTimer.Tick += (s, e) => { closeTimer.Stop(); StopAllDetection(); FadeAndClose(splashWindow); };
                splashWindow.Loaded += (s, e) => { storyboard.Begin(); closeTimer.Start(); };
            }
            else
            {
                splashWindow.Loaded += (s, e) => storyboard.Begin();
            }

            try { splashWindow.Show(); splashWindow.Activate(); }
            catch { }
        }
    }

    public static class InputPoller
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_STATE
        {
            public uint dwPacketNumber;
            public XINPUT_GAMEPAD Gamepad;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_GAMEPAD
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        [DllImport("xinput1_4.dll")]
        private static extern int XInputGetState(int dwUserIndex, out XINPUT_STATE pState);

        public static bool IsKeyPressed(Key key)
        {
            int vKey = KeyInterop.VirtualKeyFromKey(key);
            return (GetAsyncKeyState(vKey) & 0x8000) != 0;
        }

        public static bool AreModifiersPressed(ModifierKeys requiredModifiers)
        {
            bool ctrlPressed = (GetAsyncKeyState(KeyInterop.VirtualKeyFromKey(Key.LeftCtrl)) & 0x8000) != 0 || 
                               (GetAsyncKeyState(KeyInterop.VirtualKeyFromKey(Key.RightCtrl)) & 0x8000) != 0;
            bool altPressed = (GetAsyncKeyState(KeyInterop.VirtualKeyFromKey(Key.LeftAlt)) & 0x8000) != 0 || 
                              (GetAsyncKeyState(KeyInterop.VirtualKeyFromKey(Key.RightAlt)) & 0x8000) != 0;
            bool shiftPressed = (GetAsyncKeyState(KeyInterop.VirtualKeyFromKey(Key.LeftShift)) & 0x8000) != 0 || 
                                (GetAsyncKeyState(KeyInterop.VirtualKeyFromKey(Key.RightShift)) & 0x8000) != 0;
            bool winPressed = (GetAsyncKeyState(KeyInterop.VirtualKeyFromKey(Key.LWin)) & 0x8000) != 0 || 
                              (GetAsyncKeyState(KeyInterop.VirtualKeyFromKey(Key.RWin)) & 0x8000) != 0;

            bool ctrlReq = requiredModifiers.HasFlag(ModifierKeys.Control);
            bool altReq = requiredModifiers.HasFlag(ModifierKeys.Alt);
            bool shiftReq = requiredModifiers.HasFlag(ModifierKeys.Shift);
            bool winReq = requiredModifiers.HasFlag(ModifierKeys.Windows);

            return (ctrlPressed == ctrlReq) && (altPressed == altReq) && (shiftPressed == shiftReq) && (winPressed == winReq);
        }
        
        public static bool IsControllerButtonPressed(Playnite.SDK.Events.ControllerInput input)
        {
            if (input == Playnite.SDK.Events.ControllerInput.None) return false;

            for (int i = 0; i < 4; i++)
            {
                try
                {
                    if (XInputGetState(i, out XINPUT_STATE state) == 0)
                    {
                        if (IsInputPressed(state.Gamepad, input))
                            return true;
                    }
                }
                catch { } // Suppress errors if xinput is somehow completely absent
            }
            return false;
        }

        private static bool IsInputPressed(XINPUT_GAMEPAD gamepad, Playnite.SDK.Events.ControllerInput input)
        {
            switch (input)
            {
                case Playnite.SDK.Events.ControllerInput.DPadUp: return (gamepad.wButtons & 0x0001) != 0;
                case Playnite.SDK.Events.ControllerInput.DPadDown: return (gamepad.wButtons & 0x0002) != 0;
                case Playnite.SDK.Events.ControllerInput.DPadLeft: return (gamepad.wButtons & 0x0004) != 0;
                case Playnite.SDK.Events.ControllerInput.DPadRight: return (gamepad.wButtons & 0x0008) != 0;
                case Playnite.SDK.Events.ControllerInput.Start: return (gamepad.wButtons & 0x0010) != 0;
                case Playnite.SDK.Events.ControllerInput.Back: return (gamepad.wButtons & 0x0020) != 0;
                case Playnite.SDK.Events.ControllerInput.LeftStick: return (gamepad.wButtons & 0x0040) != 0;
                case Playnite.SDK.Events.ControllerInput.RightStick: return (gamepad.wButtons & 0x0080) != 0;
                case Playnite.SDK.Events.ControllerInput.LeftShoulder: return (gamepad.wButtons & 0x0100) != 0;
                case Playnite.SDK.Events.ControllerInput.RightShoulder: return (gamepad.wButtons & 0x0200) != 0;
                case Playnite.SDK.Events.ControllerInput.A: return (gamepad.wButtons & 0x1000) != 0;
                case Playnite.SDK.Events.ControllerInput.B: return (gamepad.wButtons & 0x2000) != 0;
                case Playnite.SDK.Events.ControllerInput.X: return (gamepad.wButtons & 0x4000) != 0;
                case Playnite.SDK.Events.ControllerInput.Y: return (gamepad.wButtons & 0x8000) != 0;
                case Playnite.SDK.Events.ControllerInput.TriggerLeft: return gamepad.bLeftTrigger > 128;
                case Playnite.SDK.Events.ControllerInput.TriggerRight: return gamepad.bRightTrigger > 128;
            }
            return false;
        }
    }
}
