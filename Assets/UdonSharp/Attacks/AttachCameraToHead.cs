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
			Vector3 headPosition = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
			Quaternion headRotation = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;

			Vector3 euler = headRotation.eulerAngles;
			Quaternion onlyYawRotation = Quaternion.Euler(0, euler.y, 0);

			transform.position = headPosition;
			transform.rotation = onlyYawRotation;
			//x = 0, y = - player high / 2, z = player high * 1.1
			cameraTransform.localPosition = new Vector3(0, -player.GetAvatarEyeHeightAsMeters() * 0.45f, player.GetAvatarEyeHeightAsMeters() * 1.1f);
		}
	}
}
