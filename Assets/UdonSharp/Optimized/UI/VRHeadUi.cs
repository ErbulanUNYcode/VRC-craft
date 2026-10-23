using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Rendering;
using VRC.SDKBase;

namespace VRC_MINE.UI
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class VRHeadUi : UdonSharpBehaviour
	{
		[SerializeField] private RectTransform HUD;
		[SerializeField] private RectTransform invRoot;
		[SerializeField] private RectTransform inv;
		[SerializeField] private Material[] UiMaterials;
		[SerializeField] private RectTransform renderImage;
		[SerializeField] private TextMeshProUGUI playerPosition;
		[SerializeField] private RectTransform cursor;
		[SerializeField] private Image[] cursorImages;

		[SerializeField] private RectTransform invZone;
		[SerializeField] private RectTransform hotbarZone;
		[SerializeField] private RectTransform otherZone;
		[SerializeField] private InvCell[] invCells;
		[SerializeField] private InvCell[] hotbarCells;
		[SerializeField] private RectTransform[] otherCells;

		private InvCell lastCell;
		private VRCPlayerApi localPlayer;
		private float eyeDist;
#if UNITY_EDITOR
		[SerializeField] private bool testVR = false;
#endif
		private void Start()
		{
			localPlayer = Networking.LocalPlayer;

			if (!localPlayer.IsUserInVR())
			{
				Destroy(gameObject);
				return;
			}
			eyeDist = Vector2.Distance(VRCCameraSettings.GetEyePosition(Camera.StereoscopicEye.Left), VRCCameraSettings.GetEyePosition(Camera.StereoscopicEye.Right)) / 2 / localPlayer.GetAvatarEyeHeightAsMeters();
			foreach (var coursorImage in cursorImages) coursorImage.color = Color.clear;
		}

		private void Update()
		{
			var head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

			HUD.rotation = Quaternion.Slerp(HUD.rotation, head.rotation, 0.1f);

			renderImage.position = head.position + head.rotation * Vector3.forward * 0.1f;

			playerPosition.text = Mathf.Round(head.position.x) + "," + Mathf.Round(head.position.y) + "," + Mathf.Round(head.position.z);

			var offset = VRCCameraSettings.ScreenCamera.Right * eyeDist * localPlayer.GetAvatarEyeHeightAsMeters();
			foreach (var UiMaterial in UiMaterials) UiMaterial.SetVector("_EyeOffset", offset);

			if (invRoot.localScale == Vector3.zero) return;

			var leftHand = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);

			invRoot.rotation = leftHand.rotation;
			invRoot.position = leftHand.position - head.position;
			var rightHand = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
			var localHand = inv.InverseTransformPoint(rightHand.position - head.position);

			cursorPosition = inv.InverseTransformPoint(rightHand.position + rightHand.rotation * new Vector3(0.1f, 0, 0.1f) - head.position);
			cursorPosition.z = 0;
			if (localHand.z > 0 || localHand.z < -200 || !inv.rect.Contains(cursorPosition))
			{
				cursorActive = false;
				if (lastCell != null) lastCell.OnExit();
				lastCell = null;
				return;
			}

			cursorActive = true;

			if (invZone.rect.Contains(cursorPosition - invZone.localPosition))
			{
				var pos = Vector3Int.FloorToInt(cursorPosition - invZone.localPosition + Vector3.right * invZone.rect.width / 2 + Vector3.up * invZone.rect.height / 2) / 18;
				var current = invCells[pos.x + pos.y * 9];
				if (lastCell != current)
				{
					if (lastCell != null) lastCell.OnExit();
					current.OnEnter();
					lastCell = current;
				}
				return;
			}
			else if (hotbarZone.rect.Contains(cursorPosition - hotbarZone.localPosition))
			{
				var pos = Vector3Int.FloorToInt(cursorPosition - hotbarZone.localPosition + Vector3.right * hotbarZone.rect.width / 2 + Vector3.up * hotbarZone.rect.height / 2) / 18;
				var current = hotbarCells[pos.x];
				if (lastCell != current)
				{
					if (lastCell != null) lastCell.OnExit();
					current.OnEnter();
					lastCell = current;
				}
				return;
			}
			else if (otherZone.rect.Contains(cursorPosition - otherZone.localPosition))
			{
				foreach (var cell in otherCells)
				{
					if (cell.rect.Contains(cursorPosition - cell.localPosition))
					{
						var current = cell.GetComponent<InvCell>();
						if (current != lastCell)
						{
							if (lastCell != null) lastCell.OnExit();
							current.OnEnter();
							lastCell = current;
						}
						return;
					}
				}
			}
			if (lastCell != null)
			{
				lastCell.OnExit();
				lastCell = null;
			}
			if (lastCell != null) lastCell.OnExit();
		}

		private float cursorState = 0;
		private bool cursorActive = false;
		private Vector3 cursorPosition;
		private void LateUpdate()
		{
			if (cursorActive)
			{
				if (cursorState < 1)
				{
					cursorState = Mathf.Min(1, cursorState + Time.deltaTime * 3);
					for (int i = 0; i < 4; i++)
					{
						cursorImages[i].color = Color.Lerp(Color.clear, Color.white, cursorState);
						cursorImages[i].transform.localPosition = cursorAnimOffsets[i] * (5 / (cursorState + 0.1f));
					}
				}
			}
			else
			{
				if (cursorState > 0)
				{
					cursorState = Mathf.Max(0, cursorState - Time.deltaTime * 3);
					for (int i = 0; i < 4; i++)
					{
						cursorImages[i].color = Color.Lerp(Color.clear, Color.white, cursorState);
						cursorImages[i].transform.localPosition = cursorAnimOffsets[i] * (5 / (cursorState + 0.1f));
					}
				}
			}


			cursor.localPosition = Vector3.Lerp(cursor.localPosition, lastCell == null ? cursorPosition : (cursorPosition + lastCell.transform.localPosition) / 2, Time.deltaTime * 20);
		}

		private Vector3[] cursorAnimOffsets =
		{
			new Vector3(-1, 1, 0),
			new Vector3( 1, 1, 0),
			new Vector3(-1,-1, 0),
			new Vector3( 1,-1, 0)
		};
	}
}