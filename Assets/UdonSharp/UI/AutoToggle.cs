
using UdonSharp;
using UnityEngine;

public class AutoToggle : UdonSharpBehaviour
{
	[SerializeField]
	private GameObject[] objectsToToggleOn;

	[SerializeField]
	private GameObject[] objectsToToggleOff;

	[SerializeField]
	private AutoToggle[] togglsToToggleOff;

	[SerializeField]
	private ToggleType toggleType = ToggleType.OnOff;

	public bool isOn = false;

	private void OnEnable()
	{
		for (int i = 0; i < objectsToToggleOn.Length; i++)
		{
			objectsToToggleOn[i].SetActive(isOn);
		}

		for (int i = 0; i < objectsToToggleOff.Length; i++)
		{
			objectsToToggleOff[i].SetActive(!isOn);
		}
	}

	public void Toggle()
	{
		if (toggleType == ToggleType.OnOff)
		{
			State = !isOn;
		}
		else if (toggleType == ToggleType.OnOnly)
		{
			State = true;
		}
	}

	public bool State
	{
		get { return isOn; }
		set
		{
			if (isOn == value) return;

			isOn = value;
			for (int i = 0; i < objectsToToggleOn.Length; i++)
			{
				objectsToToggleOn[i].SetActive(isOn);
			}
			for (int i = 0; i < objectsToToggleOff.Length; i++)
			{
				objectsToToggleOff[i].SetActive(!isOn);
			}

			if (value)
			{
				for (int i = 0; i < togglsToToggleOff.Length; i++)
				{
					togglsToToggleOff[i].State = false;
				}
			}
		}
	}
}

public enum ToggleType
{
	OnOff,
	OnOnly
}