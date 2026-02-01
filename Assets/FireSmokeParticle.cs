using UnityEngine;

public class FireSmokeParticle : MonoBehaviour
{
    public float riseSpeed = 0.4f;
    public float lifetime = 3f;
    public float scaleGrowSpeed = 0.2f;

    float age;
    Transform sprite;
    SpriteRenderer sr;

    void Awake()
    {
        sprite = transform.GetChild(0);
        sr = sprite.GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        age = 0f;
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.one;

        if (sr)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
    }

    void Update()
    {
        age += Time.deltaTime;

        // ROOT rises (no bend conflict)
        transform.localPosition += Vector3.up * riseSpeed * Time.deltaTime;

        // Sprite scales
        sprite.localScale += Vector3.one * scaleGrowSpeed * Time.deltaTime;

        // Fade
        if (sr)
        {
            float alpha = Mathf.Lerp(1f, 0f, age / lifetime);
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }

        if (age >= lifetime)
            gameObject.SetActive(false);
    }
}
