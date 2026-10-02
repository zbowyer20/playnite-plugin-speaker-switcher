using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace GameAudioSwitcher
{
    [LoadPlugin]
    public class GameAudioSwitcherPlugin : GenericPlugin
    {
        private static readonly ILogger Logger = LogManager.GetLogger();
        private readonly HashSet<Guid> activeGames = new HashSet<Guid>();

        public override Guid Id { get; } = Guid.Parse("6a113baf-8802-4dc5-9a5b-8cfc3519db83");

        public GameAudioSwitcherSettings Settings { get; private set; }

        public GameAudioSwitcherPlugin(IPlayniteAPI api) : base(api)
        {
            Properties = new GenericPluginProperties { HasSettings = true };
            Settings = new GameAudioSwitcherSettings(this);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunView)
        {
            return new GameAudioSwitcherSettingsView(this);
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            if (args?.Game == null)
            {
                return;
            }

            activeGames.Add(args.Game.Id);
            var mode = ResolveMode(args.Game);
            if (!ApplyAudioMode(mode, out var error))
            {
                ReportError($"Could not switch audio for {args.Game.Name}: {error}");
            }
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            if (args?.Game != null)
            {
                activeGames.Remove(args.Game.Id);
            }

            RestoreDesktopIfAppropriate();
        }

        public override void OnGameStartupCancelled(OnGameStartupCancelledEventArgs args)
        {
            if (args?.Game != null)
            {
                activeGames.Remove(args.Game.Id);
            }

            RestoreDesktopIfAppropriate();
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            if (args?.Games == null || args.Games.Count == 0)
            {
                yield break;
            }

            foreach (var mode in new[] { AudioMode.Default, AudioMode.Stereo, AudioMode.Surround51, AudioMode.Surround71, AudioMode.Atmos })
            {
                var captured = mode;
                var allMatch = args.Games.All(g => GetAssignedMode(g) == captured);
                var prefix = allMatch ? "✓ " : string.Empty;

                yield return new GameMenuItem
                {
                    MenuSection = "Game Audio Switcher|Audio Mode",
                    Description = prefix + AudioModeNames.Friendly(captured),
                    Action = actionArgs =>
                    {
                        foreach (var game in actionArgs.Games)
                        {
                            SetGameMode(game, captured);
                        }

                        Settings.Persist();
                    }
                };
            }
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem
            {
                MenuSection = "Game Audio Switcher",
                Description = "Settings…",
                Action = _ => PlayniteApi.MainView.OpenPluginSettings(Id)
            };

            yield return new MainMenuItem
            {
                MenuSection = "Game Audio Switcher|Test audio mode",
                Description = "Stereo",
                Action = _ => TestFromMenu(AudioMode.Stereo)
            };

            yield return new MainMenuItem
            {
                MenuSection = "Game Audio Switcher|Test audio mode",
                Description = "5.1 PCM",
                Action = _ => TestFromMenu(AudioMode.Surround51)
            };

            yield return new MainMenuItem
            {
                MenuSection = "Game Audio Switcher|Test audio mode",
                Description = "7.1 PCM",
                Action = _ => TestFromMenu(AudioMode.Surround71)
            };

            yield return new MainMenuItem
            {
                MenuSection = "Game Audio Switcher|Test audio mode",
                Description = "Dolby Atmos",
                Action = _ => TestFromMenu(AudioMode.Atmos)
            };
        }

        public AudioMode GetAssignedMode(Game game)
        {
            if (game == null || Settings.GameModes == null)
            {
                return AudioMode.Default;
            }

            return Settings.GameModes.TryGetValue(game.Id.ToString(), out var mode) ? mode : AudioMode.Default;
        }

        public AudioMode ResolveMode(Game game)
        {
            var assigned = GetAssignedMode(game);
            return assigned == AudioMode.Default ? Settings.DefaultGameMode : assigned;
        }

        public void SetGameMode(Game game, AudioMode mode)
        {
            if (game == null)
            {
                return;
            }

            if (Settings.GameModes == null)
            {
                Settings.GameModes = new Dictionary<string, AudioMode>();
            }

            var key = game.Id.ToString();
            if (mode == AudioMode.Default)
            {
                Settings.GameModes.Remove(key);
            }
            else
            {
                Settings.GameModes[key] = mode;
            }
        }

        public bool ApplyAudioMode(AudioMode mode, out string error)
        {
            error = null;

            if (mode == AudioMode.Default)
            {
                mode = Settings.DefaultGameMode;
            }

            if (!EnsureSoundVolumeView(out error))
            {
                return false;
            }

            try
            {
                switch (mode)
                {
                    case AudioMode.Stereo:
                        RunSoundVolumeView("/SetSpatial", Settings.AudioDevice, string.Empty);
                        SetSpeakerConfig(Settings.StereoSpeakerConfig);
                        break;

                    case AudioMode.Surround51:
                        RunSoundVolumeView("/SetSpatial", Settings.AudioDevice, string.Empty);
                        SetSpeakerConfig(Settings.Surround51SpeakerConfig);
                        break;

                    case AudioMode.Surround71:
                        RunSoundVolumeView("/SetSpatial", Settings.AudioDevice, string.Empty);
                        SetSpeakerConfig(Settings.Surround71SpeakerConfig);
                        break;

                    case AudioMode.Atmos:
                        // Start from a clean multichannel layout, then enable the Dolby spatial provider.
                        RunSoundVolumeView("/SetSpatial", Settings.AudioDevice, string.Empty);
                        SetSpeakerConfig(Settings.Surround71SpeakerConfig);
                        RunSoundVolumeView("/SetSpatial", Settings.AudioDevice, Settings.AtmosFormatName);
                        break;

                    default:
                        error = "Unsupported audio mode.";
                        return false;
                }

                Logger.Info($"Game Audio Switcher: applied {AudioModeNames.Friendly(mode)}.");

                if (Settings.ShowNotifications)
                {
                    PlayniteApi.Notifications.Add(
                        $"GameAudioSwitcher_{Guid.NewGuid():N}",
                        $"Audio: {AudioModeNames.Friendly(mode)}",
                        NotificationType.Info);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Game Audio Switcher: audio mode change failed.");
                error = ex.Message;
                return false;
            }
        }

        public string TryGetCurrentSpeakerConfig(out string error)
        {
            error = null;
            if (!EnsureSoundVolumeView(out error))
            {
                return null;
            }

            try
            {
                var result = RunSoundVolumeViewWithOutput("/GetColumnValue", Settings.AudioDevice, "Speakers Config");
                result = (result ?? string.Empty).Trim();

                if (!IsValidSpeakerConfig(result))
                {
                    error = string.IsNullOrWhiteSpace(result)
                        ? "SoundVolumeView returned no speaker configuration."
                        : $"SoundVolumeView returned an unexpected speaker configuration: {result}";
                    return null;
                }

                return NormalizeSpeakerConfig(result);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Game Audio Switcher: failed to read speaker configuration.");
                error = ex.Message;
                return null;
            }
        }

        public static bool IsValidSpeakerConfig(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var tokens = SplitConfig(value);
            if (tokens.Length != 3)
            {
                return false;
            }

            return tokens.All(IsIntegerToken);
        }

        private static bool IsIntegerToken(string value)
        {
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return long.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
            }

            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
        }

        private static string[] SplitConfig(string config)
        {
            return (config ?? string.Empty)
                .Replace(",", " ")
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string NormalizeSpeakerConfig(string config)
        {
            return string.Join(" ", SplitConfig(config));
        }

        private void RestoreDesktopIfAppropriate()
        {
            if (!Settings.RestoreAfterGame || activeGames.Count > 0)
            {
                return;
            }

            if (!ApplyAudioMode(Settings.DesktopMode, out var error))
            {
                ReportError($"Could not restore desktop audio mode: {error}");
            }
        }

        private void TestFromMenu(AudioMode mode)
        {
            if (!ApplyAudioMode(mode, out var error))
            {
                PlayniteApi.Dialogs.ShowErrorMessage(error ?? "Audio mode switch failed.", "Game Audio Switcher");
            }
        }

        private bool EnsureSoundVolumeView(out string error)
        {
            if (string.IsNullOrWhiteSpace(Settings.SoundVolumeViewPath) || !File.Exists(Settings.SoundVolumeViewPath))
            {
                error = "SoundVolumeView.exe was not found. Open Game Audio Switcher settings and set its path.";
                return false;
            }

            error = null;
            return true;
        }

        private void SetSpeakerConfig(string config)
        {
            if (!IsValidSpeakerConfig(config))
            {
                throw new InvalidOperationException($"Invalid speaker configuration: '{config}'.");
            }

            var values = SplitConfig(config);
            RunSoundVolumeView("/SetSpeakersConfig", Settings.AudioDevice, values[0], values[1], values[2]);
        }

        private void RunSoundVolumeView(params string[] args)
        {
            var psi = CreateProcessInfo(args, redirectOutput: false);
            using (var process = Process.Start(psi))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Could not start SoundVolumeView.exe.");
                }

                if (!process.WaitForExit(10000))
                {
                    try { process.Kill(); } catch { }
                    throw new TimeoutException("SoundVolumeView did not finish within 10 seconds.");
                }
            }
        }

        private string RunSoundVolumeViewWithOutput(params string[] args)
        {
            var psi = CreateProcessInfo(args, redirectOutput: true);
            using (var process = Process.Start(psi))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Could not start SoundVolumeView.exe.");
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (!process.WaitForExit(10000))
                {
                    try { process.Kill(); } catch { }
                    throw new TimeoutException("SoundVolumeView did not finish within 10 seconds.");
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    Logger.Warn("SoundVolumeView stderr: " + error.Trim());
                }

                return output;
            }
        }

        private ProcessStartInfo CreateProcessInfo(IEnumerable<string> args, bool redirectOutput)
        {
            return new ProcessStartInfo
            {
                FileName = Settings.SoundVolumeViewPath,
                Arguments = string.Join(" ", args.Select(QuoteArgument)),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectOutput
            };
        }

        private static string QuoteArgument(string value)
        {
            value = value ?? string.Empty;
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private void ReportError(string message)
        {
            Logger.Error(message);
            PlayniteApi.Notifications.Add(
                $"GameAudioSwitcher_Error_{Guid.NewGuid():N}",
                message,
                NotificationType.Error);
        }
    }
}
