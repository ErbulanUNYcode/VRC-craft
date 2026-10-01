using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.SDKBase;

namespace VRC_MINE.UI
{

	public class VRHeadUi : UdonSharpBehaviour
	{
		[SerializeField] private RectTransform inv;
		[SerializeField] private Material UiMaterial;
		[SerializeField] private Material UiTextMaterial;
		[SerializeField] private MeshRenderer[] texts;
		[SerializeField] private RectTransform renderImage;
		[SerializeField] private TextMeshPro playerPosition;

		private VRCPlayerApi localPlayer;
		private float eyeDist;
		private void Start()
		{
			localPlayer = Networking.LocalPlayer;
			if (!localPlayer.IsUserInVR())
			{
				Destroy(gameObject);
				return;
			}
			foreach (var text in texts) text.bounds = new Bounds(Vector3.zero, Vector3.one * 100000000);
			eyeDist = Vector2.Distance(VRCCameraSettings.GetEyePosition(Camera.StereoscopicEye.Left), VRCCameraSettings.GetEyePosition(Camera.StereoscopicEye.Right)) / 2 / localPlayer.GetAvatarEyeHeightAsMeters();
		}

		private void Update()
		{
			var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

			transform.rotation = Quaternion.Slerp(transform.rotation, head.rotation, 0.1f);

			renderImage.position = head.position + head.rotation * Vector3.forward * 0.1f;

			playerPosition.text = Mathf.Round(head.position.x) + "," + Mathf.Round(head.position.y) + "," + Mathf.Round(head.position.z);

			var offset = VRCCameraSettings.ScreenCamera.Right * eyeDist * localPlayer.GetAvatarEyeHeightAsMeters();

			UiMaterial.SetVector("_EyeOffset", offset);
			UiTextMaterial.SetVector("_EyeOffset", offset);

			var leftHand = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);

			inv.rotation = leftHand.rotation;
			inv.position = leftHand.position - head.position;
		}

		private void LateUpdate()
		{
		}
	}
}