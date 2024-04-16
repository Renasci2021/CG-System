namespace CG
{
    internal enum CGState
    {
        None,
        Playing,
        Paused,
        Waiting,
        AutoPlaying,
    }

    internal enum LineType
    {
        Scene,
        Narration,
        Dialog,
        PlayAnimation,
    }

    internal enum TextBoxType
    {
        Normal,
        NoAvatar,
        ReserveAfterScene,
    }

    internal enum EffectType
    {
        None,
    }

    public enum Language
    {
        Chinese,
        English,
    }
}
