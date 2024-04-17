using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CG
{
    internal class Narration : TextBlock
    {
        [SerializeField] private float _fadeSpeed = 1f; // 渐变速度
        [SerializeField] private float _fastForwardFadeSpeed = 10f; // 快进时渐变速度

        private Image _image;   // 旁白框

        private bool _isEntering = false;
        private bool _isExiting = false;

        public override async UniTask Enter(StoryLine storyLine, CancellationToken token)
        {
            InitializeLine(storyLine);
            gameObject.SetActive(true);
            _isEntering = true;
            Color imageColor = _image.color;

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (imageColor.a >= 1f)
                {
                    _isEntering = false;
                    imageColor.a = 1f;
                    _image.color = imageColor;
                    break;
                }

                if (_player.FastForward)
                {
                    imageColor.a = 1f;
                    continue;
                }

                imageColor.a += _fadeSpeed * Time.deltaTime;
                _image.color = imageColor;
                await UniTask.Yield();
            }

            await TypeText(token);
        }

        public override async UniTask Exit(CancellationToken token)
        {
            _isExiting = true;
            Color imageColor = _image.color;
            Color textColor = _textMeshPro.color;

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (imageColor.a <= 0f)
                {
                    _isExiting = false;
                    _image.color = Color.clear;
                    _textMeshPro.color = Color.clear;
                    gameObject.SetActive(false);
                    break;
                }

                float speed = _player.FastForward ? _fastForwardFadeSpeed : _fadeSpeed;
                imageColor.a -= speed * Time.deltaTime;
                textColor.a = imageColor.a;
                _image.color = imageColor;
                _textMeshPro.color = textColor;
                await UniTask.Yield();
            }
        }

        public override void Skip()
        {
            if (_isEntering)
            {
                _image.color = new(_image.color.r, _image.color.g, _image.color.b, 1f);
            }
            if (_isExiting)
            {
                _image.color = Color.clear;
                _textMeshPro.color = Color.clear;
            }

            SkipTyping();
        }

        protected override void Awake()
        {
            base.Awake();
            _image = GetComponent<Image>();
            gameObject.SetActive(false);
        }
    }
}
