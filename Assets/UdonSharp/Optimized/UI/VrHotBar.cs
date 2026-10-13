
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VRC_MINE.UI
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class VrHotBar : UdonSharpBehaviour
	{
		private RectTransform root;
		private VRCPlayerApi localPlayer;
		private int selectedIndex = 0;

		private void Start()
		{
			localPlayer = Networking.LocalPlayer;
			root = GetComponent<RectTransform>();
		}

		private void Update()
		{
			var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
			var rightHand = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
			var delay = rightHand.position - head.position;
			var dir = rightHand.rotation * new Vector3(0.707106781f, 0f, 0.707106781f);
			dir.y = 0;
			root.transform.rotation = Quaternion.LookRotation(dir);
			root.transform.position = delay + Vector3.up * 0.06f;
		}
	}
}
