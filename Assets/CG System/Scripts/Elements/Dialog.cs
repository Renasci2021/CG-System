using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CG
{
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

        public override void Initialize(CGPlayer player)
        {
            base.Initialize(player);

            Color color = Color.white;
            color.a = 0f;
            _dialogBox.color = color;
            _face.color = color;
            _nameStrip.color = color;
            gameObject.SetActive(false);
        }

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
                gameObject.SetActive(true);
                _isShowing = true;
                await _animator.FadeIn(token);
            }

            await TypeText(token);
        }

        public override async UniTask Exit(CancellationToken token)
        {
            await _animator.FadeOut(token);
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

            // TODO: Replace with Addressable Asset System
            string folderPath = "Assets/CG System/Art/Characters";
            string characterName = storyLine.Character;
            string facePath, nameStripPath;
            switch (storyLine.TextBoxType)
            {
                case TextBoxType.Normal:
                    _dialogBox.sprite = _dialogBoxes[(int)TextBoxType.Normal];
                    facePath = $"{folderPath}/{characterName}/{characterName}-{storyLine.Expression}.png";
                    _face.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(facePath);
                    nameStripPath = $"{folderPath}/NameStrips/{characterName}.png";
                    _nameStrip.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(nameStripPath);
                    break;
                case TextBoxType.NoAvatar:
                    _dialogBox.sprite = _dialogBoxes[(int)TextBoxType.NoAvatar];
                    _face.sprite = null;
                    _face.color = Color.clear;
                    nameStripPath = $"{folderPath}/NameStrips/{characterName}.png";
                    _nameStrip.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(nameStripPath);
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
                _dialog._textMeshPro.color = _dialog._textColor;
                _dialog._face.color = _dialog._face.sprite != null ? Color.white : Color.clear;
                _dialog._nameStrip.color = _dialog._nameStrip.sprite != null ? Color.white : Color.clear;
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
                _dialog._isEntering = true;
                Color imageColor = _dialog._dialogBox.color;

                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (imageColor.a >= 1f)
                    {
                        _dialog._isEntering = false;
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
                _dialog._isExiting = true;
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
