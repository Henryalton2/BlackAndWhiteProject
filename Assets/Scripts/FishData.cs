using UnityEngine;

[CreateAssetMenu(fileName = "NewFish", menuName = "Fishing/Fish Data")]
public class FishData : ScriptableObject
{
    [Header("Identity")]
    public string fishName = "Bass";
    public Sprite fishIcon;
    [TextArea] public string description = "A common river fish.";

    [Header("Rarity")]
    [Range(0f, 1f)]
    public float spawnWeight = 1f;

    [Header("Minigame Difficulty Overrides")]
    [Tooltip("Leave at 0 to use FishingMinigameUI defaults")]
    public float fishZoneHeightOverride = 0f;
    public float fishZoneMaxSpeedOverride = 0f;
}