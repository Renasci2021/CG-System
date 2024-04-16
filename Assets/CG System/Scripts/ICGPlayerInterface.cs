using System;

namespace CG
{
    public interface ICGPlayerInterface
    {
        bool AutoPlay { get; set; }
        bool FastForward { get; set; }
        Language Language { get; set; }

        event Action OnAutoPlayChanged;
        event Action OnFastForwardChanged;
        event Action OnLanguageChanged;
        event Action OnPlayCompleted;
        event Action OnHideTextAndUI;
        event Action OnShowTextAndUI;

        void Initialize(string chapterName);
        void Play();
        void Pause();
        void Resume();
        void Stop();
        void HideText();
        void ShowText();
    }
}
