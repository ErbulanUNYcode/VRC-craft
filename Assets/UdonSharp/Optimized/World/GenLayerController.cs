
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using Random = UnityEngine.Random;

namespace VRC_MINE.World
{
	/// <summary>
	/// Controls the initial biome-generation layer pass.
	/// The scene reference is assigned manually on the controller in the scene.
	/// </summary>
	[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
	public class GenLayerController : UdonSharpBehaviour
	{
		[SerializeField] private Material chunkGeneratorMaterial;
		[SerializeField] private Material chunkTerrainGeneratorMaterial;
		[SerializeField] private Material[] genLayers;
		[SerializeField] private CustomRenderTexture[] genTextures;
		[SerializeField] private CustomRenderTexture[] voxelTextures;
		[SerializeField] private CustomRenderTextureUpdateZone[] voxelWorldZones = new CustomRenderTextureUpdateZone[1];
		[SerializeField] private CustomRenderTextureUpdateZone[] lod1Zones = new CustomRenderTextureUpdateZone[1];
		[SerializeField] private CustomRenderTextureUpdateZone[] lod2Zones = new CustomRenderTextureUpdateZone[1];
		[SerializeField] private CustomRenderTextureUpdateZone[] noZones = new CustomRenderTextureUpdateZone[0];
		[SerializeField] private WorldGenerator worldGenerator;

		private int generationSeed;
		private bool generationFinished;

		public bool GenerationFinished => generationFinished;

		private void Start()
		{
			int seed;
			#if UNITY_EDITOR
			seed = Random.Range(int.MinValue, int.MaxValue);
			#else
			seed = Networking.GetUniqueName(gameObject).GetHashCode();
			#endif
			StartBiomeGeneration(seed);
		}

		public void StartBiomeGeneration(int seed)
		{
			generationSeed = seed;
			generationFinished = false;
			ClearVoxelTextures();

			Debug.Log("<color=#00ff00><<<</color><color=#0000ff>MCGE</color><color=#00ff00>>>> </color>seed is " + generationSeed);

			Random.InitState(generationSeed);
			int climateSeed = generationSeed ^ 0x5F3759DF;

			if (chunkTerrainGeneratorMaterial != null)
				chunkTerrainGeneratorMaterial.SetInt("_Seed", generationSeed);

			if (chunkGeneratorMaterial != null)
			{
				chunkGeneratorMaterial.SetInt("_Seed", generationSeed);
				chunkGeneratorMaterial.SetVector("_CavesOffset", RandomOffset());
				chunkGeneratorMaterial.SetVector("_Ore1Offset", RandomOffset());
				chunkGeneratorMaterial.SetVector("_Ore2Offset", RandomOffset());
				chunkGeneratorMaterial.SetVector("_Ore3Offset", RandomOffset());
				chunkGeneratorMaterial.SetVector("_DirtOffset", RandomOffset());
				chunkGeneratorMaterial.SetVector("_GravelOffset", RandomOffset());
				chunkGeneratorMaterial.SetVector("_GraniteOffset", RandomOffset());
				chunkGeneratorMaterial.SetVector("_AndesiteOffset", RandomOffset());
				chunkGeneratorMaterial.SetVector("_DioriteOffset", RandomOffset());
			}

			if (genLayers != null)
			{
				foreach (var genLayer in genLayers)
				{
					if (genLayer == null) continue;
					genLayer.SetInt("_Seed", Random.Range(0, int.MaxValue));
					genLayer.SetInt("_ClimateSeed", climateSeed);
				}
			}

			if (genTextures != null)
				foreach (var texture in genTextures)
				{
					if (texture == null) continue;
					texture.Update();
				}

			SendCustomEventDelayedFrames(nameof(RestoreVoxelUpdateZones), 1);
			SendCustomEventDelayedFrames(nameof(FinishBiomeGeneration), 2);
		}

		private void ClearVoxelTextures()
		{
			if (voxelTextures == null) return;

			for (int i = 0; i < voxelTextures.Length; i++)
			{
				CustomRenderTexture texture = voxelTextures[i];
				if (texture == null) continue;
				texture.initializationSource = CustomRenderTextureInitializationSource.TextureAndColor;
				texture.initializationTexture = null;
				texture.initializationColor = Color.clear;
				texture.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
				texture.SetUpdateZones(noZones);
				texture.Initialize();
			}
		}

		public void RestoreVoxelUpdateZones()
		{
			if (voxelTextures == null) return;
			for (int i = 0; i < voxelTextures.Length; i++)
				if (voxelTextures[i] != null)
					voxelTextures[i].initializationSource = CustomRenderTextureInitializationSource.Material;

			if (voxelTextures.Length > 0 && voxelTextures[0] != null)
				voxelTextures[0].SetUpdateZones(voxelWorldZones);
			if (voxelTextures.Length > 1 && voxelTextures[1] != null)
				voxelTextures[1].SetUpdateZones(lod1Zones);
			if (voxelTextures.Length > 2 && voxelTextures[2] != null)
				voxelTextures[2].SetUpdateZones(lod2Zones);
			if (voxelTextures.Length > 3 && voxelTextures[3] != null)
				voxelTextures[3].SetUpdateZones(noZones);
		}

		public void FinishBiomeGeneration()
		{
			generationFinished = true;

			// Give the biome CRT pass a couple of frames to finish, then start
			// the next GPU pipeline stage.
			if (worldGenerator != null)
				worldGenerator.enabled = true;

			enabled = false;
		}

		private Vector3 RandomOffset()
		{
			return new Vector3(
				Random.Range(-10000, 10000),
				Random.Range(-10000, 10000),
				Random.Range(-10000, 10000));
		}
	}
}
