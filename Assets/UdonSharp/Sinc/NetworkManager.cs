using UdonSharp;
using UnityEngine;

public class NetworkManager : UdonSharpBehaviour
{
	private ClientNetworkController myNetworkController;

	public void SetMyController(ClientNetworkController controller)
	{
		myNetworkController = controller;
	}

	public void SetBlock(Vector3Int pos, int block)
	{
		myNetworkController.SetBlock(pos, block);
	}
}
