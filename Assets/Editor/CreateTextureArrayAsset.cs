using UnityEditor;
using UnityEngine;

public static class CreateTextureArrayAsset
{
    [MenuItem("Assets/Create/Rendering/Texture 2D Array")]
    private static void Create()
    {
        var asset = new Texture2DArray(
            4096,
            4096,
            16,
            TextureFormat.R8,
            false,
            true
        );

        AssetDatabase.CreateAsset(asset, "Assets/WorldTextureArray.asset");
        AssetDatabase.SaveAssets();
        Selection.activeObject = asset;
    }
}
