
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TeleportTo : UdonSharpBehaviour
{
	private VRCPlayerApi currentPlayer;
	[SerializeField] TextMeshProUGUI text;

	public void Teleport()
	{
		Debug.Log("Teleport");
		if (currentPlayer == null) return;
		Networking.LocalPlayer.TeleportTo(currentPlayer.GetPosition(), currentPlayer.GetRotation());
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
}
