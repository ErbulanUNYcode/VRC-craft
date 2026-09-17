using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VRC_MINE.GamePlay
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class BlockSelector : UdonSharpBehaviour
	{
		[SerializeField] private Transform[] VRPlus;
		[SerializeField] private GameObject PCPlus;
		[SerializeField] private GameObject selectedCube;
		private Texture2D worldTexture;
		private VRCPlayerApi localPlayer;
		private Vector3Int selectedBlock;
		private Vector3Int selectedAir;
		private Vector3 point;
		public void SetWorldTexture(Texture2D world)
		{
			localPlayer = Networking.LocalPlayer;
			worldTexture = world;
		}

		private void Update()
		{
			if (worldTexture == null) return;

			var treckingData = localPlayer.GetTrackingData(localPlayer.IsUserInVR() ? VRCPlayerApi.TrackingDataType.RightHand : VRCPlayerApi.TrackingDataType.Head);

			Vector3 origin = treckingData.position;
			Vector3 direction = treckingData.rotation * new Vector3(0.707106781f, 0f, 0.707106781f);

			Vector3Int current = Vector3Int.FloorToInt(origin);
			Vector3Int step = new Vector3Int(
				direction.x > 0 ? 1 : -1,
				direction.y > 0 ? 1 : -1,
				direction.z > 0 ? 1 : -1
			);

			Vector3 tMax = new Vector3(
				IntBound(origin.x, direction.x),
				IntBound(origin.y, direction.y),
				IntBound(origin.z, direction.z)
			);

			Vector3 tDelta = new Vector3(
				Mathf.Abs(1f / direction.x),
				Mathf.Abs(1f / direction.y),
				Mathf.Abs(1f / direction.z)
			);

			float distTraveled = 0f;
			float maxDist = 25f;
			point = origin;

			while (distTraveled <= maxDist)
			{
				if (InBlock(current))
				{
					//find the b
					selectedBlock = current;
					selectedCube.SetActive(true);
					selectedCube.transform.position = selectedBlock + Vector3.one * 0.5f;
					if (localPlayer.IsUserInVR())
					{
						foreach (var p in VRPlus) p.position = point;

						VRPlus[0].gameObject.SetActive(selectedBlock.x == selectedAir.x);
						VRPlus[1].gameObject.SetActive(selectedBlock.y == selectedAir.y);
						VRPlus[2].gameObject.SetActive(selectedBlock.z == selectedAir.z);
					}
					return;
				}

				selectedAir = current;

				if (tMax.x < tMax.y && tMax.x < tMax.z)
				{
					distTraveled = tMax.x;
					tMax.x += tDelta.x;
					current.x += step.x;
				}
				else if (tMax.y < tMax.z)
				{
					distTraveled = tMax.y;
					tMax.y += tDelta.y;
					current.y += step.y;
				}
				else
				{
					distTraveled = tMax.z;
					tMax.z += tDelta.z;
					current.z += step.z;
				}

				point = origin + direction * distTraveled;
				distTraveled = Vector3.SqrMagnitude(point - origin);
			}

			selectedBlock = Vector3Int.down;
			selectedAir = Vector3Int.down;
			selectedCube.SetActive(false);

			foreach (var p in VRPlus) p.gameObject.SetActive(false);

			if (selectedAir == Vector3Int.down || InBlock(Vector3Int.FloorToInt(transform.position))) return;
		}



		private float IntBound(float s, float ds)
		{
			if (ds == 0) return float.MaxValue;
			else
			{
				float sOffset = ds > 0 ? Mathf.Ceil(s) - s : s - Mathf.Floor(s);
				return sOffset / Mathf.Abs(ds);
			}
		}

		private bool InBlock(Vector3Int pos)
		{
			if (pos.y > 127 || pos.y < 0) return false;
			var ch = new Vector2Int(((pos.x >> 4) & 31) << 8, ((pos.z >> 4) & 31) << 7);
			var res = ((Color32)worldTexture.GetPixel(ch.x + (pos.x & 15) + ((pos.z & 15) << 4), ch.y + pos.y)).r;

			return res != 0;
		}
	}
}