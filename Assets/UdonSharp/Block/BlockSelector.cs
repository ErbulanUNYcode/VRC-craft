using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
using Random = UnityEngine.Random;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class BlockSelector : UdonSharpBehaviour
{
	[SerializeField] private Transform orderTransform;
	[SerializeField] private WorldController worldController;
	//[SerializeField] private NetworkManager networkManager;
	[SerializeField] private Inventory inventory;
	[SerializeField] private GameObject selectedCube;
	[SerializeField] private GameObject UIplus;
	[SerializeField] private GameObject plus;
	[SerializeField] private GameObject plusX;
	[SerializeField] private GameObject plusY;
	[SerializeField] private GameObject plusZ;
	[SerializeField] private AutoToggle handUI;
	VRCPlayerApi localPlayer;
	public Vector3Int selectedBlock;
	public Vector3Int selectedAir;
	public Vector3 point;

	private void Start()
	{
		localPlayer = Networking.LocalPlayer;
	}

	private void Update()
	{
		if (UIplus != null) UIplus.SetActive(!Input.GetKey(KeyCode.Tab));

		if (Input.GetKey(KeyCode.Tab) || (handUI != null && handUI.isOn) || worldController.InBlock(Vector3Int.FloorToInt(transform.position)))
		{
			selectedCube.SetActive(false);
			if (plus != null) plus.SetActive(false);
			selectedBlock = Vector3Int.down;
			selectedAir = Vector3Int.down;
			return;
		}

		Vector3 origin = transform.position;
		Vector3 direction = (orderTransform.position - origin).normalized;

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
			if (worldController.InBlock(current))
			{
				//find the b
				selectedBlock = current;
				selectedCube.SetActive(true);
				selectedCube.transform.position = selectedBlock + Vector3.one * 0.5f;
				if (handUI != null)
				{
					plus.SetActive(true);
					plus.transform.position = point;
					plusX.SetActive(selectedBlock.x == selectedAir.x);
					plusY.SetActive(selectedBlock.y == selectedAir.y);
					plusZ.SetActive(selectedBlock.z == selectedAir.z);
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
		if (plus != null) plus.SetActive(false);

		if (selectedAir == Vector3Int.down || worldController.InBlock(Vector3Int.FloorToInt(transform.position))) return;
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

	public override void InputUse(bool value, UdonInputEventArgs args)
	{
		if (!localPlayer.IsUserInVR())
		{
			base.InputUse(value, args);
			return;
		}

		if (!value || selectedAir == Vector3Int.down || args.handType == HandType.LEFT || handUI.isOn || worldController.InBlock(Vector3Int.FloorToInt(transform.position)))
		{
			base.InputUse(value, args);
			return;
		}

		Random.InitState(DateTime.Now.Millisecond);
		Click(true);
		base.InputUse(value, args);
	}
	public override void InputGrab(bool value, UdonInputEventArgs args)
	{
		if (localPlayer.IsUserInVR() && (!value || selectedAir == Vector3Int.down || args.handType == HandType.LEFT || (handUI != null && handUI.isOn) || worldController.InBlock(Vector3Int.FloorToInt(transform.position))))// return;
		{
			base.InputGrab(value, args);
			return;
		}
		if (!localPlayer.IsUserInVR() && (!value || selectedAir == Vector3Int.down || worldController.InBlock(Vector3Int.FloorToInt(transform.position))))// return;
		{
			base.InputGrab(value, args);
			return;
		}

		Click(false);
		base.InputGrab(value, args);
	}

	public override void InputDrop(bool value, UdonInputEventArgs args)
	{
		if (localPlayer.IsUserInVR())// return;
		{
			base.InputDrop(value, args);
			return;
		}

		if (!value || selectedAir == Vector3Int.down || worldController.InBlock(Vector3Int.FloorToInt(transform.position)))// return;
		{
			base.InputDrop(value, args);
			return;
		}

		Random.InitState(DateTime.Now.Millisecond);
		Click(true);
		base.InputDrop(value, args);
	}

	private void Click(bool set)
	{
		inventory.ClickBlock(selectedBlock, selectedAir, set);
	}
}
