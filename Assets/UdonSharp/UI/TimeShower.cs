
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class TimeShower : UdonSharpBehaviour
{
	private VRCPlayerApi player;
	[SerializeField] private TextMeshProUGUI text;
	[SerializeField] private TextMeshProUGUI testLog;

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
		Vector3 headPosition = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
		testLog.text = System.Math.Round(headPosition.x) + ", " + System.Math.Round(headPosition.y) + ", " + System.Math.Round(headPosition.z);

		if (System.DateTime.Now.Second % 2 == 0)
			text.text = System.DateTime.Now.ToString("yyyy.MM.dd") + "\n" + System.DateTime.Now.ToString("HH:mm");
		else
			text.text = System.DateTime.Now.ToString("yyyy.MM.dd") + "\n" + System.DateTime.Now.ToString("HH mm");
	}
}
