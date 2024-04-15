using System.Security.Cryptography;

namespace CG
{
    public interface ICGPlayerInterface
    {
        bool AutoPlay { get; set; }

        bool FastForward { get; set; }

        Language Language { get; set; }

        void Initialize(string chapterName);

        void Play();

        void Pause();

        void Resume();

        void Stop();

        void HideText();

        void ShowText();
    }
}
