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

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance < medDistance)
        {
            spriteRenderer.sprite = highResSprite;
        }
        else if (distance < lowDistance)
        {
            spriteRenderer.sprite = medResSprite;
        }
        else
        {
            spriteRenderer.sprite = lowResSprite;
        }
    }
}