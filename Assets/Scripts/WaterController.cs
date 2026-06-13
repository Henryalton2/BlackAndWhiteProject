using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Runtime controller for water shader properties.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class WaterController : MonoBehaviour
{
    [Header("Wind")]
    [Range(0f, 10f)]
    public float windSpeed = 2f;
    [Range(0f, 360f)]
    public float windAngle = 0f;
    [Range(0f, 5f)]
    public float windTransitionSpeed = 1f;
    [Header("Waves")]
    [Range(0f, 2f)]
    public float waveHeight = 0.15f;
    [Range(0.1f, 10f)]
    public float waveFrequency = 1.5f;
    [Range(0f, 5f)]
    public float waveSpeed = 1.0f;
    [Range(0f, 1f)]
    public float waveSteepness = 0.5f;
    [Header("Appearance")]
    public Color shallowColor = new Color(0.08f, 0.58f, 0.68f, 0.75f);
    public Color deepColor    = new Color(0.01f, 0.12f, 0.35f, 0.95f);
    [Range(0f, 1f)]
    public float smoothness = 0.92f;
    [Range(0f, 3f)]
    public float normalStrength = 1.2f;
    [Header("Foam")]
    [Range(0f, 1f)]
    public float foamWindStreak = 0.3f;
    private Material   _mat;
    private float      _currentWindSpeed;
    private static readonly int ID_WindSpeed      = Shader.PropertyToID("_WindSpeed");
    private static readonly int ID_WindDirection  = Shader.PropertyToID("_WindDirection");
    private static readonly int ID_WaveHeight     = Shader.PropertyToID("_WaveHeight");
    private static readonly int ID_WaveFrequency  = Shader.PropertyToID("_WaveFrequency");
    private static readonly int ID_WaveSpeed      = Shader.PropertyToID("_WaveSpeed");
    private static readonly int ID_WaveSteepness  = Shader.PropertyToID("_WaveSteepness");
    private static readonly int ID_ShallowColor   = Shader.PropertyToID("_ShallowColor");
    private static readonly int ID_DeepColor      = Shader.PropertyToID("_DeepColor");
    private static readonly int ID_Smoothness     = Shader.PropertyToID("_Smoothness");
    private static readonly int ID_NormalStrength = Shader.PropertyToID("_NormalStrength");
    private static readonly int ID_FoamWindStreak = Shader.PropertyToID("_FoamWindStreak");

    void Awake()
    {
        _mat = GetComponent<Renderer>().material;
        _currentWindSpeed = windSpeed;
    }
    void Update()
    {
        if (windTransitionSpeed > 0f)
            _currentWindSpeed = Mathf.Lerp(_currentWindSpeed, windSpeed, Time.deltaTime * windTransitionSpeed);
        else
            _currentWindSpeed = windSpeed;
        PushToMaterial();
    }
    public void SetWind(float speed, float angleDeg)
    {
        windSpeed = speed;
        windAngle = angleDeg;
    }
    public void SetWindSpeedTarget(float target) => windSpeed = target;
    public void SetWindDirectionVector(Vector2 dir)
    {
        if (dir == Vector2.zero) return;
        windAngle = Vector2.SignedAngle(Vector2.right, dir.normalized);
        if (windAngle < 0f) windAngle += 360f;
    }
    void PushToMaterial()
    {
        if (_mat == null) return;

        float rad     = windAngle * Mathf.Deg2Rad;
        var   windDir = new Vector4(Mathf.Cos(rad), 0f, Mathf.Sin(rad), 0f);

        _mat.SetFloat(ID_WindSpeed,      _currentWindSpeed);
        _mat.SetVector(ID_WindDirection, windDir);
        _mat.SetFloat(ID_WaveHeight,     waveHeight);
        _mat.SetFloat(ID_WaveFrequency,  waveFrequency);
        _mat.SetFloat(ID_WaveSpeed,      waveSpeed);
        _mat.SetFloat(ID_WaveSteepness,  waveSteepness);
        _mat.SetColor(ID_ShallowColor,   shallowColor);
        _mat.SetColor(ID_DeepColor,      deepColor);
        _mat.SetFloat(ID_Smoothness,     smoothness);
        _mat.SetFloat(ID_NormalStrength, normalStrength);
        _mat.SetFloat(ID_FoamWindStreak, foamWindStreak);
    }
#if UNITY_EDITOR
    void OnValidate()
    {
        if (_mat == null)
        {
            var r = GetComponent<Renderer>();
            if (r != null) _mat = r.sharedMaterial;
        }
        if (_mat != null) PushToMaterial();
    }
#endif
}
