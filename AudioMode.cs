namespace GameAudioSwitcher
{
    public enum AudioMode
    {
        Default = 0,
        Stereo = 1,
        Surround51 = 2,
        Surround71 = 3,
        Atmos = 4
    }

    internal static class AudioModeNames
    {
        public static string Friendly(AudioMode mode)
        {
            switch (mode)
            {
                case AudioMode.Stereo:
                    return "Stereo (AVR upmix)";
                case AudioMode.Surround51:
                    return "5.1 PCM";
                case AudioMode.Surround71:
                    return "7.1 PCM";
                case AudioMode.Atmos:
                    return "Dolby Atmos";
                default:
                    return "Default";
            }
        }
    }
}
