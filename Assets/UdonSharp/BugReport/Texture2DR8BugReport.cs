
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Rendering;

namespace VRC.BugReport
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class Texture2DR8BugReport : UdonSharpBehaviour
	{
		[SerializeField] private TextMeshProUGUI text;
		[SerializeField] private Material material;

		void Start()
		{
			var c = VRCCameraSettings.ScreenCamera;
			text.text = c.PixelWidth.ToString() + "/" + c.PixelHeight.ToString();
			var textureR8 = new Texture2D(8, 8, TextureFormat.R8, false);
			textureR8.filterMode = FilterMode.Point;

			for (int i = 0; i < 8; i++)
			{
				for (int j = 0; j < 8; j++)
				{
					textureR8.SetPixel(i, j, new Color32((byte)(i + j * 8), 0, 0, 1));
				}
			}
			textureR8.Apply();

			material.mainTexture = textureR8;
		}
	}
}
