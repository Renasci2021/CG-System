using UnityEngine;
using CG;

public class Test : MonoBehaviour
{
    private CGPlayer _player;

    private void Awake()
    {
        _player = FindObjectOfType<CGPlayer>();
    }

    private async void Start()
    {
        await _player.Initialize("1-1");
        _player.Play();
    }
}