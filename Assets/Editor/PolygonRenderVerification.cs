#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VRC_MINE.World;

[InitializeOnLoad]
public static class PolygonRenderVerification
{
    [MenuItem("Tools/Verify Polygon Renderer _F6")]
    private static void Run()
    {
        SessionState.SetBool("PolygonRenderVerification.started", false);
        Begin();
    }
    static PolygonRenderVerification()
    {
    }

    private static void Diagnose()
    {
        if (!EditorApplication.isPlaying) return;
        foreach (GenLayerController c in Object.FindObjectsOfType<GenLayerController>()) Debug.Log("POLYGON_VERIFY initializer " + c.name + " children=" + c.transform.childCount);
        foreach (Camera c in Object.FindObjectsOfType<Camera>()) Debug.Log("POLYGON_VERIFY camera " + c.name + " pos=" + c.transform.position + " mask=" + c.cullingMask);
        int count=0;
        foreach (MeshFilter f in Object.FindObjectsOfType<MeshFilter>()) if(f.sharedMesh!=null && f.GetComponent<Renderer>()!=null && f.GetComponent<Renderer>().sharedMaterial!=null && f.GetComponent<Renderer>().sharedMaterial.shader.name=="VRC_MINE/WorldShow") count++;
        Debug.Log("POLYGON_VERIFY polygon mesh count="+count);
    }

    private static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool("PolygonRenderVerification.started", false)) return;
        SessionState.SetBool("PolygonRenderVerification.started", true);
        GenLayerController controller = Object.FindObjectOfType<GenLayerController>();
        if (controller == null) { Debug.LogError("POLYGON_VERIFY: initializer missing"); return; }
        SerializedObject settings = new SerializedObject(controller);
        SerializedProperty zone = settings.FindProperty("voxelWorldZones").GetArrayElementAtIndex(0);
        zone.FindPropertyRelative("updateZoneCenter").vector3Value = new Vector3(8,8,65);
        zone.FindPropertyRelative("updateZoneSize").vector3Value = new Vector3(16,16,130);
        settings.ApplyModifiedProperties();
        Debug.Log("POLYGON_VERIFY: starting Play Mode with corrected world zone");
        EditorApplication.isPlaying = true;
    }
}
#endif
