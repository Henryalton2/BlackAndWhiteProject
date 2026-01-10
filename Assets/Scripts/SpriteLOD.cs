using UnityEngine;

public class SpriteLOD : MonoBehaviour
{
    [SerializeField] private Sprite highResSprite;
    [SerializeField] private Sprite medResSprite;
    [SerializeField] private Sprite lowResSprite;
    [SerializeField] private float medDistance = 20f;
    [SerializeField] private float lowDistance = 40f;

    private SpriteRenderer spriteRenderer;
    private Transform player;
    private Sprite currentSprite;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        currentSprite = spriteRenderer.sprite;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        Sprite targetSprite = null;

        if (distance < medDistance)
        {
            targetSprite = highResSprite;
        }
        else if (distance < lowDistance)
        {
            targetSprite = medResSprite;
        }
        else
        {
            targetSprite = lowResSprite;
        }

        // Only update if sprite changed
        if (targetSprite != null && targetSprite != currentSprite)
        {
            spriteRenderer.sprite = targetSprite;
            currentSprite = targetSprite;
        }
    }
}