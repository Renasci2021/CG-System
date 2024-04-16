namespace CG
{
    public enum LineType
    {
        Scene,
        Narration,
        Dialog,
        PlayAnimation,
    }

    public enum TextBoxType
    {
        Normal,
        NoAvatar,
        ReserveAfterScene,
    }

    public enum EffectType
    {
        None,
    }

    public enum Language
    {
        Chinese,
        English,
    }

    internal enum CGState
    {
        None,
        Playing,
        Paused,
        Waiting,
        AutoPlaying,
    }
}
