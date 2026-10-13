
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
using VRC_MINE.World;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TeleportTo : UdonSharpBehaviour
{
	private VRCPlayerApi currentPlayer;
	[SerializeField] private TextMeshProUGUI text;
	[SerializeField] private WorldGenerator world;

	[SerializeField] private Transform coursor;
	[SerializeField] private Transform map;
	[SerializeField] private float tpScale;
	public void Teleport()
	{
		if (currentPlayer == null) return;
		Debug.Log("Teleport to: " + currentPlayer.displayName);
		Networking.LocalPlayer.TeleportTo(currentPlayer.GetPosition() + Vector3.up * 0.5f, currentPlayer.GetRotation());
		world.OnPlayerTeleport();
	}

	public void NextPlayer()
	{
		Debug.Log("NextPlayer");
		var players = VRCPlayerApi.GetPlayers();

		if (currentPlayer == null)
		{
			if (players.Length == 1) return;

			for (int i = 0; i < 2; i++)
			{
				if (players[i].isLocal) continue;
				currentPlayer = players[i];
				text.text = currentPlayer.displayName;
				break;
			}
		}

		if (players.Length == 2) return;

		var index = 0;
		for (int i = 0; i < players.Length; i++)
		{
			if (players[i].isLocal) continue;
			if (players[i] != currentPlayer) continue;
			index = i;
			break;
		}

		index++;
		if (index >= players.Length) index = 0;
		currentPlayer = players[index];
		text.text = currentPlayer.displayName;
		if (!currentPlayer.isLocal) return;
		index++;
		if (index >= players.Length) index = 0;
		currentPlayer = players[index];
		text.text = currentPlayer.displayName;
	}

	public void PrevPlayer()
	{
		Debug.Log("PrevPlayer");
		var players = VRCPlayerApi.GetPlayers();

		if (currentPlayer == null)
		{
			if (players.Length == 1) return;

			for (int i = 0; i < 2; i++)
			{
				if (players[i].isLocal) continue;
				currentPlayer = players[i];
				text.text = currentPlayer.displayName;
				break;
			}
		}

		if (players.Length == 2) return;

		var index = 0;
		for (int i = 0; i < players.Length; i++)
		{
			if (players[i].isLocal) continue;
			if (players[i] != currentPlayer) continue;
			index = i;
			break;
		}

		index--;
		if (index < 0) index = players.Length - 1;
		currentPlayer = players[index];
		text.text = currentPlayer.displayName;
		if (!currentPlayer.isLocal) return;
		index--;
		if (index < 0) index = players.Length - 1;
		currentPlayer = players[index];
		text.text = currentPlayer.displayName;
	}

	public override void OnPlayerLeft(VRCPlayerApi player)
	{
		Debug.Log("OnPlayerLeft");
		if (currentPlayer == null) return;
		if (player != currentPlayer) return;
		currentPlayer = null;
		text.text = "Select a player";
	}

	private void Update()
	{
		var localPlayer = Networking.LocalPlayer;
		var trakingData = localPlayer.IsUserInVR()
			? localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand)
			: localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

		var pos = map.InverseTransformPoint(trakingData.position);
		var order = map.InverseTransformDirection(
			localPlayer.IsUserInVR()
				? trakingData.rotation * new Vector3(1, 0, 1)
				: trakingData.rotation * Vector3.forward
		);

		if (Vector3.Magnitude(pos) > 2 || pos.z > 0 || order.z < 0)
		{
			coursor.gameObject.SetActive(false);
			return;
		}

		var distance = -pos.z / order.z;
		var hit = pos + order * distance;

		if (hit.x < -0.5f || hit.x > 0.5f || hit.y < -0.5f || hit.y > 0.5f)
		{
			coursor.gameObject.SetActive(false);
			return;
		}

		coursor.gameObject.SetActive(true);
		coursor.localPosition = new Vector3(hit.x, hit.y, 0);
	}

	public override void InputUse(bool value, UdonInputEventArgs args)
	{
		if (value && coursor.gameObject.activeSelf)
		{
			Networking.LocalPlayer.TeleportTo(new Vector3(coursor.localPosition.x, 0, coursor.localPosition.y) * 40940 + Vector3.up * 80, Networking.LocalPlayer.GetRotation());
		}
	}
}
