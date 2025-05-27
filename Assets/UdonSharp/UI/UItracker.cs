using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon.Common;

public class UItracker : UdonSharpBehaviour
{
	[SerializeField] private GameObject ghost;
	[SerializeField] private GameObject eye;
	[SerializeField] private TextMeshProUGUI debugText;
	[SerializeField] private GameObject[] trackedObjects;
	[SerializeField] private GameObject[] coursor;
	[SerializeField] private AutoToggle inventoryOpened;
	[SerializeField] private AutoToggle settingsOpened;
	[SerializeField] private AutoToggle handUI;
	[SerializeField] private Inventory inventory;

	private CellController selectedCell = null;
	[SerializeField] private CellController dragedCell;
	private bool startDrag = false;



	private VRCPlayerApi player;
	private float stateAnimator;
	private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
	private Image ghostImage;
	private Vector2 coursorPos;
	private Vector2 coursorAnimPos;
	private bool coursorActive = false;
	private int index;


	private void Start()
	{
		ghostImage = ghost.GetComponent<Image>();
		player = Networking.LocalPlayer;
	}

	private void Update()
	{
		var ghostTracked = false;
		if (!handUI.State)
		{
			ghost.SetActive(true);

			var headPos = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
			var pos = headPos + (ghost.transform.position - headPos).normalized;

			var dist = Vector3.Distance(pos, eye.transform.position);
			if (dist < 0.2)
			{
				ghost.SetActive(true);
				ghostImage.color = new Color(1, 1, 1, Mathf.Min(1, 2f - dist * 10));
				if (dist < 0.15)
					ghostTracked = true;
			}
			else
			{
				ghost.SetActive(false);
			}
		}
		else if (!inventoryOpened.State && !settingsOpened.State && stateAnimator > 0)
		{
			ghostImage.color = new Color(1, 1, 1, 1 - stateAnimator);
		}
		else
		{
			ghost.SetActive(false);
		}

		if (!inventoryOpened.State && !settingsOpened.State && (handUI.State || ghostTracked))
		{
			if (
				transform.localPosition.z > -120 &&
				transform.localPosition.z < 0 &&
				transform.localPosition.x < 130 &&
				transform.localPosition.x > -130 &&
				transform.localPosition.y < -49 &&
				transform.localPosition.y > -85)
			{
				handUI.State = true;
				coursorActive = true;
			}
			else
			{
				handUI.State = false;
				coursorActive = false;
			}
		}

		if (inventoryOpened.State)
		{
			if (
				transform.localPosition.z > -120 &&
				transform.localPosition.z < 0 &&
					(
						transform.localPosition.x < 130 &&
						transform.localPosition.x > -130 &&
						transform.localPosition.y < -49 &&
						transform.localPosition.y > -85 ||

						transform.localPosition.x < 100 &&
						transform.localPosition.x > -100 &&
						transform.localPosition.y < 95 &&
						transform.localPosition.y >= -49
					)
				)
			{
				coursorActive = true;
			}
			else
			{
				coursorActive = false;
			}
		}

		if (settingsOpened.State)
		{
			if (
				transform.localPosition.z > -120 &&
				transform.localPosition.z < 0 &&
					(
						transform.localPosition.x < 100 &&
						transform.localPosition.x > -130 &&
						transform.localPosition.y < -49 &&
						transform.localPosition.y > -85 ||

						transform.localPosition.x < 100 &&
						transform.localPosition.x > -100 &&
						transform.localPosition.y < 95 &&
						transform.localPosition.y >= -49
					)
				)
			{
				coursorActive = true;
			}
			else
			{
				coursorActive = false;
			}
		}


		index = -1;
		if (coursorActive)
		{
			if (stateAnimator < 1)
			{
				stateAnimator += Time.deltaTime * 3;
				if (stateAnimator > 1)
				{
					stateAnimator = 1;
				}
			}
			float dist = 1000;
			for (int i = 0; i < trackedObjects.Length; i++)
			{
				var obj = trackedObjects[i];
				if (obj.activeInHierarchy == false) continue;

				var newDist = Vector2.SqrMagnitude(obj.transform.localPosition - transform.localPosition);
				if (newDist > 225) continue;

				if (newDist < dist)
				{
					dist = newDist;
					index = i;
				}
			}

			if (index == -1)
			{
				coursorPos = transform.localPosition;
			}
			else
			{
				coursorPos = trackedObjects[index].transform.localPosition;
			}

			coursorAnimPos = (coursorAnimPos + coursorPos * 7) / 8;
		}
		else if (stateAnimator > 0)
		{
			stateAnimator -= Time.deltaTime * 3;
			if (stateAnimator < 0)
			{
				stateAnimator = 0;
			}
		}

		foreach (var obj in coursor)
		{
			obj.GetComponent<Image>().color = new Color(1, 1, 1, curve.Evaluate(stateAnimator));
		}
		var center = (coursorAnimPos + (Vector2)transform.localPosition) / 2;
		if (index != -1 && (trackedObjects[index].GetComponent<AutoToggle>() != null) || settingsOpened.State)
		{
			var offsetMultiplier = 9 - stateAnimator * 10;
			coursor[0].transform.localPosition = center + new Vector2(-9, 9) * offsetMultiplier;
			coursor[1].transform.localPosition = center + new Vector2(9, 9) * offsetMultiplier;
			coursor[2].transform.localPosition = center + new Vector2(-9, -9) * offsetMultiplier;
			coursor[3].transform.localPosition = center + new Vector2(9, -9) * offsetMultiplier;
		}
		else
		{
			var offsetMultiplier = 10 - stateAnimator * 9;
			coursor[0].transform.localPosition = center + new Vector2(-9, 9) * offsetMultiplier;
			coursor[1].transform.localPosition = center + new Vector2(9, 9) * offsetMultiplier;
			coursor[2].transform.localPosition = center + new Vector2(-9, -9) * offsetMultiplier;
			coursor[3].transform.localPosition = center + new Vector2(9, -9) * offsetMultiplier;
		}

		if (coursorActive)
		{
			if (selectedCell != null)
				dragedCell.transform.localPosition = (Vector2)transform.localPosition;
		}
		else
		{
			if (selectedCell != null)
			{
				dragedCell.Shuffle(selectedCell);
				selectedCell = null;
			}
		}
	}

	public override void InputUse(bool value, UdonInputEventArgs args)
	{
		if (args.handType == HandType.LEFT) return;
		if (!value) return;
		if (index == -1) return;

		var index2 = index - trackedObjects.Length + 9;

		if (index2 >= 0)
		{
			inventory.ChangeUsed(index2);
			return;
		}

		var cell = trackedObjects[index].GetComponent<CellController>();

		if (cell != null)
		{
			if (selectedCell == null)
				cell._Click();
			else
				cell.TryOne(dragedCell);
			return;
		}

		if (selectedCell != null) return;

		var toggle = trackedObjects[index].GetComponent<AutoToggle>();

		if (toggle != null)
		{
			toggle.Toggle();
			player.Immobilize(inventoryOpened.State || settingsOpened.State);
			return;
		}
	}

	public override void InputGrab(bool value, UdonInputEventArgs args)
	{
		if (args.handType == HandType.LEFT) return;
		if (index == -1) return;
		if (value)
		{
			var cell = trackedObjects[index].GetComponent<CellController>();
			if (cell == null) return;
			inventory.Deselect();
			selectedCell = cell;
			cell.Shuffle(dragedCell);
		}
		else if (selectedCell != null)
		{
			dragedCell.Shuffle(selectedCell);
			var cell = trackedObjects[index].GetComponent<CellController>();
			if (cell != null) selectedCell.Shuffle(cell);
			selectedCell = null;
		}
	}

	[UdonSynced] private int syncedValue;

	public void ApplyChange()
	{
		syncedValue++;
		debugText.text = syncedValue.ToString();
		RequestSerialization();
	}

	public override void OnDeserialization()
	{
		debugText.text = syncedValue.ToString();
	}
}
