using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CG
{
    using StoryLine = XMLReader.XMLLine;

    internal class Dialog : TextBlock
    {
        [SerializeField] private Sprite[] _dialogBoxes; // 对话框数组

        [SerializeField] private float _fadeSpeed = 1f; // 渐变速度
        [SerializeField] private float _fastForwardFadeSpeed = 10f; // 快进时渐变速度

        private EnterAnimation _animator;

        private Image _dialogBox;   // 对话框
        private Image _face;    // 角色头像
        private Image _nameStrip;   // 角色名条

        private bool _isShowing = false;
        private bool _isEntering = false;
        private bool _isExiting = false;

        public override async UniTask Enter(StoryLine storyLine, CancellationToken token)
        {
            if (_isShowing)
            {
                _animator.ImmediateHide();
                InitializeLine(storyLine);
                _animator.ImmediateShow();
            }
            else
            {
                InitializeLine(storyLine);
                _isShowing = true;
                await _animator.FadeIn(token);
            }

            await TypeText(token);
        }

        public override async UniTask Exit(CancellationToken token)
        {
            await _animator.FadeOut(token);
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

            // * Make sure the images are in the correct order
            Image[] images = GetComponentsInChildren<Image>();
            _dialogBox = images[0];
            _face = images[1];
            _nameStrip = images[2];

            _animator = new(this);
        }

        protected override void InitializeLine(StoryLine storyLine)
        {
            base.InitializeLine(storyLine);

            _dialogBox.sprite = _dialogBoxes[(int)storyLine.TextBoxType];
            switch (storyLine.TextBoxType)
            {
                case TextBoxType.Normal:
                    // TODO: Replace with Addressable Asset System
                    string folderPath = "Assets/CG System/Art/Characters";
                    string characterName = storyLine.Character;
                    string facePath = $"{folderPath}/{characterName}/{characterName}-{storyLine.Expression}.png";
                    _face.sprite = Resources.Load<Sprite>(facePath);
                    string nameStripPath = $"{folderPath}/NameStrips/{characterName}.png";
                    _nameStrip.sprite = Resources.Load<Sprite>(nameStripPath);
                    break;
                case TextBoxType.NoAvatar:
                    _face.sprite = null;
                    _nameStrip.sprite = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(storyLine.TextBoxType),
                        $"The specified text box type '{storyLine.TextBoxType}' is not allowed."
                    );
            }
        }

        private class EnterAnimation
        {
            private Dialog _dialog;

            public EnterAnimation(Dialog dialog)
            {
                _dialog = dialog;
            }

            public void ImmediateShow()
            {
                _dialog._dialogBox.color = Color.white;
                _dialog._face.color = Color.white;
                _dialog._nameStrip.color = Color.white;
                _dialog._textMeshPro.color = _dialog._textColor;
            }

            public void ImmediateHide()
            {
                _dialog._dialogBox.color = Color.clear;
                _dialog._face.color = Color.clear;
                _dialog._nameStrip.color = Color.clear;
                _dialog._textMeshPro.color = Color.clear;
            }

            public async UniTask FadeIn(CancellationToken token)
            {
                Color imageColor = _dialog._dialogBox.color;

                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (imageColor.a >= 1f)
                    {
                        imageColor.a = 1f;
                        _dialog._dialogBox.color = imageColor;
                        break;
                    }

                    if (_dialog._player.FastForward)
                    {
                        imageColor.a = 1f;
                        continue;
                    }

                    imageColor.a += _dialog._fadeSpeed * Time.deltaTime;
                    _dialog._dialogBox.color = imageColor;
                    _dialog._face.color = imageColor;
                    _dialog._nameStrip.color = imageColor;
                    await UniTask.Yield();
                }
            }

            public async UniTask FadeOut(CancellationToken token)
            {
                Color imageColor = _dialog._dialogBox.color;
                Color textColor = _dialog._textMeshPro.color;

                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (imageColor.a <= 0f)
                    {
                        _dialog._dialogBox.color = Color.clear;
                        break;
                    }

                    float speed = _dialog._player.FastForward ? _dialog._fastForwardFadeSpeed : _dialog._fadeSpeed;
                    imageColor.a -= speed * Time.deltaTime;
                    _dialog._dialogBox.color = imageColor;
                    _dialog._face.color = imageColor;
                    _dialog._nameStrip.color = imageColor;
                    textColor.a = imageColor.a;
                    _dialog._textMeshPro.color = textColor;
                    await UniTask.Yield();
                }
            }
        }
    }
}
