using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class AttachCameraToHead : UdonSharpBehaviour
{
	private VRCPlayerApi player;
	[SerializeField] private Transform cameraTransform;

	void Start()
	{
		player = Networking.LocalPlayer;
	}

	void Update()
	{
		if (player != null)
		{
			Vector3 playerPosition = (player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position + player.GetPosition()) / 2;
			Quaternion headRotation = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;

			Vector3 euler = headRotation.eulerAngles;
			Quaternion onlyYawRotation = Quaternion.Euler(0, euler.y, 0);

			transform.position = playerPosition;
			transform.rotation = onlyYawRotation;
			cameraTransform.localPosition = new Vector3(0, 0, player.GetAvatarEyeHeightAsMeters() * 1.1f);
		}
	}
}
