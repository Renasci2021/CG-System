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
        DelayExit,
    }

    public enum ContinuationMode
    {
        Interval,
        Click,
        Gesture,
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
