using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace CG
{
    public class CGPlayer : MonoBehaviour, ICGPlayerInterface
    {
        [SerializeField] private float _interval = 1f;

        private string _chapterName;
        private XMLReader _xmlReader;

        private StoryLine _storyLine;
        private Scene[] _scenes;
        private Narration[] _narrations;
        private DialogPlayer _dialogPlayer;
        private int _currentSceneIndex = 0;
        private int _currentNarrationIndex;

        private CGState _state = CGState.None;
        private CGState _previousState = CGState.None;
        private bool _autoPlay;
        private bool _fastForward;
        private Language _language;

        private CancellationTokenSource _tokenSource;
        private CancellationTokenSource _intervalTokenSource;
        // private List<ResumeMethod> _resumeMethods = new();

        // private delegate UniTask ResumeMethod();

        public event Action OnAutoPlayChange;
        public event Action OnFastForwardChange;
        public event Action OnLanguageChange;
        public event Action OnPlayComplete;
        public event Action OnHideTextAndUI;
        public event Action OnShowTextAndUI;
        internal event Action OnSkip;

        public bool AutoPlay
        {
            get => _autoPlay;
            set
            {
                throw new NotImplementedException();
                // if (value)
                // {
                //     if (_state == CGState.Waiting)
                //     {
                //         _intervalTokenSource.Cancel();
                //     }
                // }
                // else
                // {
                //     _state = CGState.Playing;
                //     if (_fastForward)
                //     {
                //         FastForward = false;
                //     }
                // }

                // _autoPlay = value;
                // OnAutoPlayChange?.Invoke();
            }
        }

        public bool FastForward
        {
            get => _fastForward;
            set
            {
                _fastForward = value;
                if (_state == CGState.Waiting)
                {
                    _intervalTokenSource.Cancel();
                }
                // if (value && !_autoPlay)
                // {
                //     AutoPlay = true;
                // }

                // _fastForward = value;
                // OnFastForwardChange?.Invoke();
            }
        }

        public Language Language
        {
            get => _language;
            set
            {
                _language = value;
                OnLanguageChange?.Invoke();
            }
        }

        internal bool IsPaused => _state == CGState.Paused;

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
            _storyLine = _xmlReader.NextLine;

            _tokenSource = new();
            PlayStoryLine().Forget();
        }

        public void Pause()
        {
            // _tokenSource.Cancel();

            _previousState = _state;
            _state = CGState.Paused;
        }

        public void Resume()
        {
            _state = _previousState;
            _previousState = CGState.None;

            // TODO: Replace with async UniTask methods
            // _tokenSource = new();
            // var tasks = new List<UniTask>(_resumeMethods.Count);
            // _resumeMethods.ForEach(resumeMethod => tasks.Add(resumeMethod()));
            // _resumeMethods.Clear();
            // bool isCanceled = await UniTask.WhenAll(tasks).SuppressCancellationThrow();
            // if (isCanceled)
            // {
            //     return;
            // }
            // OnStoryLineCompleted().Forget();
        }

        public void Stop()
        {
            OnAutoPlayChange = null;
            OnFastForwardChange = null;
            OnLanguageChange = null;
            OnPlayComplete = null;
            OnHideTextAndUI = null;
            OnShowTextAndUI = null;
            OnSkip = null;

            // _resumeMethods.Clear();
            _tokenSource.Cancel();
        }

        public void Skip()
        {
            if (_state == CGState.Waiting)
            {
                _intervalTokenSource.Cancel();
            }
            else
            {
                OnSkip?.Invoke();
            }
        }

        public void HideText() => OnHideTextAndUI?.Invoke();

        public void ShowText() => OnShowTextAndUI?.Invoke();

        private void UpdateStoryLine() => _storyLine = _xmlReader.NextLine;

        private async UniTaskVoid PlayStoryLine()
        {
            if (_storyLine == null)
            {
                _state = CGState.None;
                OnPlayComplete?.Invoke();
                Stop();
                return;
            }

            _state = CGState.Playing;

            bool isCanceled = await (_storyLine.LineType switch
            {
                LineType.Scene => PlayScene(),
                LineType.Narration => PlayNarration(),
                LineType.Dialog => PlayDialog(),
                LineType.PlayAnimation => PlayAnimation(),
                _ => UniTask.CompletedTask
            }).SuppressCancellationThrow();
            if (isCanceled)
            {
                return;
            }
            OnStoryLineCompleted();
        }

        private void OnStoryLineCompleted()
        {
            if (FastForward)
            {
                UpdateStoryLine();
                PlayStoryLine().Forget();
                return;
            }

            TimeSpan timeSpan = _storyLine.ContinuationMode switch
            {
                ContinuationMode.Interval => TimeSpan.FromSeconds(_interval),
                _ => TimeSpan.MaxValue
            };
            _intervalTokenSource = new();
            NextStoryLineAfterInterval(timeSpan, _intervalTokenSource.Token).Forget();
        }

        private async UniTaskVoid NextStoryLineAfterInterval(TimeSpan timeSpan, CancellationToken token)
        {
            _state = CGState.Waiting;
            await UniTask.Delay(timeSpan, cancellationToken: token).SuppressCancellationThrow();

            UpdateStoryLine();
            PlayStoryLine().Forget();
        }

        private async UniTask PlayScene()
        {
            bool isCanceled;
            if (_currentSceneIndex != 0)
            {
                isCanceled = await ClearScene().SuppressCancellationThrow();
                if (isCanceled)
                {
                    // _resumeMethods.Add(() => PlayScene());
                    return;
                }
            }

            var scene = _scenes[_currentSceneIndex];
            scene.Initialize(this);
            // * param true: include inactive objects
            _narrations = scene.GetComponentsInChildren<Narration>(true);
            _currentNarrationIndex = 0;

            isCanceled = await scene.Enter(_tokenSource.Token).SuppressCancellationThrow();
            if (await scene.Enter(_tokenSource.Token).SuppressCancellationThrow())
            {
                // _resumeMethods.Add(() => PlayScene());
                return;
            }
            _currentSceneIndex++;
        }

        private async UniTask PlayNarration()
        {
            bool isCanceled = await _dialogPlayer.Exit(_tokenSource.Token).SuppressCancellationThrow();
            if (isCanceled)
            {
                // _resumeMethods.Add(() => PlayNarration());
                return;
            }

            var narration = _narrations[_currentNarrationIndex];
            narration.Initialize(this);
            narration.InitializeLine(_storyLine);

            await EnterNarration(narration);

            async UniTask EnterNarration(Narration narration)
            {
                isCanceled = await narration.Enter(_tokenSource.Token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    // _resumeMethods.Add(() => EnterNarration(narration));
                    return;
                }
                _currentNarrationIndex++;
            }
        }

        private async UniTask PlayDialog()
        {
            bool isCanceled;
            if (_storyLine.NeedClearNarrations)
            {
                isCanceled = await ClearNarrations().SuppressCancellationThrow();
                if (isCanceled)
                {
                    // _resumeMethods.Add(() => PlayDialog());
                    return;
                }
            }

            isCanceled = await _dialogPlayer.Enter(_storyLine, _tokenSource.Token).SuppressCancellationThrow();
            if (isCanceled)
            {
                // _resumeMethods.Add(() => PlayDialog());
                return;
            }
        }

        private async UniTask PlayAnimation()
        {
            var scene = _scenes[_currentSceneIndex - 1];
            bool isCanceled = await scene.PlayAnimation(_tokenSource.Token).SuppressCancellationThrow();
            if (isCanceled)
            {
                // _resumeMethods.Add(() => PlayAnimation());
                return;
            }
        }

        private async UniTask ClearScene()
        {
            bool isCanceled = await UniTask.WhenAll(
                _scenes[_currentSceneIndex - 1].Exit(_tokenSource.Token),
                _dialogPlayer.Exit(_tokenSource.Token),
                _storyLine.NeedClearNarrations ? ClearNarrations() : UniTask.CompletedTask
            ).SuppressCancellationThrow();
            if (isCanceled)
            {
                // _resumeMethods.Add(() => ClearScene());
                return;
            }
        }

        public async UniTask ClearNarrations()
        {
            var tasks = new List<UniTask>(_narrations.Length);
            foreach (var narration in _narrations)
            {
                tasks.Add(narration.Exit(_tokenSource.Token));
            }

            bool isCanceled = await UniTask.WhenAll(tasks).SuppressCancellationThrow();
            if (isCanceled)
            {
                // _resumeMethods.Add(() => ClearNarrations());
                return;
            }
        }
    }
}
