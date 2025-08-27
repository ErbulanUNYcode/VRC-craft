using UdonSharp;
using UnityEngine;

public class NetworkManager : UdonSharpBehaviour
{
	private ClientNetworkController myNetworkController;

	public void SetMyController(ClientNetworkController controller)
	{
		myNetworkController = controller;
	}

	public bool SetBlock(Vector3Int pos, int block)
	{
		return myNetworkController.SetBlock(pos, block);
	}
}
