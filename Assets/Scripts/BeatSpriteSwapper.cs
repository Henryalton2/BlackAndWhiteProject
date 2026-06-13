using UnityEngine;

/// <summary>
/// Attach to your tree prefab. Listens to TriggerPlaySound.OnBeat
/// and swaps sprites in sync — no audio ownership needed.
/// </summary>
public class BeatSpriteSwapper : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private SwapMode swapMode = SwapMode.Cycle;
    [SerializeField] private int swapEveryNBeats = 1;

    public enum SwapMode { Cycle, Random, PingPong }

    private SpriteRenderer _spriteRenderer;
    private int _currentIndex = 0;
    private int _pingPongDir = 1;
    private int _beatCounter = 0;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        TriggerPlaySound.OnBeat += HandleBeat;
    }

    void OnDisable()
    {
        TriggerPlaySound.OnBeat -= HandleBeat;
    }

    private void HandleBeat()
    {
        if (sprites == null || sprites.Length == 0) return;

        _beatCounter++;
        if (_beatCounter < swapEveryNBeats) return;
        _beatCounter = 0;

        switch (swapMode)
        {
            case SwapMode.Cycle:
                _currentIndex = (_currentIndex + 1) % sprites.Length;
                break;

            case SwapMode.Random:
                int next;
                do { next = Random.Range(0, sprites.Length); }
                while (sprites.Length > 1 && next == _currentIndex);
                _currentIndex = next;
                break;

            case SwapMode.PingPong:
                _currentIndex += _pingPongDir;
                if (_currentIndex >= sprites.Length - 1 || _currentIndex <= 0)
                    _pingPongDir *= -1;
                _currentIndex = Mathf.Clamp(_currentIndex, 0, sprites.Length - 1);
                break;
        }

        _spriteRenderer.sprite = sprites[_currentIndex];
    }
}