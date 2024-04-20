using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.Utilities;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace CG
{
    public class CGPlayer : MonoBehaviour, ICGPlayerInterface
    {
        private string _chapterName;
        private XMLReader _xmlReader;

        private StoryLine _storyLine;
        private Scene[] _scenes;
        private Narration[] _narrations;
        private DialogPlayer _dialogPlayer;
        private int _currentSceneIndex = -1;
        private int _currentNarrationIndex;

        private CGState _state = CGState.None;
        private CGState _previousState = CGState.None;
        private bool _autoPlay;
        private bool _fastForward;
        private Language _language;

        private bool _needReserveNarration = false;

        private delegate UniTask PlayMethod(CancellationToken token);
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

        public TMP_FontAsset FontAsset { get; private set; } = null;

        public async UniTask Initialize(string chapterName)
        {
            _chapterName = chapterName;
            _xmlReader = new XMLReader(chapterName);
            // TODO: Replace with Addressable Reference
            FontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"Assets/CG System/Fonts/{chapterName}.asset");
            await UniTask.WaitUntil(() => FontAsset != null);

            // * param true: include inactive objects
            var canvasObject = GameObject.Find("Canvas");
            _scenes = canvasObject.GetComponentsInChildren<Scene>(true);
            _dialogPlayer = canvasObject.GetComponentInChildren<DialogPlayer>(true);
            _dialogPlayer.Initialize(this);
        }

        public void Play()
        {
            _state = CGState.Playing;
            _storyLine = _xmlReader.NextLine;

            _tokenSource = new();
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

        public void HideText() => OnHideTextAndUI?.Invoke();

        public void ShowText() => OnShowTextAndUI?.Invoke();

        private void UpdateStoryLine() => _storyLine = _xmlReader.NextLine;

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
                List<PlayMethod> playMethods = new()
                {
                    _scenes[_currentSceneIndex].Exit,
                    _dialogPlayer.Exit
                };
                if (!_needReserveNarration)
                {
                    _narrations.ForEach(narration => playMethods.Add(narration.Exit));
                }
                else
                {
                    _needReserveNarration = false;
                }

                var tasks = new List<UniTask>(playMethods.Count);
                playMethods.ForEach(playMethod => tasks.Add(playMethod(_tokenSource.Token)));
                await UniTask.WhenAll(tasks);
                if (_tokenSource.Token.IsCancellationRequested)
                {
                    return;
                }
            }

            var scene = _scenes[++_currentSceneIndex];
            // * param true: include inactive objects
            _narrations = scene.GetComponentsInChildren<Narration>(true);
            _currentNarrationIndex = 0;

            await scene.Enter(_tokenSource.Token);
            if (_tokenSource.Token.IsCancellationRequested)
            {
                return;
            }
        }

        private async UniTask PlayNarration()
        {
            await _dialogPlayer.Exit(_tokenSource.Token);
            if (_tokenSource.Token.IsCancellationRequested)
            {
                return;
            }

            // FIXME: Pause will cause out of index
            // Use UniTask.WaitUntil to implement pause
            var narration = _narrations[_currentNarrationIndex++];
            narration.Initialize(this);
            narration.InitializeLine(_storyLine);

            await narration.Enter(_tokenSource.Token);
            if (_tokenSource.Token.IsCancellationRequested)
            {
                return;
            }
        }

        private async UniTask PlayDialog()
        {
            if (!_needReserveNarration)
            {
                List<PlayMethod> playMethods = new();
                _narrations.ForEach(Narration => playMethods.Add(Narration.Exit));
                var tasks = new List<UniTask>(playMethods.Count);
                playMethods.ForEach(playMethod => tasks.Add(playMethod(_tokenSource.Token)));
                await UniTask.WhenAll(tasks);
                if (_tokenSource.Token.IsCancellationRequested)
                {
                    return;
                }
            }
            else
            {
                _needReserveNarration = false;
            }

            await _dialogPlayer.Enter(_storyLine, _tokenSource.Token);
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
