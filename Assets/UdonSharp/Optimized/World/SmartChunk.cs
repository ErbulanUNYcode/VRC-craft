
using UdonSharp;
using UnityEngine;

namespace VRC_MINE.Data
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class SmartChunk : UdonSharpBehaviour
	{
		[System.NonSerialized] public byte[] blockData;
		[System.NonSerialized] public Vector2Int chunkPosition;
		[System.NonSerialized] public bool hasData;
	}
}
