using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


/// <summary>
/// Planar water reflection system that renders a mirrored camera view to a
/// render texture and applies it to the attached material.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class PlanarReflection : MonoBehaviour
{
    [Header("Quality")]
    public int textureSize = 512;
    [Range(1, 4)]
    public int renderEveryNFrames = 1;
    [Header("Content")]
    public LayerMask reflectionLayers = ~0;
    public bool reflectSky = true;
    [Header("Appearance")]
    [Range(0f, 1f)] public float reflectionStrength = 0.7f;
    [Range(0f, 0.1f)] public float distortion = 0.02f;
    [Range(0f, 1f)] public float planarBlend = 1f;
    [Range(0f, 0.5f)] public float clipPlaneOffset = 0.05f;

    private float lodDistance = 40f;
    private float motionThreshold = 0.0001f;
    private bool frustumCull = true;
    private bool lightweightFormat = true;

    private Camera _reflCam;
    private RenderTexture _reflRT;
    private Material _mat;
    private int _frameCount;
    private int _currentTexSize;

    private Matrix4x4 _lastViewMatrix;
    private bool _hasRenderedOnce;
    private static readonly Matrix4x4 _yFlip;

    static PlanarReflection()
    {
        _yFlip = Matrix4x4.identity;
        _yFlip.m11 = -1f;
    }

    private static readonly int ID_ReflTex = Shader.PropertyToID("_ReflectionTex");
    private static readonly int ID_ReflStrength = Shader.PropertyToID("_ReflectionStrength");
    private static readonly int ID_ReflDistort = Shader.PropertyToID("_ReflectionDistortion");
    private static readonly int ID_PlanarBlend = Shader.PropertyToID("_PlanarBlend");

    private readonly Vector3[] _boundsCorners = new Vector3[8];

    void OnEnable()
    {
        _mat = GetComponent<Renderer>().material;
        _currentTexSize = textureSize;
        CreateResources(_currentTexSize);
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        DestroyResources();
    }

    void LateUpdate()
    {
        if (_mat == null) return;
        _mat.SetFloat(ID_ReflStrength, reflectionStrength);
        _mat.SetFloat(ID_ReflDistort, distortion);
        _mat.SetFloat(ID_PlanarBlend, planarBlend);
    }

    void OnBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
    {
        if (renderingCamera == _reflCam) return;
        if (renderingCamera.cameraType == CameraType.Reflection) return;
        if (renderingCamera.cameraType == CameraType.Preview) return;

        _frameCount++;
        if (_frameCount % renderEveryNFrames != 0) return;
        if (frustumCull && !IsVisibleFrom(renderingCamera)) return;
        float dist = Vector3.Distance(renderingCamera.transform.position, transform.position);
        int targetSize = (dist > lodDistance) ? Mathf.Max(64, textureSize / 2) : textureSize;

        if (_reflRT == null || _currentTexSize != targetSize)
        {
            DestroyResources();
            _currentTexSize = targetSize;
            CreateResources(_currentTexSize);
        }
        Matrix4x4 currentView = renderingCamera.worldToCameraMatrix;
        if (_hasRenderedOnce && MatricesApproxEqual(currentView, _lastViewMatrix, motionThreshold))
        {
            return;
        }
        _lastViewMatrix = currentView;
        _hasRenderedOnce = true;
        ConfigureReflectionCamera(renderingCamera);

        GL.invertCulling = true;
        UniversalRenderPipeline.RenderSingleCamera(context, _reflCam);
        GL.invertCulling = false;

        _mat.SetTexture(ID_ReflTex, _reflRT);
    }

    void ConfigureReflectionCamera(Camera src)
    {
        float waterY = transform.position.y;
        Matrix4x4 t = Matrix4x4.Translate(new Vector3(0f, -waterY, 0f));
        Matrix4x4 ti = Matrix4x4.Translate(new Vector3(0f, waterY, 0f));
        Matrix4x4 worldReflect = ti * _yFlip * t;

        _reflCam.worldToCameraMatrix = src.worldToCameraMatrix * worldReflect.inverse;

        _reflCam.fieldOfView = src.fieldOfView;
        _reflCam.aspect = src.aspect;
        _reflCam.nearClipPlane = src.nearClipPlane;
        _reflCam.farClipPlane = src.farClipPlane;
        _reflCam.cullingMask = reflectionLayers;

        if (reflectSky)
        {
            _reflCam.clearFlags = CameraClearFlags.Skybox;
            _reflCam.backgroundColor = src.backgroundColor;
        }
        else
        {
            _reflCam.clearFlags = CameraClearFlags.SolidColor;
            _reflCam.backgroundColor = Color.clear;
        }

        Vector4 clipPlane = GetClipPlaneInCameraSpace(_reflCam, waterY + clipPlaneOffset);
        _reflCam.projectionMatrix = _reflCam.CalculateObliqueMatrix(clipPlane);
    }

    Vector4 GetClipPlaneInCameraSpace(Camera cam, float height)
    {
        Vector3 planePoint = new Vector3(0f, height, 0f);
        Vector3 planeNormal = Vector3.up;

        Matrix4x4 worldToCam = cam.worldToCameraMatrix;
        Vector3 cpPos = worldToCam.MultiplyPoint(planePoint);
        Vector3 cpNorm = worldToCam.MultiplyVector(planeNormal).normalized;

        return new Vector4(cpNorm.x, cpNorm.y, cpNorm.z, -Vector3.Dot(cpPos, cpNorm));
    }
    bool IsVisibleFrom(Camera cam)
    {
        Bounds b = GetComponent<Renderer>().bounds;
        Vector3 min = b.min, max = b.max;
        _boundsCorners[0] = new Vector3(min.x, min.y, min.z);
        _boundsCorners[1] = new Vector3(max.x, min.y, min.z);
        _boundsCorners[2] = new Vector3(min.x, max.y, min.z);
        _boundsCorners[3] = new Vector3(max.x, max.y, min.z);
        _boundsCorners[4] = new Vector3(min.x, min.y, max.z);
        _boundsCorners[5] = new Vector3(max.x, min.y, max.z);
        _boundsCorners[6] = new Vector3(min.x, max.y, max.z);
        _boundsCorners[7] = new Vector3(max.x, max.y, max.z);

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        return GeometryUtility.TestPlanesAABB(planes, b);
    }

    static bool MatricesApproxEqual(Matrix4x4 a, Matrix4x4 b, float threshold)
    {
        return Mathf.Abs(a.m00 - b.m00) < threshold &&
               Mathf.Abs(a.m01 - b.m01) < threshold &&
               Mathf.Abs(a.m02 - b.m02) < threshold &&
               Mathf.Abs(a.m03 - b.m03) < threshold &&
               Mathf.Abs(a.m10 - b.m10) < threshold &&
               Mathf.Abs(a.m11 - b.m11) < threshold &&
               Mathf.Abs(a.m12 - b.m12) < threshold &&
               Mathf.Abs(a.m13 - b.m13) < threshold &&
               Mathf.Abs(a.m20 - b.m20) < threshold &&
               Mathf.Abs(a.m21 - b.m21) < threshold &&
               Mathf.Abs(a.m22 - b.m22) < threshold &&
               Mathf.Abs(a.m23 - b.m23) < threshold;
    }

    void CreateResources(int size)
    {
        var fmt = lightweightFormat
            ? RenderTextureFormat.RGB111110Float
            : RenderTextureFormat.ARGB32;
        if (lightweightFormat && !SystemInfo.SupportsRenderTextureFormat(fmt))
            fmt = RenderTextureFormat.ARGB32;

        _reflRT = new RenderTexture(size, size, 16, fmt);
        _reflRT.name = "_WaterReflectionRT";
        _reflRT.filterMode = FilterMode.Bilinear;
        _reflRT.wrapMode = TextureWrapMode.Clamp;
        _reflRT.antiAliasing = 1;
        _reflRT.Create();

        var go = new GameObject("_WaterReflectionCam");
        go.hideFlags = HideFlags.HideAndDontSave;
        _reflCam = go.AddComponent<Camera>();
        _reflCam.enabled = false;
        _reflCam.targetTexture = _reflRT;

        var urpData = go.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderShadows = false;
        urpData.requiresColorTexture = false;
        urpData.requiresDepthTexture = false;
        urpData.renderPostProcessing = false;
        urpData.antialiasing = AntialiasingMode.None;
    }

    void DestroyResources()
    {
        if (_reflCam != null) { DestroyImmediate(_reflCam.gameObject); _reflCam = null; }
        if (_reflRT != null) { _reflRT.Release(); DestroyImmediate(_reflRT); _reflRT = null; }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_mat == null) return;
        _mat.SetFloat(ID_ReflStrength, reflectionStrength);
        _mat.SetFloat(ID_ReflDistort, distortion);
        _mat.SetFloat(ID_PlanarBlend, planarBlend);
    }
#endif
}