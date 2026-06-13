using UnityEngine;
using UnityEditor;
using System.IO;

public class GreyscaleTextureConverter
{
    [MenuItem("Assets/Make Greyscale Copy", true)]
    static bool Validate()
    {
        return Selection.activeObject is Texture2D;
    }

    [MenuItem("Assets/Make Greyscale Copy")]
    static void MakeGreyscaleCopy()
    {
        Texture2D source = Selection.activeObject as Texture2D;
        if (source == null) return;

        string sourcePath = AssetDatabase.GetAssetPath(source);
        string dir        = Path.GetDirectoryName(sourcePath);
        string name       = Path.GetFileNameWithoutExtension(sourcePath);
        string newPath    = Path.Combine(dir, name + "_greyscale.png").Replace('\\', '/');

        // Temporarily enable Read/Write if needed
        TextureImporter importer  = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        bool wasReadable = importer != null && importer.isReadable;
        if (importer != null && !wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        // Convert pixels to greyscale
        Color[] pixels = source.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            float g = pixels[i].r * 0.299f + pixels[i].g * 0.587f + pixels[i].b * 0.114f;
            pixels[i] = new Color(g, g, g, pixels[i].a);
        }

        // Build and save new texture
        Texture2D result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        result.SetPixels(pixels);
        result.Apply();
        File.WriteAllBytes(newPath, result.EncodeToPNG());
        Object.DestroyImmediate(result);

        // Restore original readable state
        if (importer != null && !wasReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        AssetDatabase.ImportAsset(newPath);
        AssetDatabase.Refresh();

        Texture2D newTex = AssetDatabase.LoadAssetAtPath<Texture2D>(newPath);
        Selection.activeObject = newTex;
        EditorGUIUtility.PingObject(newTex);

        Debug.Log("Greyscale copy saved: " + newPath);
    }
}
