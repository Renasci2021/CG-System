using UnityEngine;
using CG;

public class Test : MonoBehaviour
{
    private CGPlayer _player;

    private void Awake()
    {
        _player = FindObjectOfType<CGPlayer>();
        _player.Initialize("1-1");
    }

    private void Start()
    {
        _player.Play();
    }
}