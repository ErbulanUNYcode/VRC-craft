using UdonSharp;
using UnityEngine;

public class NetworkManager : UdonSharpBehaviour
{
	private ClientNetworkController myNetworkController;
	[SerializeField] private WorldData worldData;

	public void SetMyController(ClientNetworkController controller)
	{
		myNetworkController = controller;
	}

	public bool SetBlock(Vector3Int pos, int block)
	{
		return myNetworkController.SetBlock(pos, block);
	}

	public void GiveKitStart(string playerName)
	{
		myNetworkController.GiveStartData(playerName, worldData.GetWorldData(), worldData.GetPositions());
	}
}
