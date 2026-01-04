using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CameraScreenshot : MonoBehaviour
{
    [Header("Capture Settings")]
    public Camera targetCamera;
    public int width = 1920;
    public int height = 1080;
    public string fileName = "Screenshot.png";

    public void Capture()
    {
        if (targetCamera == null)
        {
            Debug.LogError("CameraScreenshot: Target Camera is NULL");
            return;
        }

        RenderTexture rt = new RenderTexture(width, height, 24);
        targetCamera.targetTexture = rt;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        targetCamera.Render();
        RenderTexture.active = rt;

        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        targetCamera.targetTexture = null;
        RenderTexture.active = null;

        byte[] bytes = tex.EncodeToPNG();
        string path = Path.Combine(Application.dataPath, fileName);
        File.WriteAllBytes(path, bytes);

        DestroyImmediate(rt);
        DestroyImmediate(tex);

        Debug.Log("Screenshot saved at: " + path);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CameraScreenshot))]
public class CameraScreenshotEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        CameraScreenshot screenshot = (CameraScreenshot)target;

        if (GUILayout.Button("📸 Capture Screenshot", GUILayout.Height(30)))
        {
            screenshot.Capture();
        }
    }
}
#endif
