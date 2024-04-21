using UnityEngine;
using CG;

public class Test : MonoBehaviour
{
    private ICGPlayerInterface _player;

    private bool _isPlaying = true;
    private bool _isHiding = false;

    private void Awake()
    {
        _player = FindObjectOfType<CGPlayer>();
    }

    private async void Start()
    {
        await _player.Initialize("1-1");
        _player.Play();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (_isPlaying)
            {
                _player.Pause();
            }
            else
            {
                _player.Resume();
            }
            _isPlaying = !_isPlaying;
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            _player.Stop();
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (_isHiding)
            {
                _player.ShowText();
            }
            else
            {
                _player.HideText();
            }
            _isHiding = !_isHiding;
        }
    }
}