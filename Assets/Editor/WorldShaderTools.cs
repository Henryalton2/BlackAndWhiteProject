using UnityEngine;
using UnityEditor;

public static class WorldShaderTools
{
    // Change this to your actual terrain/world material
    private static string materialPath = "Assets/art/Triplanarfix.mat";

    private static Material GetMaterial()
    {
        return AssetDatabase.LoadAssetAtPath<Material>(materialPath);
    }

    [MenuItem("World Tools/Set World Bend To 0")]
    public static void SetWorldBendZero()
    {
        Material mat = GetMaterial();
        if (mat != null)
        {
            mat.SetFloat("_BendAmount", 0f);
            EditorUtility.SetDirty(mat);
            Debug.Log("World Bend set to 0");
        }
        else
        {
            Debug.LogError("Material not found. Check materialPath.");
        }
    }

    [MenuItem("World Tools/Set Day")]
    public static void SetDay()
    {
        Material mat = GetMaterial();
        if (mat != null)
        {
            mat.SetFloat("_Invert", 0f);
            EditorUtility.SetDirty(mat);
            Debug.Log("Set to Day");
        }
    }

    [MenuItem("World Tools/Set Night")]
    public static void SetNight()
    {
        Material mat = GetMaterial();
        if (mat != null)
        {
            mat.SetFloat("_Invert", 1f);
            EditorUtility.SetDirty(mat);
            Debug.Log("Set to Night");
        }
    }
}
