using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace CG
{
    using StoryLine = XMLReader.XMLLine;

    internal abstract class TextBlock : MonoBehaviour
    {
        [SerializeField] protected Color _textColor = Color.black;
        [SerializeField] private float _typeSpeed = 10f;
        [SerializeField] private float _fastForwardTypeSpeed = 50f;

        protected CGPlayer _player;
        protected TextMeshProUGUI _textMeshPro;

        public string Text
        {
            get => _textMeshPro.text;
            set => _textMeshPro.text = value;
        }

        public void Initialize(CGPlayer player)
        {
            _player = player;
            _textMeshPro.font = _player.FontAsset;
            _textMeshPro.color = _textColor;
            _textMeshPro.maxVisibleCharacters = 0;
        }

        public abstract UniTask Enter(StoryLine storyLine, CancellationToken token);
        public abstract UniTask Exit(CancellationToken token);
        public abstract void Skip();

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        protected virtual void Awake()
        {
            _textMeshPro = GetComponentInChildren<TextMeshProUGUI>();
        }

        protected virtual void InitializeLine(StoryLine storyLine)
        {
            Text = _player.Language switch
            {
                Language.English => storyLine.EnglishText,
                Language.Chinese => storyLine.ChineseText,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(_player.Language),
                    $"The specified language '{_player.Language}' is not allowed."
                )
            };
        }

        protected async UniTask TypeText(CancellationToken token)
        {
            _player.OnLanguageChanged += OnLanguageChangedHandler;

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                int length = _textMeshPro.maxVisibleCharacters;
                if (length >= _textMeshPro.text.Length)
                {
                    _player.OnLanguageChanged -= OnLanguageChangedHandler;
                    break;
                }

                float speed = _player.FastForward ? _fastForwardTypeSpeed : _typeSpeed;
                float delay = 1f / speed;
                _textMeshPro.maxVisibleCharacters = length + 1;
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
            }
        }

        protected void SkipTyping()
        {
            _textMeshPro.maxVisibleCharacters = _textMeshPro.text.Length;
        }

        private void OnLanguageChangedHandler()
        {
            // TODO: Change text when language changed
        }
    }
}
