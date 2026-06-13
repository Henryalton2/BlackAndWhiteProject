using UnityEngine;

public class CloudMover : MonoBehaviour
{
    [HideInInspector] public Vector3 moveDirection = Vector3.right;
    [HideInInspector] public float speed = 1f;

    void Update()
    {
        // Move cloud
        transform.position += moveDirection * speed * Time.deltaTime;
    }
}
