using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;

namespace GameAudioSwitcher
{
    public class GameAudioSwitcherSettings : ObservableObject, ISettings
    {
        private readonly GameAudioSwitcherPlugin plugin;
        private GameAudioSwitcherSettings editingClone;

        private string soundVolumeViewPath = @"C:\Tools\SoundVolumeView\SoundVolumeView.exe";
        private string audioDevice = "DefaultRenderDevice";
        private string atmosFormatName = "Dolby Atmos for home theater";
        private string stereoSpeakerConfig = "0x3 0x3 0x3";
        private string surround51SpeakerConfig = "0x3f 0x3f 0x3f";
        private string surround71SpeakerConfig = "0x63f 0x63f 0x63f";
        private AudioMode defaultGameMode = AudioMode.Surround51;
        private bool restoreAfterGame = true;
        private AudioMode desktopMode = AudioMode.Stereo;
        private bool showNotifications = false;

        public string SoundVolumeViewPath
        {
            get => soundVolumeViewPath;
            set => SetValue(ref soundVolumeViewPath, value);
        }

        public string AudioDevice
        {
            get => audioDevice;
            set => SetValue(ref audioDevice, value);
        }

        public string AtmosFormatName
        {
            get => atmosFormatName;
            set => SetValue(ref atmosFormatName, value);
        }

        public string StereoSpeakerConfig
        {
            get => stereoSpeakerConfig;
            set => SetValue(ref stereoSpeakerConfig, value);
        }

        public string Surround51SpeakerConfig
        {
            get => surround51SpeakerConfig;
            set => SetValue(ref surround51SpeakerConfig, value);
        }

        public string Surround71SpeakerConfig
        {
            get => surround71SpeakerConfig;
            set => SetValue(ref surround71SpeakerConfig, value);
        }

        public AudioMode DefaultGameMode
        {
            get => defaultGameMode;
            set => SetValue(ref defaultGameMode, value);
        }

        public bool RestoreAfterGame
        {
            get => restoreAfterGame;
            set => SetValue(ref restoreAfterGame, value);
        }

        public AudioMode DesktopMode
        {
            get => desktopMode;
            set => SetValue(ref desktopMode, value);
        }

        public bool ShowNotifications
        {
            get => showNotifications;
            set => SetValue(ref showNotifications, value);
        }

        public Dictionary<string, AudioMode> GameModes { get; set; } = new Dictionary<string, AudioMode>();

        public GameAudioSwitcherSettings()
        {
        }

        public GameAudioSwitcherSettings(GameAudioSwitcherPlugin plugin)
        {
            this.plugin = plugin;

            var saved = plugin.LoadPluginSettings<GameAudioSwitcherSettings>();
            if (saved != null)
            {
                CopyFrom(saved);
            }
        }

        public void BeginEdit()
        {
            editingClone = Clone();
        }

        public void CancelEdit()
        {
            if (editingClone != null)
            {
                CopyFrom(editingClone);
            }
        }

        public void EndEdit()
        {
            plugin?.SavePluginSettings(this);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(SoundVolumeViewPath) || !File.Exists(SoundVolumeViewPath))
            {
                errors.Add("SoundVolumeView.exe was not found. Set the correct path before saving.");
            }

            if (string.IsNullOrWhiteSpace(AudioDevice))
            {
                errors.Add("Audio device cannot be blank. 'DefaultRenderDevice' is recommended.");
            }

            if (string.IsNullOrWhiteSpace(AtmosFormatName))
            {
                errors.Add("Atmos spatial format name cannot be blank.");
            }

            if (!GameAudioSwitcherPlugin.IsValidSpeakerConfig(StereoSpeakerConfig))
            {
                errors.Add("Stereo speaker config must contain exactly three integer/hex values, for example: 0x3 0x3 0x3");
            }

            if (!GameAudioSwitcherPlugin.IsValidSpeakerConfig(Surround51SpeakerConfig))
            {
                errors.Add("5.1 speaker config must contain exactly three integer/hex values, for example: 0x3f 0x3f 0x3f");
            }

            if (!GameAudioSwitcherPlugin.IsValidSpeakerConfig(Surround71SpeakerConfig))
            {
                errors.Add("7.1 speaker config must contain exactly three integer/hex values, for example: 0x63f 0x63f 0x63f");
            }

            if (DefaultGameMode == AudioMode.Default)
            {
                errors.Add("Default game mode cannot itself be 'Default'.");
            }

            if (DesktopMode == AudioMode.Default)
            {
                errors.Add("Desktop restore mode cannot be 'Default'.");
            }

            return errors.Count == 0;
        }

        internal void Persist()
        {
            plugin?.SavePluginSettings(this);
        }

        private GameAudioSwitcherSettings Clone()
        {
            return new GameAudioSwitcherSettings
            {
                SoundVolumeViewPath = SoundVolumeViewPath,
                AudioDevice = AudioDevice,
                AtmosFormatName = AtmosFormatName,
                StereoSpeakerConfig = StereoSpeakerConfig,
                Surround51SpeakerConfig = Surround51SpeakerConfig,
                Surround71SpeakerConfig = Surround71SpeakerConfig,
                DefaultGameMode = DefaultGameMode,
                RestoreAfterGame = RestoreAfterGame,
                DesktopMode = DesktopMode,
                ShowNotifications = ShowNotifications,
                GameModes = new Dictionary<string, AudioMode>(GameModes ?? new Dictionary<string, AudioMode>())
            };
        }

        private void CopyFrom(GameAudioSwitcherSettings source)
        {
            SoundVolumeViewPath = source.SoundVolumeViewPath;
            AudioDevice = source.AudioDevice;
            AtmosFormatName = source.AtmosFormatName;
            StereoSpeakerConfig = source.StereoSpeakerConfig;
            Surround51SpeakerConfig = source.Surround51SpeakerConfig;
            Surround71SpeakerConfig = source.Surround71SpeakerConfig;
            DefaultGameMode = source.DefaultGameMode == AudioMode.Default ? AudioMode.Surround51 : source.DefaultGameMode;
            RestoreAfterGame = source.RestoreAfterGame;
            DesktopMode = source.DesktopMode == AudioMode.Default ? AudioMode.Stereo : source.DesktopMode;
            ShowNotifications = source.ShowNotifications;
            GameModes = source.GameModes != null
                ? new Dictionary<string, AudioMode>(source.GameModes)
                : new Dictionary<string, AudioMode>();
        }
    }
}
