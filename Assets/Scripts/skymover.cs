using UnityEngine;

public class SkyMover : MonoBehaviour
{
    [Header("Arc Movement Settings")]
    [SerializeField] private float duration = 5f; // Time to complete the arc
    [SerializeField] private Vector3 endPosition = new Vector3(50f, 20f, 0f); // Where to end up
    [SerializeField] private float arcHeight = 10f; // How high the arc curves

    [Header("Billboard Settings")]
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private Transform cameraTransform;

    [Header("Optional Settings")]
    [SerializeField] private bool loopBackAndForth = false;
    [SerializeField] private bool destroyAtEnd = true;

    private Vector3 startPosition;
    private Vector3 midPoint;
    private float timer = 0f;
    private bool movingForward = true;

    private void Start()
    {
        // Start position is wherever you placed it in the scene
        startPosition = transform.position;

        // Find camera
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Calculate the mid point (highest point of the arc)
        Vector3 centerPoint = (startPosition + endPosition) / 2f;
        midPoint = centerPoint + Vector3.up * arcHeight;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        if (t < 1f)
        {
            // Quadratic Bezier curve: B(t) = (1-t)²P0 + 2(1-t)tP1 + t²P2
            float oneMinusT = 1f - t;
            Vector3 position;

            if (movingForward)
            {
                position = (oneMinusT * oneMinusT * startPosition) +
                          (2f * oneMinusT * t * midPoint) +
                          (t * t * endPosition);
            }
            else
            {
                // Moving backward (swap start and end)
                position = (oneMinusT * oneMinusT * endPosition) +
                          (2f * oneMinusT * t * midPoint) +
                          (t * t * startPosition);
            }

            transform.position = position;
        }
        else
        {
            if (loopBackAndForth)
            {
                // Reset timer and flip direction
                timer = 0f;
                movingForward = !movingForward;
            }
            else if (destroyAtEnd)
            {
                Destroy(gameObject);
            }
        }

        // Make sprite face the camera (billboard effect)
        if (faceCamera && cameraTransform != null)
        {
            transform.LookAt(transform.position + cameraTransform.forward);
        }
    }
}