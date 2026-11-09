
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

			SendCustomEventDelayedFrames(nameof(FinishBiomeGeneration), 2);
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
