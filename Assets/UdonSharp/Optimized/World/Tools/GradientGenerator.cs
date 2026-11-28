using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Rendering;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GradientGenerator : UdonSharpBehaviour
{
	private float time;
	private Color[] up;
	private Color[] down;
	private Color[] right;
	private Color[] left;
	private Color[] front;
	private Color[] fogFront;
	private Color[] fogBack;
	private int[] upT;
	private int[] downT;
	private int[] rightT;
	private int[] leftT;
	private int[] frontT;
	private int[] fogFrontT;
	private int[] fogBackT;


	private float runtimeTime = 0.07f;
	private const float StartCycleTime = 4.07f;
	private float cycleTime = StartCycleTime;
	private const float DayDurationSeconds = 24f * 60f;
	[UdonSynced] private double clockStartServerTime;
	[UdonSynced] private bool clockStarted;
	private bool localClockAnchored;
	private float localClockAnchor;
	private double elapsedAtAnchor;
	private int lastClockTick = -1;
	// Kept as empty compatibility hooks for old Unity event bindings. The
	// gradient editor and LineRenderer preview are no longer implemented.
	private void GradientToJson() { }
	private void UpdateView(int id) { }

	private void Start()
	{
		shadowCam.SetReplacementShader(replacementShader, "RenderType");
		shadowCam1.SetReplacementShader(replacementShader, "RenderType");
		shadowCam.enabled = true;
		shadowCam1.enabled = false;
		StartClockIfOwner();
		godRaysRight.enabled = Networking.LocalPlayer.IsUserInVR();
#if UNITY_EDITOR
		godRaysMaterial.SetFloat("_EditorFlarePreview", 1f);
#else
		godRaysMaterial.SetFloat("_EditorFlarePreview", 0f);
#endif
		timeChanged = true;
		runtimeTime = 0.07f;
		if (moonDaySlider != null)
		{
			moonDaySlider.minValue = 0f;
			moonDaySlider.maxValue = 7f;
			moonDaySlider.wholeNumbers = true;
		}


	}

	[System.NonSerialized] private LineRenderer[] lineRenderers;
	[System.NonSerialized] private Slider timeSlider;
	[System.NonSerialized] private Slider moonDaySlider;
	[System.NonSerialized] private Slider secondTimeSlider;
	[System.NonSerialized] private Slider backTimeSlider;
	[SerializeField] private Transform shadowCameraParent;
	[SerializeField] private Material worldMaterial;
	[SerializeField] private Material skyMaterial;
	[SerializeField] private Camera shadowCam1;
	[SerializeField] private Camera shadowCam;
	[SerializeField] private Shader replacementShader;

	[Header("God Rays")]
	[SerializeField] private Camera godRaysLeft;
	[SerializeField] private Camera godRaysRight;
	[SerializeField] private Camera godRaysPhoto;
	[SerializeField] private Material godRaysMaterial;
	private int moonDay;
	[Tooltip("One offset per moon phase, in pixels from the center of the 480 px frame. Positive Y is up.")]
	[SerializeField] private Vector2[] moonLightOffsets =
	{
		Vector2.zero, new Vector2(-34f, 1f), new Vector2(-5.5f, -22f), new Vector2(19f / 3f, -19f / 3f),
		new Vector2(19f / 3f, -19f / 3f), new Vector2(26.5f, -10f), new Vector2(26.5f, -10f), new Vector2(30f, 25f)
	};
	[Tooltip("Moon phases 0–7, then the sun. RGB colors the rays; alpha sets flare brightness.")]
	[SerializeField] private Color[] godRaysColors =
	{
		new Color(0f, 0f, 0f, 0f),
		new Color(0.22f / 3f, 0.28f / 3f, 0.38f / 3f, 1f / 3f),
		new Color(0.22f * 2f / 3f, 0.28f * 2f / 3f, 0.38f * 2f / 3f, 2f / 3f),
		new Color(0.22f, 0.28f, 0.38f, 1f),
		new Color(0.22f, 0.28f, 0.38f, 1f),
		new Color(0.22f * 2f / 3f, 0.28f * 2f / 3f, 0.38f * 2f / 3f, 2f / 3f),
		new Color(0.22f * 2f / 3f, 0.28f * 2f / 3f, 0.38f * 2f / 3f, 2f / 3f),
		new Color(0.22f / 3f, 0.28f / 3f, 0.38f / 3f, 1f / 3f),
		Color.white
	};
	private Vector3 moonDirection;
	private Quaternion moonCenterRotation;
	private Quaternion moonRotation;
	private int renderedMoonDay = -1;
	private bool timeChanged;
	[System.NonSerialized] private Toggle editModeToggle;
	[System.NonSerialized] private Button prevKey;
	[System.NonSerialized] private Button nextKey;
	[System.NonSerialized] private Button addKey;
	[System.NonSerialized] private Button removeKey;
	public void EditMode()
	{
		// Keep the editor permanently disabled. This also protects against a
		// Toggle event or a stale serialized value re-enabling key editing.
		const bool editMode = false;
		if (editModeToggle != null) editModeToggle.SetIsOnWithoutNotify(false);
		red.interactable = editMode;
		green.interactable = editMode;
		blue.interactable = editMode;
		alpha.interactable = editMode;
		prevKey.interactable = editMode;
		nextKey.interactable = editMode;
		addKey.interactable = editMode;
		removeKey.interactable = editMode;
		foreach (Toggle t in directionToggleGroup) t.interactable = editMode;
		// The day selector is a runtime control.  It is intentionally available
		// while scrubbing time, but locked while editing a gradient key.
		if (moonDaySlider != null) moonDaySlider.interactable = !editMode;

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
			if (moonDaySlider != null) moonDaySlider.interactable = true;

			removeKey.interactable = false;
		}
	}

	public void TimeChanged()
	{
		var old = timeSlider.value;
		if (false && currentKey != 0 && currentKey != gradients[(int)selectedDirection].colorKeys.Length - 1)
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


	private bool UsesMoonShadows(float dayTime)
	{
		return dayTime >= 0.53f && dayTime <= 0.97f;
	}

	private float GetFaceLightStrength()
	{
		return UsesMoonShadows(runtimeTime) ? GetGodRaysColor(Mathf.Clamp(moonDay, 0, 7)).a : 1f;
	}

	private void UpdateFaceLighting(float strength)
	{
		float secondTime = Mathf.Abs(((runtimeTime + 0.25f) % 1f) * 2f - 1f) / 2f + 0.25f;
		float backTime = 1f - (runtimeTime + 0.5f) % 1f;
		SetFaceLight("_UpLightColor", gradients[0].Evaluate(secondTime), strength);
		SetFaceLight("_DownLightColor", gradients[1].Evaluate(secondTime), strength);
		SetFaceLight("_RightLightColor", gradients[2].Evaluate(secondTime), strength);
		SetFaceLight("_LeftLightColor", gradients[3].Evaluate(secondTime), strength);
		SetFaceLight("_FrontLightColor", gradients[4].Evaluate(runtimeTime), strength);
		SetFaceLight("_BackLightColor", gradients[4].Evaluate(backTime), strength);
		if (gradients.Length > 7)
			worldMaterial.SetColor("_FogColor", gradients[gradients.Length - 1].Evaluate(runtimeTime));
	}

	private void SetFaceLight(string property, Color color, float strength)
	{
		color.a *= strength;
		worldMaterial.SetColor(property, color);
	}

	private void RequestData()
	{
		UpdateFaceLighting(GetFaceLightStrength());


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


	}

	public override void OnDeserialization()
	{
		// Gradient editing/network reconstruction was removed. Keep the
		// serialized gradient array authoritative; only the runtime clock and
		// material update path are used now.
		return;

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


		UpdateFaceLighting(GetFaceLightStrength());
	}

	public void MoonDayChanged()
	{
	}

	private void StartClockIfOwner()
	{
		if (clockStarted || !Networking.IsOwner(gameObject)) return;
		clockStartServerTime = Networking.GetServerTimeInSeconds();
		clockStarted = true;
		RequestSerialization();
	}

	private void LateUpdate()
	{
		if (!clockStarted && Networking.IsNetworkSettled)
			StartClockIfOwner();

		float localNow = Time.realtimeSinceStartup;
		if (clockStarted && !localClockAnchored)
		{
			elapsedAtAnchor = Networking.GetServerTimeInSeconds() - clockStartServerTime;
			if (elapsedAtAnchor < 0.0) elapsedAtAnchor = 0.0;
			localClockAnchor = localNow;
			localClockAnchored = true;
		}

		double elapsed = localClockAnchored ? elapsedAtAnchor + ((double)localNow - localClockAnchor) : 0.0;
		float elapsedSeconds = Mathf.Max(0f, (float)elapsed);
		cycleTime = StartCycleTime + elapsedSeconds / DayDurationSeconds;
		runtimeTime = Mathf.Repeat(cycleTime, 1f);
		moonDay = Mathf.FloorToInt(cycleTime) % 8;
		float dayTime = runtimeTime;
		var a = dayTime * Mathf.PI * 2;
		var pos = new Vector3(0, Mathf.Sin(a), Mathf.Cos(a));
		a = skyMaterial.GetFloat("_Angle") * Mathf.Deg2Rad;
		pos.x = Mathf.Sin(a) * pos.y;
		pos.y = Mathf.Cos(a) * pos.y;
		a = Mathf.Atan2(pos.x, pos.z);
		pos.z = new Vector2(pos.x, pos.z).magnitude;

		moonCenterRotation = Quaternion.Euler(Mathf.Atan2(pos.y, pos.z) / Mathf.PI * 180, a / Mathf.PI * 180 - 180, 0);
		skyMaterial.SetFloat("_TimePower", cycleTime);
		UpdateMoonLighting(moonDay);

		int clockTick = Mathf.FloorToInt(elapsedSeconds * 4f);
		if (clockTick == lastClockTick) return;
		lastClockTick = clockTick;

		bool moonShadows = UsesMoonShadows(dayTime);
		// Shadow cameras sit behind the target and look away from the selected light.
		shadowCameraParent.rotation = moonShadows
			? moonCenterRotation * Quaternion.Euler(0f, 180f, 0f)
			: moonCenterRotation;


		UpdateFaceLighting(GetFaceLightStrength());
		shadowPositionReady = false;
	}

	private void UpdateMoonLighting(int day)
	{
		Vector2 center = moonLightOffsets != null && day < moonLightOffsets.Length ? moonLightOffsets[day] : Vector2.zero;

		// Invert the skybox's atlas projection and then its two sky rotations.
		Vector3 atlasDirection = new Vector3(-center.x / 1200f, center.y / 1200f, -1f);
		Quaternion toWorld = Quaternion.AngleAxis(-skyMaterial.GetFloat("_Angle"), Vector3.forward)
			* Quaternion.AngleAxis(-360f * runtimeTime, Vector3.right);
		moonDirection = (toWorld * atlasDirection).normalized;
		moonRotation = Quaternion.LookRotation(moonDirection, moonCenterRotation * Vector3.up);
		renderedMoonDay = day;
		skyMaterial.SetFloat("_MoonDay", day);
		skyMaterial.SetVector("_MoonRaysDirection", moonDirection);
	}

	private bool shadowPositionReady;
	private Vector3Int shadowRenderPos;

	public override void PostLateUpdate()
	{
		var localPlayer = Networking.LocalPlayer;
		if (localPlayer == null) return;

		Vector3 predictedPosition = localPlayer.GetPosition() + localPlayer.GetVelocity() * Time.deltaTime;
		UpdateShadowRendering(Vector3Int.FloorToInt(predictedPosition + Vector3.up * 0.5f));

		Quaternion sunRotation = moonCenterRotation * Quaternion.Euler(0f, 180f, 0f);
		var leftEye = Camera.StereoscopicEye.Left;
		var rightEye = Camera.StereoscopicEye.Right;
		Vector3 headForward = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation * Vector3.forward;
		bool headLooksAtMoon = Vector3.Dot(headForward, moonDirection) > 0f;
		Quaternion headLightRotation = headLooksAtMoon ? moonRotation : sunRotation;

		UpdateGodRaysCamera(godRaysLeft, "_LeftLightProjection",
			VRCCameraSettings.GetEyePosition(leftEye), headLightRotation);
		if (godRaysRight.enabled)
			UpdateGodRaysCamera(godRaysRight, "_RightLightProjection",
				VRCCameraSettings.GetEyePosition(rightEye), headLightRotation);

		var photoCamera = VRCCameraSettings.PhotoCamera;
		// ClientSim does not expose a handheld camera.
		bool photoActive = photoCamera != null && photoCamera.Active;
		bool photoLooksAtMoon = false;
		if (photoActive)
		{
			photoLooksAtMoon = Vector3.Dot(photoCamera.Forward, moonDirection) > 0f;
			UpdateGodRaysCamera(godRaysPhoto, "_PhotoLightProjection", photoCamera.Position,
				photoLooksAtMoon ? moonRotation : sunRotation);
		}
		if (godRaysPhoto.enabled != photoActive)
			godRaysPhoto.enabled = photoActive;
		godRaysMaterial.SetFloat("_PhotoMaskReady", photoActive ? 1f : 0f);
		godRaysMaterial.SetColor("_EyeLightColor", GetGodRaysColor(headLooksAtMoon ? renderedMoonDay : 8));
		godRaysMaterial.SetColor("_PhotoLightColor", GetGodRaysColor(photoLooksAtMoon ? renderedMoonDay : 8));
		godRaysMaterial.SetVector("_MoonFlareCameras", new Vector4(headLooksAtMoon ? 1f : 0f, photoLooksAtMoon ? 1f : 0f, 0f, 0f));
	}

	private Color GetGodRaysColor(int index)
	{
		if (godRaysColors != null && index >= 0 && index < godRaysColors.Length) return godRaysColors[index];
		return index == 8 ? Color.white : Color.clear;
	}

	private void UpdateGodRaysCamera(Camera maskCamera, string projectionName,
		Vector3 eyePosition, Quaternion lightRotation)
	{
		maskCamera.transform.SetPositionAndRotation(eyePosition, lightRotation);
		godRaysMaterial.SetMatrix(projectionName, maskCamera.projectionMatrix * maskCamera.worldToCameraMatrix);
	}

	private void UpdateShadowRendering(Vector3Int pos)
	{
		bool refreshFarShadow = !shadowPositionReady || shadowRenderPos != pos;
		shadowCam1.enabled = refreshFarShadow;
		if (refreshFarShadow)
		{
			Vector3 oldCamPos = shadowCam.transform.position;
			Vector3 oldCamPos1 = shadowCam1.transform.position;
			shadowCameraParent.localPosition = pos;
			Vector3 shadowTarget = shadowCameraParent.TransformPoint(new Vector3(0, 0, -327.68f));

			shadowCam.transform.position = oldCamPos;
			shadowCam.transform.position = shadowCam.transform.TransformPoint(
				Vector3Int.RoundToInt(shadowCam.transform.InverseTransformPoint(
					shadowTarget)));
			shadowCam1.transform.position = oldCamPos1;
			shadowCam1.transform.position = shadowCam1.transform.TransformPoint(
				Vector3Int.RoundToInt(shadowCam1.transform.InverseTransformPoint(
					shadowTarget)));
			shadowRenderPos = pos;
			shadowPositionReady = true;
		}

		Vector3 shadowCam1Offset = shadowCam.transform.InverseTransformPoint(shadowCam1.transform.position);
		shadowCam1Offset.x /= 1000f;
		shadowCam1Offset.y /= 1000f;
		shadowCam1Offset.z /= 65536f;
		worldMaterial.SetVector("_ShadowLodOffset", shadowCam1Offset);
		worldMaterial.SetMatrix("_ShadowMatrix", shadowCam.projectionMatrix * shadowCam.worldToCameraMatrix);
	}

	[System.NonSerialized] private Toggle[] directionToggleGroup;

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
	[System.NonSerialized] public Slider red;
	[System.NonSerialized] public Slider green;
	[System.NonSerialized] public Slider blue;
	[System.NonSerialized] public Slider alpha;

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
		// Evaluate the actual gradient instead of pairing color and alpha arrays
		// by index. Their key times are independent, so index-based interpolation
		// can select the wrong alpha (and therefore the wrong resulting color).
		Color color = g.Evaluate(time);
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
