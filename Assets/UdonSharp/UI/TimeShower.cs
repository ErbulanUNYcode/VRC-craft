using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TimeShower : UdonSharpBehaviour
{
	private VRCPlayerApi player;
	[SerializeField] private TextMeshProUGUI text;
	[SerializeField] private TextMeshProUGUI testLog;
	[SerializeField] private WorldController worldController;

	void Start()
	{
		player = Networking.LocalPlayer;
		player.SetGravityStrength(1.5f);
		VRCPlayerApi owner = Networking.GetOwner(gameObject);
	}

	void Update()
	{
		//log.GetComponent<Canvas>().additionalShaderChannels = 0;

		//coordinate.text = log.GetComponent<Camera>().targetTexture == null ? "No" : "Yes";
		//coordinate.text = player position to round
		if (Networking.LocalPlayer.displayName == "TonyEric" || Networking.LocalPlayer.displayName == "vrcdevs-idiots" || Networking.LocalPlayer.displayName == "[1] Local Player" || Networking.LocalPlayer.displayName == "_Dozer")
		{
			testLog.text = worldController.LogData;

			text.text = worldController.LogData2;
		}
		else
		{
			Vector3 headPosition = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
			testLog.text = System.Math.Floor(headPosition.x) + ", " + System.Math.Floor(headPosition.y) + ", " + System.Math.Floor(headPosition.z);

			if (System.DateTime.Now.Second % 2 == 0)
				text.text = System.DateTime.Now.ToString("yyyy.MM.dd") + "\n" + System.DateTime.Now.ToString("HH:mm");
			else
				text.text = System.DateTime.Now.ToString("yyyy.MM.dd") + "\n" + System.DateTime.Now.ToString("HH mm");
		}

		LayoutRebuilder.ForceRebuildLayoutImmediate(text.transform.parent.GetComponent<RectTransform>());
		LayoutRebuilder.ForceRebuildLayoutImmediate(testLog.transform.parent.GetComponent<RectTransform>());
		LayoutRebuilder.ForceRebuildLayoutImmediate(testLog.transform.parent.parent.GetComponent<RectTransform>());
	}
}
