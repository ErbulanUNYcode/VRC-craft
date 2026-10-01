using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC_MINE.World;

namespace VRC_MINE.Net
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
	public class NetClient : UdonSharpBehaviour
	{
		[SerializeField] private WorldGenerator worldGenerator;
		[SerializeField] private NetManager netManager;
		[UdonSynced] private int[] setBlockPositionsX;
		[UdonSynced] private byte[] setBlockPositionsY;
		[UdonSynced] private int[] setBlockPositionsZ;
		[UdonSynced] private byte[] setBlockTypes;
		[UdonSynced] private byte[] oldBlockTypes;

		public void SetBlocks(int[] setX, byte[] setY, int[] setZ, byte[] setType, byte[] setOld, int count)
		{
			if (count == 0)
			{
				setBlockPositionsX = new int[0];
				setBlockPositionsY = new byte[0];
				setBlockPositionsZ = new int[0];
				setBlockTypes = new byte[0];
				oldBlockTypes = new byte[0];
				return;
			}
			setBlockPositionsX = new int[count];
			setBlockPositionsY = new byte[count];
			setBlockPositionsZ = new int[count];
			setBlockTypes = new byte[count];
			oldBlockTypes = new byte[count];
			Array.Copy(setX, setBlockPositionsX, count);
			Array.Copy(setY, setBlockPositionsY, count);
			Array.Copy(setZ, setBlockPositionsZ, count);
			Array.Copy(setType, setBlockTypes, count);
			Array.Copy(setOld, oldBlockTypes, count);
		}

		void Start()
		{
			if (Networking.IsOwner(gameObject))
			{
				netManager.SetOwnClient(this);
			}
		}

		public override void OnDeserialization()
		{
			for (int i = 0; i < setBlockTypes.Length; i++)
			{
				worldGenerator.SetBlock(new Vector3Int(setBlockPositionsX[i], setBlockPositionsY[i], setBlockPositionsZ[i]), setBlockTypes[i], oldBlockTypes[i]);
			}

		}
	}
}