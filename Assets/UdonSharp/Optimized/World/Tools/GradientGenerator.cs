using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GradientGenerator : UdonSharpBehaviour
{
	[UdonSynced] private float time;
	[UdonSynced] private Color[] up;
	[UdonSynced] private Color[] down;
	[UdonSynced] private Color[] right;
	[UdonSynced] private Color[] left;
	[UdonSynced] private Color[] front;
	[UdonSynced] private Color[] fogFront;
	[UdonSynced] private Color[] fogBack;
	[UdonSynced] private int[] upT;
	[UdonSynced] private int[] downT;
	[UdonSynced] private int[] rightT;
	[UdonSynced] private int[] leftT;
	[UdonSynced] private int[] frontT;
	[UdonSynced] private int[] fogFrontT;
	[UdonSynced] private int[] fogBackT;

	[SerializeField] private TMP_InputField output;
	private void GradientToJson()
	{
		var gr = gradients[(int)selectedDirection];

		var colors = gr.colorKeys;
		var alphas = gr.alphaKeys;

		string json = "UnityEditor.GradientWrapperJSON:{\"gradient\":{";
		json += "\"serializedVersion\":\"2\",";

		for (int i = 0; i < 8; i++)
		{
			float r = 0;
			float g = 0;
			float b = 0;
			float a = 0;

			if (i < colors.Length)
			{
				r = colors[i].color.r;
				g = colors[i].color.g;
				b = colors[i].color.b;
				a = colors[i].color.a;
			}

			if (i < alphas.Length)
				a = alphas[i].alpha;

			json += "\"key" + i + "\":{";
			json += "\"r\":" + Float(r) + ",";
			json += "\"g\":" + Float(g) + ",";
			json += "\"b\":" + Float(b) + ",";
			json += "\"a\":" + Float(a);
			json += "},";
		}

		for (int i = 0; i < 8; i++)
		{
			int t = i < colors.Length
				? Mathf.RoundToInt(colors[i].time * 65535f)
				: 0;

			json += "\"ctime" + i + "\":" + t + ",";
		}

		for (int i = 0; i < 8; i++)
		{
			int t = i < alphas.Length
				? Mathf.RoundToInt(alphas[i].time * 65535f)
				: 0;

			json += "\"atime" + i + "\":" + t + ",";
		}

		json += "\"m_Mode\":" + (int)gr.mode + ",";
		json += "\"m_ColorSpace\":0,";
		json += "\"m_NumColorKeys\":" + colors.Length + ",";
		json += "\"m_NumAlphaKeys\":" + alphas.Length;
		json += "}}";

		output.text = json;
	}

	private string Float(float value)
	{
		string s = value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

		if (!s.Contains(".") && !s.Contains("E"))
			s += ".0";

		return s;
	}

	private void UpdateAllView()
	{
		for (int i = 0; i < lineRenderers.Length; i++)
		{
			UpdateView(i);
		}
	}

	private void UpdateView(int id)
	{
		var l = lineRenderers[id];
		var g = gradients[id];

		l.colorGradient = g;
		var c = g.colorKeys;
		if (c[0].time == 0)
		{
			var positions = new Vector3[c.Length];

			for (int i = 1; i < c.Length; i++)
			{
				positions[i] = Vector3.right * c[i].time * 600;
			}

			l.positionCount = positions.Length;
			l.SetPositions(positions);
		}
		else
		{
			var positions = new Vector3[c.Length + 4];
			positions[1] = Vector3.right * 149.99f;
			positions[2] = Vector3.right * 150.01f;
			positions[positions.Length - 1] = Vector3.right * 600;
			positions[positions.Length - 2] = Vector3.right * 450.01f;
			positions[positions.Length - 3] = Vector3.right * 449.99f;
			for (int i = 1; i < c.Length - 1; i++)
			{
				positions[i + 2] = Vector3.right * c[i].time * 600;
			}
			l.positionCount = positions.Length;
			l.SetPositions(positions);
		}
	}

	private void Start()
	{
#if UNITY_EDITOR
		if (true)
#else
		if (Networking.LocalPlayer.displayName == "TonyEric")
#endif
		{
			Networking.SetOwner(Networking.LocalPlayer, gameObject);
		}
		else
		{
			GetComponent<BoxCollider>().enabled = false;
			GetComponent<Canvas>().enabled = false;
		}

		GradientToJson();
	}

	[SerializeField] private LineRenderer[] lineRenderers;
	[SerializeField] private Slider timeSlider;
	[SerializeField] private Slider secondTimeSlider;
	[SerializeField] private Slider backTimeSlider;
	[SerializeField] private Transform shadowCameraParent;
	[SerializeField] private Material worldMaterial;
	[SerializeField] private Material skyMaterial;
	[SerializeField] private Camera shadowCam1;
	[SerializeField] private Camera shadowCam;
	private bool timeChanged;
	[SerializeField] private Toggle editModeToggle;
	[SerializeField] private Button prevKey;
	[SerializeField] private Button nextKey;
	[SerializeField] private Button addKey;
	[SerializeField] private Button removeKey;
	public void EditMode()
	{
		var editMode = editModeToggle.isOn;
		red.interactable = editMode;
		green.interactable = editMode;
		blue.interactable = editMode;
		alpha.interactable = editMode;
		prevKey.interactable = editMode;
		nextKey.interactable = editMode;
		addKey.interactable = editMode;
		removeKey.interactable = editMode;
		foreach (Toggle t in directionToggleGroup) t.interactable = editMode;

		if (editMode)
		{
			timeSlider.interactable = currentKey != 0 && currentKey != gradients[(int)selectedDirection].colorKeys.Length - 1;
			removeKey.interactable = timeSlider.interactable;
			timeSlider.value = gradients[(int)selectedDirection].colorKeys[currentKey].time;
			UpdateView((int)selectedDirection);
		}
		else
		{
			timeSlider.interactable = true;

			removeKey.interactable = false;
		}
	}

	public void TimeChanged()
	{
		var old = timeSlider.value;
		if (editModeToggle.isOn && currentKey != 0 && currentKey != gradients[(int)selectedDirection].colorKeys.Length - 1)
		{
			var g = gradients[(int)selectedDirection];
			var c = g.colorKeys;
			var a = g.alphaKeys;
			var val = timeSlider.value;
			val = Mathf.Clamp(val, c[currentKey - 1].time, c[currentKey + 1].time);
			c[currentKey].time = val;
			a[currentKey].time = val;
			g.SetKeys(c, a);
			timeSlider.SetValueWithoutNotify(val);
			UpdateView((int)selectedDirection);
			GradientToJson();
		}
		secondTimeSlider.SetValueWithoutNotify(Mathf.Abs(((timeSlider.value + 0.25f) % 1) * 2 - 1) / 2 + 0.25f);
		backTimeSlider.SetValueWithoutNotify(1 - (timeSlider.value + 0.5f) % 1);
		timeChanged = old == timeSlider.value;
		if (timeChanged) RequestData();
	}

	private void RequestData()
	{
		worldMaterial.SetColor("_UpLightColor", gradients[0].Evaluate(secondTimeSlider.value));
		worldMaterial.SetColor("_DownLightColor", gradients[1].Evaluate(secondTimeSlider.value));
		worldMaterial.SetColor("_RightLightColor", gradients[2].Evaluate(secondTimeSlider.value));
		worldMaterial.SetColor("_LeftLightColor", gradients[3].Evaluate(secondTimeSlider.value));
		worldMaterial.SetColor("_FrontLightColor", gradients[4].Evaluate(timeSlider.value));
		worldMaterial.SetColor("_BackLightColor", gradients[4].Evaluate(backTimeSlider.value));

		if (!Networking.IsOwner(gameObject)) return;

		time = timeSlider.value;

		GradientColorKey[] keys;
		GradientAlphaKey[] alphaKeys;

		keys = gradients[0].colorKeys;
		alphaKeys = gradients[0].alphaKeys;
		up = new Color[keys.Length];
		upT = new int[keys.Length];
		for (int i = 0; i < keys.Length; i++)
		{
			up[i] = keys[i].color;
			up[i].a = alphaKeys[i].alpha;
			upT[i] = Mathf.RoundToInt(keys[i].time * 65535f);
		}

		keys = gradients[1].colorKeys;
		alphaKeys = gradients[1].alphaKeys;
		down = new Color[keys.Length];
		downT = new int[keys.Length];
		for (int i = 0; i < keys.Length; i++)
		{
			down[i] = keys[i].color;
			down[i].a = alphaKeys[i].alpha;
			downT[i] = Mathf.RoundToInt(keys[i].time * 65535f);
		}

		keys = gradients[2].colorKeys;
		alphaKeys = gradients[2].alphaKeys;
		right = new Color[keys.Length];
		rightT = new int[keys.Length];
		for (int i = 0; i < keys.Length; i++)
		{
			right[i] = keys[i].color;
			right[i].a = alphaKeys[i].alpha;
			rightT[i] = Mathf.RoundToInt(keys[i].time * 65535f);
		}

		keys = gradients[3].colorKeys;
		alphaKeys = gradients[3].alphaKeys;
		left = new Color[keys.Length];
		leftT = new int[keys.Length];
		for (int i = 0; i < keys.Length; i++)
		{
			left[i] = keys[i].color;
			left[i].a = alphaKeys[i].alpha;
			leftT[i] = Mathf.RoundToInt(keys[i].time * 65535f);
		}

		keys = gradients[4].colorKeys;
		alphaKeys = gradients[4].alphaKeys;
		front = new Color[keys.Length];
		frontT = new int[keys.Length];
		for (int i = 0; i < keys.Length; i++)
		{
			front[i] = keys[i].color;
			front[i].a = alphaKeys[i].alpha;
			frontT[i] = Mathf.RoundToInt(keys[i].time * 65535f);
		}

		keys = gradients[5].colorKeys;
		alphaKeys = gradients[5].alphaKeys;
		fogFront = new Color[keys.Length];
		fogFrontT = new int[keys.Length];
		for (int i = 0; i < keys.Length; i++)
		{
			fogFront[i] = keys[i].color;
			fogFront[i].a = alphaKeys[i].alpha;
			fogFrontT[i] = Mathf.RoundToInt(keys[i].time * 65535f);
		}

		keys = gradients[6].colorKeys;
		alphaKeys = gradients[6].alphaKeys;
		fogBack = new Color[keys.Length];
		fogBackT = new int[keys.Length];
		for (int i = 0; i < keys.Length; i++)
		{
			fogBack[i] = keys[i].color;
			fogBack[i].a = alphaKeys[i].alpha;
			fogBackT[i] = Mathf.RoundToInt(keys[i].time * 65535f);
		}

		RequestSerialization();
	}

	public override void OnDeserialization()
	{
		timeSlider.value = time;

		GradientColorKey[] colorKeys;
		GradientAlphaKey[] alphaKeys;
		Gradient gradient;

		gradient = gradients[0];
		colorKeys = new GradientColorKey[up.Length];
		alphaKeys = new GradientAlphaKey[up.Length];
		for (int i = 0; i < up.Length; i++)
		{
			float t = upT[i] / 65535f;
			colorKeys[i] = new GradientColorKey(up[i], t);
			alphaKeys[i] = new GradientAlphaKey(up[i].a, t);
		}
		gradient.SetKeys(colorKeys, alphaKeys);

		gradient = gradients[1];
		colorKeys = new GradientColorKey[down.Length];
		alphaKeys = new GradientAlphaKey[down.Length];
		for (int i = 0; i < down.Length; i++)
		{
			float t = downT[i] / 65535f;
			colorKeys[i] = new GradientColorKey(down[i], t);
			alphaKeys[i] = new GradientAlphaKey(down[i].a, t);
		}
		gradient.SetKeys(colorKeys, alphaKeys);

		gradient = gradients[2];
		colorKeys = new GradientColorKey[right.Length];
		alphaKeys = new GradientAlphaKey[right.Length];
		for (int i = 0; i < right.Length; i++)
		{
			float t = rightT[i] / 65535f;
			colorKeys[i] = new GradientColorKey(right[i], t);
			alphaKeys[i] = new GradientAlphaKey(right[i].a, t);
		}
		gradient.SetKeys(colorKeys, alphaKeys);

		gradient = gradients[3];
		colorKeys = new GradientColorKey[left.Length];
		alphaKeys = new GradientAlphaKey[left.Length];
		for (int i = 0; i < left.Length; i++)
		{
			float t = leftT[i] / 65535f;
			colorKeys[i] = new GradientColorKey(left[i], t);
			alphaKeys[i] = new GradientAlphaKey(left[i].a, t);
		}
		gradient.SetKeys(colorKeys, alphaKeys);

		gradient = gradients[4];
		colorKeys = new GradientColorKey[front.Length];
		alphaKeys = new GradientAlphaKey[front.Length];
		for (int i = 0; i < front.Length; i++)
		{
			float t = frontT[i] / 65535f;
			colorKeys[i] = new GradientColorKey(front[i], t);
			alphaKeys[i] = new GradientAlphaKey(front[i].a, t);
		}
		gradient.SetKeys(colorKeys, alphaKeys);

		gradient = gradients[5];
		colorKeys = new GradientColorKey[fogFront.Length];
		alphaKeys = new GradientAlphaKey[fogFront.Length];
		for (int i = 0; i < fogFront.Length; i++)
		{
			float t = fogFrontT[i] / 65535f;
			colorKeys[i] = new GradientColorKey(fogFront[i], t);
			alphaKeys[i] = new GradientAlphaKey(fogFront[i].a, t);
		}
		gradient.SetKeys(colorKeys, alphaKeys);

		gradient = gradients[6];
		colorKeys = new GradientColorKey[fogBack.Length];
		alphaKeys = new GradientAlphaKey[fogBack.Length];
		for (int i = 0; i < fogBack.Length; i++)
		{
			float t = fogBackT[i] / 65535f;
			colorKeys[i] = new GradientColorKey(fogBack[i], t);
			alphaKeys[i] = new GradientAlphaKey(fogBack[i].a, t);
		}
		gradient.SetKeys(colorKeys, alphaKeys);

		UpdateAllView();


		worldMaterial.SetColor("_UpLightColor", gradients[0].Evaluate(secondTimeSlider.value));
		worldMaterial.SetColor("_DownLightColor", gradients[1].Evaluate(secondTimeSlider.value));
		worldMaterial.SetColor("_RightLightColor", gradients[2].Evaluate(secondTimeSlider.value));
		worldMaterial.SetColor("_LeftLightColor", gradients[3].Evaluate(secondTimeSlider.value));
		worldMaterial.SetColor("_FrontLightColor", gradients[4].Evaluate(timeSlider.value));
		worldMaterial.SetColor("_BackLightColor", gradients[4].Evaluate(backTimeSlider.value));
	}

	private void LateUpdate()
	{
		if (!timeChanged) return;
		var a = timeSlider.value * Mathf.PI * 2;
		var pos = new Vector3(0, Mathf.Sin(a), Mathf.Cos(a));
		a = 50f / 180 * Mathf.PI;
		pos.x = Mathf.Sin(a) * pos.y;
		pos.y = Mathf.Cos(a) * pos.y;
		a = Mathf.Atan2(pos.x, pos.z);
		pos.z = new Vector2(pos.x, pos.z).magnitude;

		shadowCameraParent.eulerAngles = new Vector3(Mathf.Atan2(pos.y, pos.z) / Mathf.PI * 180, a / Mathf.PI * 180 - 180, 0);


		worldMaterial.SetMatrix("_ShadowMatrix", shadowCam.projectionMatrix * shadowCam.worldToCameraMatrix);
		skyMaterial.SetFloat("_TimePower", timeSlider.value);
		shadowCam1.enabled = true;
		timeChanged = false;
	}

	[SerializeField] private Toggle[] directionToggleGroup;

	private void ResetToggles()
	{
		Toggle ignore = directionToggleGroup[(int)selectedDirection];

		foreach (var toggle in directionToggleGroup)
			ResetToggle(toggle);

		ignore.enabled = false;
		currentKey = 0;
		UpdateSliders();
		GradientToJson();
	}

	private void ResetToggle(Toggle t)
	{
		if (t.enabled) return;
		t.SetIsOnWithoutNotify(false);
		t.enabled = true;
	}

	public void SelectUp()
	{
		selectedDirection = Direction.Up;
		ResetToggles();
	}

	public void SelectDown()
	{
		selectedDirection = Direction.Down;
		ResetToggles();
	}

	public void SelectRight()
	{
		selectedDirection = Direction.Right;
		ResetToggles();
	}

	public void SelectLeft()
	{
		selectedDirection = Direction.Left;
		ResetToggles();
	}

	public void SelectFront()
	{
		selectedDirection = Direction.Front;
		ResetToggles();
	}

	public void SelectFogFront()
	{
		selectedDirection = Direction.FogFront;
		ResetToggles();
	}

	public void SelectFogBack()
	{
		selectedDirection = Direction.FogBack;
		ResetToggles();
	}

	private void UpdateSliders()
	{
		var g = gradients[(int)selectedDirection];
		var c = g.colorKeys[currentKey].color;

		red.SetValueWithoutNotify(c.r);
		green.SetValueWithoutNotify(c.g);
		blue.SetValueWithoutNotify(c.b);
		alpha.SetValueWithoutNotify(c.a);
		timeSlider.value = g.colorKeys[currentKey].time;
	}

	public void RedChanged()
	{
		var g = gradients[(int)selectedDirection];
		var colorKeys = g.colorKeys;
		colorKeys[currentKey].color = new Color(red.value, green.value, blue.value, alpha.value);
		g.SetKeys(colorKeys, g.alphaKeys);
		UpdateView((int)selectedDirection);
		GradientToJson(); RequestData();
	}

	public void GreenChanged()
	{
		var g = gradients[(int)selectedDirection];
		var colorKeys = g.colorKeys;
		colorKeys[currentKey].color = new Color(red.value, green.value, blue.value, alpha.value);
		g.SetKeys(colorKeys, g.alphaKeys);
		UpdateView((int)selectedDirection);
		GradientToJson(); RequestData();
	}

	public void BlueChanged()
	{
		var g = gradients[(int)selectedDirection];
		var colorKeys = g.colorKeys;
		colorKeys[currentKey].color = new Color(red.value, green.value, blue.value, alpha.value);
		g.SetKeys(colorKeys, g.alphaKeys);
		UpdateView((int)selectedDirection);
		GradientToJson(); RequestData();
	}

	public void AlphaChanged()
	{
		var g = gradients[(int)selectedDirection];

		var alphaKeys = g.alphaKeys;
		alphaKeys[currentKey].alpha = alpha.value;

		g.SetKeys(g.colorKeys, alphaKeys);
		UpdateView((int)selectedDirection);
		GradientToJson(); RequestData();
	}

	private Direction selectedDirection;
	[SerializeField] public Slider red;
	[SerializeField] public Slider green;
	[SerializeField] public Slider blue;
	[SerializeField] public Slider alpha;

	[SerializeField]
	private Gradient[] gradients;

	private int currentKey = 0;
	public void NextKey()
	{
		var g = gradients[(int)selectedDirection];
		currentKey++;
		if (currentKey >= g.colorKeys.Length) currentKey = 0;
		timeSlider.value = g.colorKeys[currentKey].time;
		timeSlider.interactable = currentKey != 0 && currentKey != g.colorKeys.Length - 1;
		removeKey.interactable = timeSlider.interactable;
		var c = g.colorKeys[currentKey].color;
		red.SetValueWithoutNotify(c.r);
		green.SetValueWithoutNotify(c.g);
		blue.SetValueWithoutNotify(c.b);
		alpha.SetValueWithoutNotify(c.a);
	}

	public void PrevKey()
	{
		var g = gradients[(int)selectedDirection];
		currentKey--;
		if (currentKey < 0) currentKey = g.colorKeys.Length - 1;
		timeSlider.value = g.colorKeys[currentKey].time;
		timeSlider.interactable = currentKey != 0 && currentKey != g.colorKeys.Length - 1;
		removeKey.interactable = timeSlider.interactable;
		var c = g.colorKeys[currentKey].color;
		red.SetValueWithoutNotify(c.r);
		green.SetValueWithoutNotify(c.g);
		blue.SetValueWithoutNotify(c.b);
		alpha.SetValueWithoutNotify(g.alphaKeys[currentKey].alpha);
	}

	public void AddKey()
	{
		Debug.Log("AddKey");
		var g = gradients[(int)selectedDirection];

		var colorKeys = g.colorKeys;
		var alphaKeys = g.alphaKeys;

		int insertIndex = currentKey == colorKeys.Length - 1 ? currentKey : currentKey + 1;

		int left = insertIndex - 1;
		int right = insertIndex;

		float time = (colorKeys[left].time + colorKeys[right].time) * 0.5f;
		Color color = Color.Lerp(colorKeys[left].color, colorKeys[right].color, 0.5f);
		color.a = (alphaKeys[left].alpha + alphaKeys[right].alpha) * 0.5f;
		var newColorKeys = new GradientColorKey[colorKeys.Length + 1];
		var newAlphaKeys = new GradientAlphaKey[alphaKeys.Length + 1];

		Array.Copy(colorKeys, 0, newColorKeys, 0, insertIndex);
		Array.Copy(alphaKeys, 0, newAlphaKeys, 0, insertIndex);

		newColorKeys[insertIndex] = new GradientColorKey(color, time);
		newAlphaKeys[insertIndex] = new GradientAlphaKey(color.a, time);

		Array.Copy(
			colorKeys,
			insertIndex,
			newColorKeys,
			insertIndex + 1,
			colorKeys.Length - insertIndex);

		Array.Copy(
			alphaKeys,
			insertIndex,
			newAlphaKeys,
			insertIndex + 1,
			alphaKeys.Length - insertIndex);

		g.SetKeys(newColorKeys, newAlphaKeys);

		currentKey = insertIndex;

		timeSlider.value = time;
		timeSlider.interactable = true;
		removeKey.interactable = true;
		red.SetValueWithoutNotify(color.r);
		green.SetValueWithoutNotify(color.g);
		blue.SetValueWithoutNotify(color.b);
		alpha.SetValueWithoutNotify(color.a);
	}

	public void RemoveKey()
	{
		var g = gradients[(int)selectedDirection];

		var colorKeys = g.colorKeys;
		var alphaKeys = g.alphaKeys;

		var newColorKeys = new GradientColorKey[colorKeys.Length - 1];
		var newAlphaKeys = new GradientAlphaKey[alphaKeys.Length - 1];

		Array.Copy(colorKeys, 0, newColorKeys, 0, currentKey);
		Array.Copy(alphaKeys, 0, newAlphaKeys, 0, currentKey);

		Array.Copy(
			colorKeys,
			currentKey + 1,
			newColorKeys,
			currentKey,
			colorKeys.Length - currentKey - 1);

		Array.Copy(
			alphaKeys,
			currentKey + 1,
			newAlphaKeys,
			currentKey,
			alphaKeys.Length - currentKey - 1);

		g.SetKeys(newColorKeys, newAlphaKeys);

		if (currentKey >= newColorKeys.Length)
			currentKey = newColorKeys.Length - 1;

		timeSlider.value = newColorKeys[currentKey].time;
		timeSlider.interactable = currentKey != 0 && currentKey != gradients[(int)selectedDirection].colorKeys.Length - 1;
		removeKey.interactable = timeSlider.interactable;
		var c = newColorKeys[currentKey].color;
		red.SetValueWithoutNotify(c.r);
		green.SetValueWithoutNotify(c.g);
		blue.SetValueWithoutNotify(c.b);
		alpha.SetValueWithoutNotify(newAlphaKeys[currentKey].alpha);

		UpdateView((int)selectedDirection);
	}
}

public enum Direction
{
	Up,
	Down,
	Right,
	Left,
	Front,
	FogFront,
	FogBack
}
