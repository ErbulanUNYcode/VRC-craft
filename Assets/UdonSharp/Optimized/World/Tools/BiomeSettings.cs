using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BiomeSettings : UdonSharpBehaviour
{
	[SerializeField] private CustomRenderTexture[] genTextures;
	[SerializeField] private Material[] genLayers;
	[SerializeField] private Material view;
	[SerializeField] private Slider[] climateProbabilities;
	[SerializeField] private Slider[] climateWeights;
	[SerializeField] private Slider[] biomeProbabilities;
	[SerializeField] private Slider[] biomeWeights;
	[SerializeField] private Slider seed;
	[SerializeField]
	private string[] biomes = {
				"Polar",
				"Cold",
				"Temp",
				"Warm",

		"Tundra",
		"SnowForest",
		"SnowTaiga",
		"SnowPlain",

		"Taiga",
		"DarkForest",
		"Swamp",
		"DenseForest",

		"Plain",
		"Forest",
		"BrichForest",
		"SakuraForest",

		"Desert",
		"Savanna",
		"Jungle",
		"Wasteland"
	};
	[UdonSynced]
	[SerializeField]
	private float[] _climateProbabilities = new float[4];
	[UdonSynced]
	[SerializeField]
	private float[] _biomeProbabilities = new float[16];
	[UdonSynced]
	[SerializeField]
	private float[] _weights = new float[22];
	[UdonSynced]
	private float seedSynced;
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
			return;
		}

		for (int i = 0; i < 4; i++)//
		{
			climateProbabilities[i].GetComponentInChildren<Image>().color = view.GetColor("_" + biomes[i] + "Color");
			climateWeights[i].GetComponentInChildren<Image>().color = view.GetColor("_" + biomes[i] + "Color");
			climateProbabilities[i].GetComponentInChildren<TextMeshProUGUI>().text = biomes[i] + " Probability";
			climateWeights[i].GetComponentInChildren<TextMeshProUGUI>().text = biomes[i] + " Weight";
		}

		for (int i = 0; i < 16; i++)//
		{
			biomeProbabilities[i].GetComponentInChildren<Image>().color = view.GetColor("_" + biomes[i + 4] + "Color");
			biomeProbabilities[i].GetComponentInChildren<TextMeshProUGUI>().text = biomes[i + 4] + " Probability";
			biomeWeights[i].GetComponentInChildren<Image>().color = view.GetColor("_" + biomes[i + 4] + "Color");
			biomeWeights[i].GetComponentInChildren<TextMeshProUGUI>().text = biomes[i + 4] + " Weight";
		}

		_weights[0] = 0;
		_weights[1] = 1;

		UpdateBiomeSettings();
	}

	private void UpdateBiomeSettings()
	{
		for (int i = 0; i < 4; i++)
		{
			_climateProbabilities[i] = climateProbabilities[i].value;
			_weights[i + 2] = climateWeights[i].value;
		}

		for (int i = 0; i < 16; i++)
		{
			_biomeProbabilities[i] = biomeProbabilities[i].value;
			_weights[i + 6] = biomeWeights[i].value;
		}

		Random.InitState((int)seed.value);
		var sakuraProbability = biomeProbabilities[11].value;
		for (var i = 0; i < genLayers.Length; i++)
		{
			var layer = genLayers[i];
			_biomeProbabilities[11] = i == 1 ? 0 : sakuraProbability;
			layer.SetFloatArray("_BiomeProbabilities", _biomeProbabilities);
			layer.SetFloatArray("_Weights", _weights);
			layer.SetFloatArray("_ClimateProbabilities", _climateProbabilities);
			layer.SetInt("_Seed", Random.Range(0, int.MaxValue));
		}
	}
	public void GenTexturesStart()
	{
		UpdateBiomeSettings();
		seedSynced = seed.value;
		RequestSerialization();
		foreach (var texture in genTextures)
		{
			texture.initializationMode = CustomRenderTextureUpdateMode.Realtime;
		}
		SendCustomEventDelayedFrames(nameof(GenTexturesStop), 2);
	}
	public void GenTexturesStop()
	{
		foreach (var texture in genTextures)
		{
			texture.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
		}
	}

	public override void OnDeserialization()
	{
		for (int i = 0; i < 4; i++)
		{
			climateProbabilities[i].value = _climateProbabilities[i];
			climateWeights[i].value = _weights[i + 2];
		}

		for (int i = 0; i < 16; i++)
		{
			biomeProbabilities[i].value = _biomeProbabilities[i];
			biomeWeights[i].value = _weights[i + 6];
		}

		seed.value = seedSynced;
		Random.InitState((int)seed.value);

		var sakuraProbability = biomeProbabilities[11].value;
		for (var i = 0; i < genLayers.Length; i++)
		{
			var layer = genLayers[i];
			_biomeProbabilities[11] = i == 1 ? 0 : sakuraProbability;
			layer.SetFloatArray("_BiomeProbabilities", _biomeProbabilities);
			layer.SetFloatArray("_Weights", _weights);
			layer.SetFloatArray("_ClimateProbabilities", _climateProbabilities);
			layer.SetInt("_Seed", Random.Range(0, int.MaxValue));
		}
		foreach (var texture in genTextures) texture.initializationMode = CustomRenderTextureUpdateMode.Realtime;
		SendCustomEventDelayedFrames(nameof(GenTexturesStop), 2);
	}
}
