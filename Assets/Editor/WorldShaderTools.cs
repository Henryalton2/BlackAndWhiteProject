using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class WorldShaderTools
{
    // ── collect every material that needs updating ───────────────────────

    static List<Material> GetWorldMaterials()
    {
        var mats = new List<Material>();

        // Terrain material (whatever is currently assigned)
        Terrain terrain = Object.FindObjectOfType<Terrain>();
        if (terrain != null && terrain.materialTemplate != null)
            mats.Add(terrain.materialTemplate);

        // Any additional named materials kept for legacy reasons
        string[] extraPaths = new[]
        {
            "Assets/art/Triplanarfix.mat",
            "Assets/art/Triplanarfix 1.mat",
            "Assets/Shaders/TriplanarPaintable.mat",
        };

        foreach (string path in extraPaths)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null && !mats.Contains(m))
                mats.Add(m);
        }

        return mats;
    }

    static void SetOnAll(string property, float value)
    {
        var mats = GetWorldMaterials();
        if (mats.Count == 0)
        {
            Debug.LogWarning("WorldShaderTools: no materials found.");
            return;
        }

        foreach (var mat in mats)
        {
            if (mat.HasProperty(property))
            {
                mat.SetFloat(property, value);
                EditorUtility.SetDirty(mat);
            }
        }
    }

    // ── menu items ───────────────────────────────────────────────────────

    [MenuItem("World Tools/Set Day")]
    public static void SetDay()
    {
        SetOnAll("_Invert", 0f);
        Debug.Log("Set to Day");
    }

    [MenuItem("World Tools/Set Night")]
    public static void SetNight()
    {
        SetOnAll("_Invert", 1f);
        Debug.Log("Set to Night");
    }

    [MenuItem("World Tools/Set World Bend To 0")]
    public static void SetWorldBendZero()
    {
        SetOnAll("_BendAmount", 0f);
        Debug.Log("World Bend set to 0");
    }
}
