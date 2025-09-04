using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class RotateLightCamera : UdonSharpBehaviour
{
	private VRCPlayerApi localPlayer;
	[SerializeField] private Transform right;
	[SerializeField] private Transform left;
	[SerializeField] private Transform getRight;
	[SerializeField] private Transform getLeft;

	private void Start()
	{
		localPlayer = Networking.LocalPlayer;
	}

	private void Update()
	{
		transform.position = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
		transform.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
		transform.localScale = localPlayer.GetAvatarEyeHeightAsMeters() * Vector3.one;
		left.position = getLeft.position;
		right.position = getRight.position;
	}

	public void SetTime(float time)
	{
		float angle = time * 360.0f;

		Vector3 euler;

		euler.x = -50f;
		euler.y = -90f;
		euler.z = angle;

		right.localEulerAngles = euler;
		if (left != null)
		{
			left.localEulerAngles = euler;
		}
	}
}
