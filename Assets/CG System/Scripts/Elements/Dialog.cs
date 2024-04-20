using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CG
{
    internal class Dialog : TextBlock
    {
        // TODO: Implement fade animation
        // [SerializeField] private float _fadeSpeed = 1f; // 渐变速度
        // [SerializeField] private float _fastForwardFadeSpeed = 10f; // 快进时渐变速度

        [SerializeField] private TextBoxType _textBoxType;
        [SerializeField] private Image _dialogBox = null;
        [SerializeField] private Image _face = null;
        [SerializeField] private Image _nameStrip = null;

        private DialogAnimator _animator;
        private AnimationType _animationType;

        // TODO: Replace with Addressable Asset System
        private readonly string _folderPath = "Assets/CG System/Art/Characters";

        private bool _isEntering = false;
        private bool _isExiting = false;

        public override void Initialize(CGPlayer player)
        {
            base.Initialize(player);

            _animator = new(this);
            gameObject.SetActive(false);
        }

        public override void InitializeLine(StoryLine storyLine)
        {
            base.InitializeLine(storyLine);

            string characterName = storyLine.Character;
            string nameStripPath = $"{_folderPath}/NameStrips/{characterName}.png";
            _nameStrip.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(nameStripPath);

            if (_face != null)
            {
                string facePath = $"{_folderPath}/{characterName}/{characterName}-{storyLine.Expression}.png";
                _face.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(facePath);
            }

            // TODO: Support more animation types
            _animationType = AnimationType.Immediate;
        }

        public override async UniTask Enter(CancellationToken token)
        {
            gameObject.SetActive(true);

            if (_animationType == AnimationType.Fade)
            {
                _isEntering = true;
                await _animator.FadeIn(token);
                if (token.IsCancellationRequested)
                {
                    return;
                }
            }
            else
            {
                _animator.ImmediateShow();
            }

            await TypeText(token);
            if (token.IsCancellationRequested)
            {
                return;
            }
            _isEntering = false;
        }

        public override async UniTask Exit(CancellationToken token)
        {
            if (_animationType == AnimationType.Fade)
            {
                _isExiting = true;
                await _animator.FadeOut(token);
                if (token.IsCancellationRequested)
                {
                    return;
                }
            }
            else
            {
                _animator.ImmediateHide();
            }

            _textMeshPro.maxVisibleCharacters = 0;
            _isExiting = false;
            gameObject.SetActive(false);
        }

        public override void Skip()
        {
            if (_isEntering)
            {
                _animator.ImmediateShow();
            }
            if (_isExiting)
            {
                _animator.ImmediateHide();
            }

            SkipTyping();
        }

        protected override void Awake()
        {
            base.Awake();

            var images = GetComponentsInChildren<Image>(true);
            Color color = Color.white;
            color.a = 0f;
            switch (_textBoxType)
            {
                case TextBoxType.Normal:
                    _dialogBox = images[0];
                    _face = images[1];
                    _nameStrip = images[2];
                    _dialogBox.color = color;
                    _face.color = color;
                    _nameStrip.color = color;
                    break;
                case TextBoxType.NoAvatar:
                    _dialogBox = images[0];
                    _nameStrip = images[1];
                    _dialogBox.color = color;
                    _nameStrip.color = color;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(_textBoxType),
                        _textBoxType,
                        null
                    );
            }
        }

        internal class DialogAnimator
        {
            private readonly Dialog _dialog;

            public DialogAnimator(Dialog dialog)
            {
                _dialog = dialog;
            }

            public void ImmediateShow()
            {
                _dialog._dialogBox.color = Color.white;
                _dialog._nameStrip.color = Color.white;
                if (_dialog._face != null)
                {
                    _dialog._face.color = Color.white;
                }
                _dialog._textMeshPro.color = _dialog._textColor;
            }

            public void ImmediateHide()
            {
                _dialog._dialogBox.color = Color.clear;
                _dialog._nameStrip.color = Color.clear;
                _dialog._textMeshPro.color = Color.clear;
                if (_dialog._face != null)
                {
                    _dialog._face.color = Color.clear;
                }
            }

            public UniTask FadeIn(CancellationToken token)
            {
                throw new System.NotImplementedException();
            }
            public UniTask FadeOut(CancellationToken token)
            {
                throw new System.NotImplementedException();
            }
        }

        public enum AnimationType
        {
            Immediate,
            Fade
        }
    }
}
