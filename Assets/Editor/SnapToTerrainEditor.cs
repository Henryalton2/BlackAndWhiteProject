using UnityEngine;
using UnityEditor;

public class SnapToTerrainEditor : EditorWindow
{
    [MenuItem("Tools/Snap Selected Objects To Terrain %#s")] // Ctrl+Shift+S
    public static void SnapSelectedObjects()
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogWarning("No active Terrain found!");
            return;
        }

        foreach (GameObject obj in Selection.gameObjects)
        {
            if (obj == null) continue;

            Vector3 pos = obj.transform.position;

            // Get terrain height at object's XZ position
            float terrainY = terrain.SampleHeight(pos) + terrain.transform.position.y;

            // Find the lowest point of the object
            float lowestY = pos.y;
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                lowestY = float.MaxValue;
                foreach (Renderer rend in renderers)
                    lowestY = Mathf.Min(lowestY, rend.bounds.min.y);
            }

            // Offset so bottom touches terrain
            float offset = pos.y - lowestY;

            obj.transform.position = new Vector3(pos.x, terrainY + offset, pos.z);
            Debug.Log($"Snapped {obj.name} to terrain at Y={obj.transform.position.y}");
        }
    }
}
