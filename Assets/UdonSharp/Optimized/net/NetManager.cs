using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC_MINE.World;

namespace VRC_MINE.Net
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class NetManager : UdonSharpBehaviour
	{
		[SerializeField] private WorldGenerator worldGenerator;
		private NetClient ownClient;
		private VRCPlayerApi localPlayer;
		private int setBlockCount = 0;
		private int[] setBlockPositionsX = new int[16];//x,y,z
		private byte[] setBlockPositionsY = new byte[16];//x,y,z
		private int[] setBlockPositionsZ = new int[16];//x,y,z
		private byte[] setBlockTypes = new byte[16];
		private byte[] oldBlockTypes = new byte[16];

		public void SetOwnClient(NetClient netClient)
		{
			ownClient = netClient;
		}

		public void SetBlock(Vector3Int pos, byte blockType)
		{
			worldGenerator.SetBlock(pos, blockType, 0);
			setBlockPositionsX[setBlockCount] = pos.x;
			setBlockPositionsY[setBlockCount] = (byte)pos.y;
			setBlockPositionsZ[setBlockCount] = pos.z;
			setBlockTypes[setBlockCount] = blockType;
			setBlockCount++;
		}

		private DateTime timer;
		private void Start()
		{
			timer = DateTime.Now + TimeSpan.FromSeconds(0.1);
			localPlayer = Networking.LocalPlayer;
		}
		private void Update()
		{
			if (timer > DateTime.Now) return;
			timer = DateTime.Now + TimeSpan.FromSeconds(0.1);
			ownClient.SetBlocks(setBlockPositionsX, setBlockPositionsY, setBlockPositionsZ, setBlockTypes, oldBlockTypes, setBlockCount);
			Networking.SetOwner(localPlayer, ownClient.gameObject);
			ownClient.RequestSerialization();
			setBlockCount = 0;
		}
	}
}
