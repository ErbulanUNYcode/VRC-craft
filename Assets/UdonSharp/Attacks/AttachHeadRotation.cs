using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

class AttachHeadRotation : UdonSharpBehaviour
{
	private VRCPlayerApi player;

	void Start()
	{
		player = Networking.LocalPlayer;
	}

	void Update()
	{
		if (player != null)
		{
			Quaternion headRotation = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;

			transform.rotation = Quaternion.Slerp(transform.rotation, headRotation, 0.1f);
		}
	}
}
