using UnityEngine;

public class DebugBirdMover : MonoBehaviour
{
    private Vector3 direction;
    private float speed;

    public void Init(Vector3 dir, float spd)
    {
        direction = dir.normalized;
        speed = spd;
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }
}
