using System;
using UdonSharp;
using UnityEngine;

public class NetworkManager : UdonSharpBehaviour
{
	private ClientNetworkController myNetworkController;
	private string[] players = new string[1];
	private int playerCount = 0;
	[SerializeField] private WorldData worldData;

	public void SetMyController(ClientNetworkController controller)
	{
		myNetworkController = controller;
	}

	public void SetBlock(Vector3Int pos, int to, int from)
	{
		myNetworkController.SetBlockSignal(pos, to, from);
	}

	public void SendStartData(string playerName)
	{
		myNetworkController.SendStartData(playerName, worldData.GetWorldData(), worldData.GetPositions());
	}

	public void PublicOwner(string displayName, Vector3Int pos, int to, int from)
	{
		myNetworkController.PublicOwnerSignal(displayName, pos, to, from);
	}

	public void WantOwner(Vector3Int pos, int to, int from)
	{
		myNetworkController.WantOwnerSignal(pos, to, from);
	}

	public void PublicOwner(int x, int z, string playerName)
	{
		myNetworkController.PublicOwnerSignal(x, z, playerName);
	}

	public void FixBack(Vector3Int pos, int oldTo, int fix, string playerName)
	{
		myNetworkController.FixBackSignal(pos, oldTo, fix, playerName);
	}

	public void FixBack(Vector3Int[] fixPositions, int[] fixToBlocks, int[] fixFromBlocks, string[] fixOwners)
	{
		myNetworkController.FixBackSignal(fixPositions, fixToBlocks, fixFromBlocks, fixOwners);
	}
	public void GlobalRequest(Vector2Int[] wantChunks, Vector2Int[] requestedChunks, string[] requestedOwners, string[] discardedData = null, Vector2Int[] discardedChunks = null)
	{
		myNetworkController.GlobalRequestSignal(wantChunks, requestedChunks, requestedOwners, discardedChunks, discardedData);
	}

	public void AnswerGlobalRequest(string[] requestedData, Vector2Int[] requestedPositions, string playerName)
	{
		myNetworkController.AnswerGlobalRequestSignal(requestedData, requestedPositions, playerName);
	}

	public void AddPlayer(string playerName)
	{
		if (players.Length <= playerCount)
		{
			var newPlayers = new string[players.Length * 2];
			Array.Copy(players, newPlayers, players.Length);
			players = newPlayers;
		}

		players[playerCount] = playerName;
		playerCount++;
	}

	public void RemovePlayer(string playerName)
	{
		for (int i = 0; i < playerCount; i++)
		{
			if (players[i] == playerName)
			{
				players[i] = players[playerCount - 1];
				players[playerCount - 1] = null;
				playerCount--;
				return;
			}
		}
	}
}
