using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.Utilities;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace CG
{
    using StoryLine = XMLReader.XMLLine;

    public class CGPlayer : MonoBehaviour, ICGPlayerInterface
    {
        private string _chapterName;
        private XMLReader _xmlReader;

        private StoryLine _storyLine;
        private StoryLine _previousStoryLine;
        private Scene[] _scenes;
        private Narration[] _narrations;
        private Dialog _dialog;
        private int _currentSceneIndex = -1;
        private int _currentNarrationIndex;

        private CGState _state = CGState.None;
        private CGState _previousState = CGState.None;
        private bool _autoPlay;
        private bool _fastForward;
        private Language _language;

        private bool _needReserveNarration = false;

        private CancellationTokenSource _tokenSource;

        public event Action OnAutoPlayChanged;
        public event Action OnFastForwardChanged;
        public event Action OnLanguageChanged;
        public event Action OnPlayCompleted;
        public event Action OnHideTextAndUI;
        public event Action OnShowTextAndUI;

        public bool AutoPlay
        {
            get => _autoPlay;
            set
            {
                if (value)
                {
                    _state = CGState.AutoPlaying;
                    if (_state == CGState.Waiting)
                    {
                        UpdateStoryLine();
                        PlayStoryLine();
                    }
                }
                else
                {
                    _state = CGState.Playing;
                    if (_fastForward)
                    {
                        FastForward = false;
                    }
                }

                _autoPlay = value;
                OnAutoPlayChanged?.Invoke();
            }
        }

        public bool FastForward
        {
            get => _fastForward;
            set
            {
                if (value && !_autoPlay)
                {
                    AutoPlay = true;
                }

                _fastForward = value;
                OnFastForwardChanged?.Invoke();
            }
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
            _xmlReader = new XMLReader(chapterName);
            // TODO: Replace with Addressable Reference
            FontAsset = Resources.Load<TMP_FontAsset>($"Fonts/{chapterName}.asset");

            var canvasObject = GameObject.Find("Canvas");
            _scenes = canvasObject.GetComponentsInChildren<Scene>();
            _dialog = GetComponentInChildren<Dialog>();
        }

        public void Play()
        {
            _tokenSource = new();
            _state = CGState.Playing;
            _storyLine = _xmlReader.NextLine;
            PlayStoryLine();
        }

        public void Pause()
        {
            _tokenSource.Cancel();
            _previousState = _state;
            _state = CGState.Paused;
        }

        public void Resume()
        {
            _tokenSource = new();
            _state = _previousState;
            _previousState = CGState.None;

            _tokenSource = new();
            PlayStoryLine();
        }

        public void Stop()
        {
            OnAutoPlayChanged = null;
            OnFastForwardChanged = null;
            OnLanguageChanged = null;
            OnPlayCompleted = null;

            _tokenSource.Cancel();
        }

        public void HideText()
        {
            OnHideTextAndUI?.Invoke();
        }

        public void ShowText()
        {
            OnShowTextAndUI?.Invoke();
        }

        private void UpdateStoryLine()
        {
            // TODO: Simply store StoryLine in a list
            _previousStoryLine = _storyLine;
            _storyLine = _xmlReader.NextLine;
        }

        private async void PlayStoryLine()
        {
            if (_storyLine == null)
            {
                OnPlayCompleted?.Invoke();
                return;
            }

            await (_storyLine.LineType switch
            {
                LineType.Scene => PlayScene(),
                LineType.Narration => PlayNarration(),
                LineType.Dialog => PlayDialog(),
                LineType.PlayAnimation => PlayAnimation(),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(_storyLine.LineType),
                    _storyLine.LineType,
                    null
                )
            });

            OnStoryLineCompleted();
        }

        private async void OnStoryLineCompleted()
        {
            // TODO: Interval or interactive between story lines
            await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: _tokenSource.Token);
            if (_tokenSource.Token.IsCancellationRequested)
            {
                return;
            }
            UpdateStoryLine();
            PlayStoryLine();
        }

        private async UniTask PlayScene()
        {
            if (_currentSceneIndex != -1)
            {
                List<UniTask> tasks = new(2 + _narrations.Length)
                {
                    _scenes[_currentSceneIndex].Exit(_tokenSource.Token),
                    _dialog.Exit(_tokenSource.Token)
                };
                if (!_needReserveNarration)
                {
                    _narrations.ForEach(narration => tasks.Add(narration.Exit(_tokenSource.Token)));
                }
                else
                {
                    _needReserveNarration = false;
                }

                await UniTask.WhenAll(tasks);
                if (_tokenSource.Token.IsCancellationRequested)
                {
                    return;
                }
            }

            var scene = _scenes[_currentSceneIndex++];
            _narrations.AddRange(scene.GetComponentsInChildren<Narration>());

            await scene.Enter(_tokenSource.Token);
            if (_tokenSource.Token.IsCancellationRequested)
            {
                return;
            }
        }

        private async UniTask PlayNarration()
        {
            var narration = _narrations[++_currentNarrationIndex];
            narration.Initialize(this);

            await narration.Enter(_storyLine, _tokenSource.Token);
            if (_tokenSource.Token.IsCancellationRequested)
            {
                return;
            }
        }

        private async UniTask PlayDialog()
        {
            if (!_needReserveNarration)
            {
                List<UniTask> tasks = new(_narrations.Length);
                _narrations.ForEach(narration => tasks.Add(narration.Exit(_tokenSource.Token)));
            }
            else
            {
                _needReserveNarration = false;
            }

            await _dialog.Enter(_storyLine, _tokenSource.Token);
            if (_tokenSource.Token.IsCancellationRequested)
            {
                return;
            }
        }

        private async UniTask PlayAnimation()
        {
            var scene = _scenes[_currentSceneIndex];
            await scene.PlayAnimation(_tokenSource.Token);
            if (_tokenSource.Token.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
