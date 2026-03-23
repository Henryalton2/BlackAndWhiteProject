using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class WaterReflection : MonoBehaviour
{
    // referenses
    Camera mainCamera;
    Camera reflectionCamera;

    [Tooltip("The plane where the camera will be reflected, the water plane or any object with the same position and rotation")]
    public Transform reflectionPlane;
    [Tooltip("The texture used by the Water shader to display the reflection")]
    public RenderTexture outputTexture;

    // parameters
    public bool copyCameraParameters = true;
    public float clipPlaneOffset = 0.07f;

    // cache
    private Transform mainCamTransform;
    private Transform reflectionCamTransform;

    public void Awake()
    {
        mainCamera = Camera.main;

        reflectionCamera = GetComponent<Camera>();

        if (mainCamera != null)
            mainCamTransform = mainCamera.transform;

        reflectionCamTransform = reflectionCamera.transform;

        reflectionCamera.enabled = false;
    }

    private void LateUpdate()
    {
        if (mainCamera == null || reflectionPlane == null)
            return;
        RenderReflection();
    }

    private void RenderReflection()
    {
        if (copyCameraParameters)
        {
            reflectionCamera.CopyFrom(mainCamera);
            reflectionCamera.targetTexture = outputTexture;
        }

        Vector3 planeNormal = reflectionPlane.up;
        Vector3 planePosition = reflectionPlane.position;

        Vector3 camPosition = mainCamTransform.position;

        float d = -Vector3.Dot(planeNormal, planePosition);
        float distance = Vector3.Dot(planeNormal, camPosition) + d;

        Vector3 reflectedPosition = camPosition - 2 * distance * planeNormal;

        Vector3 forward = mainCamTransform.forward;
        Vector3 up = mainCamTransform.up;

        Vector3 reflectedForward = Vector3.Reflect(forward, planeNormal);
        Vector3 reflectedUp = Vector3.Reflect(up, planeNormal);

        reflectionCamTransform.position = reflectedPosition;
        reflectionCamTransform.rotation = Quaternion.LookRotation(reflectedForward, reflectedUp);

        Vector4 clipPlane = CameraSpacePlane(reflectionCamera, planePosition, planeNormal, 1.0f);
        reflectionCamera.projectionMatrix = mainCamera.CalculateObliqueMatrix(clipPlane);

        reflectionCamera.Render();
    }

    Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * clipPlaneOffset;

        Matrix4x4 m = cam.worldToCameraMatrix;

        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;

        return new Vector4(
            cnormal.x,
            cnormal.y,
            cnormal.z,
            -Vector3.Dot(cpos, cnormal)
        );
    }
}