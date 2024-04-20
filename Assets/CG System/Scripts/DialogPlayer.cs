using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CG
{
    public class DialogPlayer : MonoBehaviour
    {
        private Dialog[] _dialogs;

        private Dialog _currentDialog = null;
        private Dialog _nextDialog = null;

        public void Initialize(CGPlayer player)
        {
            foreach (Dialog dialog in _dialogs)
            {
                dialog.Initialize(player);
            }
        }

        public async UniTask Enter(StoryLine storyLine, CancellationToken token)
        {
            _nextDialog = _dialogs[(int)storyLine.DialogBoxType];
            _nextDialog.InitializeLine(storyLine);
            if (_currentDialog != null)
            {
                await _currentDialog.Exit(token);
            }
            await _nextDialog.Enter(token);
            _currentDialog = _nextDialog;
            _nextDialog = null;
        }

        public async UniTask Exit(CancellationToken token)
        {
            if (_currentDialog == null)
            {
                return;
            }

            await _currentDialog.Exit(token);
            _currentDialog = null;
        }

        private void Awake()
        {
            _dialogs = GetComponentsInChildren<Dialog>();
        }
    }
}
