
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

public class Alpinism : UdonSharpBehaviour
{
	[SerializeField] private Transform[] points;
	[SerializeField] private float distance = 0.1f;

	VRCPlayerApi player;
	private int currentPointIndex = 0;
	private bool isClimbing = false;
	private HandType hand;
	private Vector3 lastPosition;

	private Vector3 oldPos;
	private Vector3 newPos;

	private void Start()
	{
		player = Networking.LocalPlayer;
	}

	private void Update()
	{
		oldPos = newPos;
		newPos = player.GetPosition();

		if (!isClimbing) return;

		player.SetVelocity(Vector3.zero);
		var offset = player.GetTrackingData(hand == HandType.LEFT ? VRCPlayerApi.TrackingDataType.LeftHand : VRCPlayerApi.TrackingDataType.RightHand).position - lastPosition;
		Quaternion headRotation = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
		Vector3 euler = headRotation.eulerAngles;
		Quaternion onlyYawRotation = Quaternion.Euler(0, euler.y, 0);
		player.TeleportTo(player.GetPosition() - offset, onlyYawRotation);
	}

	public override void InputGrab(bool value, UdonInputEventArgs args)
	{
		if (!value)
		{
			if (args.handType == hand)
			{
				isClimbing = false;
				player.Immobilize(false);

				var posOffset = newPos - oldPos;

				player.SetVelocity(posOffset / Time.deltaTime);
			}
		}
		else
		{
			var sqrDistance = distance * distance;
			Vector3 pos;
			if (args.handType == HandType.LEFT)
			{
				pos = player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
			}
			else if (args.handType == HandType.RIGHT)
			{
				pos = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
			}
			else
			{
				return;
			}
			for (int i = 0; i < points.Length; i++)
			{
				if (Vector3.SqrMagnitude(pos - points[i].position) < sqrDistance)
				{
					if (!isClimbing)
					{
						player.Immobilize(true);
					}
					currentPointIndex = i;
					isClimbing = true;
					hand = args.handType;
					lastPosition = pos;
					break;
				}
			}
		}
	}
}
