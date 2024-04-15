using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CG
{
    public class CGPlayer : MonoBehaviour, ICGPlayerInterface
    {
        private string _chapterName;
        private Language _language;

        private List<Func<CancellationToken, UniTask>> _tasksToResume;
        private CancellationTokenSource _tokenSource;

        public event Action OnLanguageChanged;

        public bool AutoPlay
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public bool FastForward
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public Language Language
        {
            get => _language;
            set
            {
                _language = value;
                OnLanguageChanged?.Invoke();
            }
        }

        public TMP_FontAsset FontAsset { get; private set; }

        public void Initialize(string chapterName)
        {
            _chapterName = chapterName;
            // TODO: Replace with Addressable Reference
            FontAsset = Resources.Load<TMP_FontAsset>($"Fonts/{chapterName}.asset");
        }

        public void Play()
        {
            throw new System.NotImplementedException();
        }

        public void Pause()
        {
            _tokenSource.Cancel();
        }

        public async void Resume()
        {
            _tokenSource = new();
            await UniTask.WhenAll(_tasksToResume.ConvertAll(task => task(_tokenSource.Token)));
        }

        public void Stop()
        {
            OnLanguageChanged = null;
            _tokenSource.Cancel();
            _tasksToResume.Clear();
        }

        public void HideText()
        {
            throw new System.NotImplementedException();
        }

        public void ShowText()
        {
            throw new System.NotImplementedException();
        }
    }

    internal class ResumeEventArgs : EventArgs
    {
        public CancellationToken Token { get; private set; }

        public ResumeEventArgs(CancellationToken token)
        {
            Token = token;
        }
    }
}
