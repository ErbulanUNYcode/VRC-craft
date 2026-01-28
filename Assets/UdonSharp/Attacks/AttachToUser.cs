
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AttachToUser : UdonSharpBehaviour
{
	[SerializeField] private bool attachPosition;
	[SerializeField] private bool attachRotation;
	[SerializeField] private bool attachScale;
	[SerializeField] private AttachTo attachTo;

	private Vector3 scale;

	private VRCPlayerApi player;
	void Start()
	{
		player = Networking.LocalPlayer;
		scale = transform.localScale;
	}

	void Update()
	{
		if (player == null) return;

		if (attachPosition)
		{
			switch (attachTo)
			{
				case AttachTo.Head:
					transform.position = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
					break;
				case AttachTo.LeftHand:
					transform.position = player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
					break;
				case AttachTo.RightHand:
					transform.position = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
					break;
			}
		}

		if (attachRotation)
		{
			switch (attachTo)
			{
				case AttachTo.Head:
					transform.rotation = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
					break;
				case AttachTo.LeftHand:
					transform.rotation = player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).rotation;
					break;
				case AttachTo.RightHand:
					transform.rotation = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).rotation;
					break;
			}
		}

		if (attachScale)
		{
			transform.localScale = player.GetAvatarEyeHeightAsMeters() * scale;
		}
	}
}

[Serializable]
public enum AttachTo
{
	Head,
	LeftHand,
	RightHand
}