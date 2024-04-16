using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace CG
{
    internal class Scene : MonoBehaviour
    {
        [SerializeField] private float _fadeSpeed = 1f;  // 渐变速度

        [SerializeField] private Sprite[] _frames;  // 帧动画
        [SerializeField] private int _frameRate = 8;  // 帧率

        private Image _background;  // 背景图片

        private int _currentFrameIndex = 0;  // 当前帧索引

        public async UniTask Enter(CancellationToken token)
        {
            gameObject.SetActive(true);
            Color color = _background.color;

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (color.a >= 1f)
                {
                    color.a = 1f;
                    _background.color = color;
                    break;
                }

                color.a += _fadeSpeed * Time.deltaTime;
                _background.color = color;
                await UniTask.Yield();
            }
        }

        public async UniTask Exit(CancellationToken token)
        {
            Color color = _background.color;

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                if (color.a <= 0f)
                {
                    color.a = 0f;
                    _background.color = color;
                    gameObject.SetActive(false);
                    break;
                }

                color.a -= _fadeSpeed * Time.deltaTime;
                _background.color = color;
                await UniTask.Yield();
            }
        }

        public async UniTask PlayAnimation(CancellationToken token)
        {
            while (_currentFrameIndex < _frames.Length)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                _background.sprite = _frames[_currentFrameIndex];
                int interval = 1000 / _frameRate;
                await UniTask.Delay(interval, cancellationToken: token);
                _currentFrameIndex++;
            }
        }

        private void Awake()
        {
            _background = GetComponent<Image>();

            Color color = _background.color;
            color.a = 0f;
            _background.color = color;
            gameObject.SetActive(false);
        }
    }
}
