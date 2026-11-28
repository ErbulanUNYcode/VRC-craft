
using System;
using UdonSharp;
using UnityEngine;

namespace VRC_MINE.Data
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class SmartChunk : UdonSharpBehaviour
	{
		[NonSerialized] public byte[] blockData;
		[NonSerialized] public Vector2Int chunkPosition;
		[NonSerialized] public bool hasData;

		public void SetBlock(Vector3Int pos, byte block)
		{
			blockData[(pos.x & 15) + ((pos.z & 15) << 4) + (pos.y << 8)] = block;
		}
	}
}
