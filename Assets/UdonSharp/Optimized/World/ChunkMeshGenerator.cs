using UdonSharp;
using UnityEngine;

namespace VRC_MINE.World
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class ChunkMeshGenerator : UdonSharpBehaviour
	{
		[SerializeField] private CustomRenderTexture[] biomeGenerators;

		public void StartBiomeGeneration()
		{
			foreach (var biomeGenerator in biomeGenerators)
			{
				biomeGenerator.initializationMode = CustomRenderTextureUpdateMode.Realtime;
			}
			SendCustomEventDelayedFrames(nameof(StopBiomeGeneration), 1);
		}
		public void StopBiomeGeneration()
		{
			foreach (var biomeGenerator in biomeGenerators)
			{
				biomeGenerator.initializationMode = CustomRenderTextureUpdateMode.OnDemand;
			}
		}

		private Mesh[] meshes = new Mesh[9];

		public Mesh GetMesh(ChunkMeshType type)
		{
			if (type == ChunkMeshType.NN) return null;
			return meshes[(int)type];
		}
		public void CustomStart()
		{
			SendCustomEventDelayedFrames(nameof(StartBiomeGeneration), 1);
			meshes[0] = CreateTypePP();
			meshes[1] = CreateTypeMP();
			meshes[2] = CreateTypePM();
			meshes[3] = CreateTypeMM();
			meshes[4] = CreateTypeEP();
			meshes[5] = CreateTypePE();
			meshes[6] = CreateTypeEM();
			meshes[7] = CreateTypeME();
			meshes[8] = CreateTypeEE();
		}

		private Mesh CreateTypePP()
		{
			var m = new Mesh();
			var q = 385;
			var v = new Vector3[q * 4];
			var c = new Color[q * 4];
			var uv = new Vector2[q * 4];
			var t = new int[q * 6];
			var id = 0;
			for (var i = 0; i < 32; i++)
			{
				for (var j = 0; j < 4; j++)
				{
					var y = j * 32;
					var id4 = id * 4;
					var uvC = new Vector2(i, j);

					v[id4] = new Vector3(i, y, 0);
					c[id4] = new Color(1, 0, 0, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 0);
					c[id4] = new Color(1, 0, 0, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 32);
					c[id4] = new Color(1, 0, 0, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y, 32);
					c[id4] = new Color(1, 0, 0, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 0; i < 32; i++)
			{
				for (var j = 0; j < 4; j++)
				{
					var uvC = new Vector2(i, j + 4);
					var y = j * 32;
					var id4 = id * 4;

					v[id4] = new Vector3(0, y, i);
					c[id4] = new Color(0, 0, 1, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(0, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y, i);
					c[id4] = new Color(0, 0, 1, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 128; i > -1; i--)
			{
				var uvC = new Vector2((i - 1) & 31, ((i - 1) >> 5) + 8);
				var id4 = id * 4;
				v[id4] = new Vector3(0, i, 0);
				c[id4] = new Color(0, 1, 0, 0);
				uv[id4++] = uvC;

				v[id4] = new Vector3(0, i, 32);
				c[id4] = new Color(0, 1, 0, 0.25f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 32);
				c[id4] = new Color(0, 1, 0, 0.75f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 0);
				c[id4] = new Color(0, 1, 0, 1);
				uv[id4] = uvC;

				id4 -= 3;
				var id6 = id * 6;

				t[id6++] = id4;
				t[id6++] = id4 + 1;
				t[id6++] = id4 + 2;

				t[id6++] = id4;
				t[id6++] = id4 + 2;
				t[id6] = id4 + 3;
				id++;
			}

			m.vertices = v;
			m.uv = uv;
			m.colors = c;
			m.triangles = t;

			return m;
		}

		private Mesh CreateTypeMP()
		{
			var m = new Mesh();
			var q = 385;
			var v = new Vector3[q * 4];
			var c = new Color[q * 4];
			var uv = new Vector2[q * 4];
			var t = new int[q * 6];
			var id = 0;
			for (var i = 31; i > -1; i--)
			{
				for (var j = 0; j < 4; j++)
				{
					var y = j * 32;
					var id4 = id * 4;
					var uvC = new Vector2(i, j);

					v[id4] = new Vector3(i, y, 0);
					c[id4] = new Color(1, 0, 0, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 0);
					c[id4] = new Color(1, 0, 0, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 32);
					c[id4] = new Color(1, 0, 0, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y, 32);
					c[id4] = new Color(1, 0, 0, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 0; i < 32; i++)
			{
				for (var j = 0; j < 4; j++)
				{
					var uvC = new Vector2(i, j + 4);
					var y = j * 32;
					var id4 = id * 4;

					v[id4] = new Vector3(0, y, i);
					c[id4] = new Color(0, 0, 1, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(0, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y, i);
					c[id4] = new Color(0, 0, 1, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 128; i > -1; i--)
			{
				var uvC = new Vector2((i - 1) & 31, ((i - 1) >> 5) + 8);
				var id4 = id * 4;
				v[id4] = new Vector3(0, i, 0);
				c[id4] = new Color(0, 1, 0, 0);
				uv[id4++] = uvC;

				v[id4] = new Vector3(0, i, 32);
				c[id4] = new Color(0, 1, 0, 0.25f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 32);
				c[id4] = new Color(0, 1, 0, 0.75f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 0);
				c[id4] = new Color(0, 1, 0, 1);
				uv[id4] = uvC;

				id4 -= 3;
				var id6 = id * 6;

				t[id6++] = id4;
				t[id6++] = id4 + 1;
				t[id6++] = id4 + 2;

				t[id6++] = id4;
				t[id6++] = id4 + 2;
				t[id6] = id4 + 3;
				id++;
			}

			m.vertices = v;
			m.uv = uv;
			m.colors = c;
			m.triangles = t;

			return m;
		}

		private Mesh CreateTypePM()
		{
			var m = new Mesh();
			var q = 385;
			var v = new Vector3[q * 4];
			var c = new Color[q * 4];
			var uv = new Vector2[q * 4];
			var t = new int[q * 6];
			var id = 0;
			for (var i = 0; i < 32; i++)
			{
				for (var j = 0; j < 4; j++)
				{
					var y = j * 32;
					var id4 = id * 4;
					var uvC = new Vector2(i, j);

					v[id4] = new Vector3(i, y, 0);
					c[id4] = new Color(1, 0, 0, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 0);
					c[id4] = new Color(1, 0, 0, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 32);
					c[id4] = new Color(1, 0, 0, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y, 32);
					c[id4] = new Color(1, 0, 0, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 31; i > -1; i--)
			{
				for (var j = 0; j < 4; j++)
				{
					var uvC = new Vector2(i, j + 4);
					var y = j * 32;
					var id4 = id * 4;

					v[id4] = new Vector3(0, y, i);
					c[id4] = new Color(0, 0, 1, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(0, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y, i);
					c[id4] = new Color(0, 0, 1, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 128; i > -1; i--)
			{
				var uvC = new Vector2((i - 1) & 31, ((i - 1) >> 5) + 8);
				var id4 = id * 4;
				v[id4] = new Vector3(0, i, 0);
				c[id4] = new Color(0, 1, 0, 0);
				uv[id4++] = uvC;

				v[id4] = new Vector3(0, i, 32);
				c[id4] = new Color(0, 1, 0, 0.25f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 32);
				c[id4] = new Color(0, 1, 0, 0.75f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 0);
				c[id4] = new Color(0, 1, 0, 1);
				uv[id4] = uvC;

				id4 -= 3;
				var id6 = id * 6;

				t[id6++] = id4;
				t[id6++] = id4 + 1;
				t[id6++] = id4 + 2;

				t[id6++] = id4;
				t[id6++] = id4 + 2;
				t[id6] = id4 + 3;
				id++;
			}

			m.vertices = v;
			m.uv = uv;
			m.colors = c;
			m.triangles = t;

			return m;
		}

		private Mesh CreateTypeMM()
		{
			var m = new Mesh();
			var q = 385;
			var v = new Vector3[q * 4];
			var c = new Color[q * 4];
			var uv = new Vector2[q * 4];
			var t = new int[q * 6];
			var id = 0;
			for (var i = 31; i > -1; i--)
			{
				for (var j = 0; j < 4; j++)
				{
					var y = j * 32;
					var id4 = id * 4;
					var uvC = new Vector2(i, j);

					v[id4] = new Vector3(i, y, 0);
					c[id4] = new Color(1, 0, 0, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 0);
					c[id4] = new Color(1, 0, 0, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 32);
					c[id4] = new Color(1, 0, 0, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y, 32);
					c[id4] = new Color(1, 0, 0, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 31; i > -1; i--)
			{
				for (var j = 0; j < 4; j++)
				{
					var uvC = new Vector2(i, j + 4);
					var y = j * 32;
					var id4 = id * 4;

					v[id4] = new Vector3(0, y, i);
					c[id4] = new Color(0, 0, 1, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(0, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y, i);
					c[id4] = new Color(0, 0, 1, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 128; i > -1; i--)
			{
				var uvC = new Vector2((i - 1) & 31, ((i - 1) >> 5) + 8);
				var id4 = id * 4;
				v[id4] = new Vector3(0, i, 0);
				c[id4] = new Color(0, 1, 0, 0);
				uv[id4++] = uvC;

				v[id4] = new Vector3(0, i, 32);
				c[id4] = new Color(0, 1, 0, 0.25f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 32);
				c[id4] = new Color(0, 1, 0, 0.75f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 0);
				c[id4] = new Color(0, 1, 0, 1);
				uv[id4] = uvC;

				id4 -= 3;
				var id6 = id * 6;

				t[id6++] = id4;
				t[id6++] = id4 + 1;
				t[id6++] = id4 + 2;

				t[id6++] = id4;
				t[id6++] = id4 + 2;
				t[id6] = id4 + 3;
				id++;
			}

			m.vertices = v;
			m.uv = uv;
			m.colors = c;
			m.triangles = t;

			return m;
		}

		private Mesh CreateTypeEP()
		{
			var m = new Mesh();
			var q = 381;
			var v = new Vector3[q * 4];
			var c = new Color[q * 4];
			var uv = new Vector2[q * 4];
			var t = new int[q * 6];
			var id = 0;
			for (var i = 31; i > 0; i--)
			{
				for (var j = 0; j < 4; j++)
				{
					var y = j * 32;
					var id4 = id * 4;
					var uvC = new Vector2(i, j);

					v[id4] = new Vector3(i, y, 0);
					c[id4] = new Color(1, 0, 0, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 0);
					c[id4] = new Color(1, 0, 0, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 32);
					c[id4] = new Color(1, 0, 0, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y, 32);
					c[id4] = new Color(1, 0, 0, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 0; i < 32; i++)
			{
				for (var j = 0; j < 4; j++)
				{
					var uvC = new Vector2(i, j + 4);
					var y = j * 32;
					var id4 = id * 4;

					v[id4] = new Vector3(0, y, i);
					c[id4] = new Color(0, 0, 1, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(0, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y, i);
					c[id4] = new Color(0, 0, 1, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 128; i > -1; i--)
			{
				var uvC = new Vector2((i - 1) & 31, ((i - 1) >> 5) + 8);
				var id4 = id * 4;
				v[id4] = new Vector3(0, i, 0);
				c[id4] = new Color(0, 1, 0, 0);
				uv[id4++] = uvC;

				v[id4] = new Vector3(0, i, 32);
				c[id4] = new Color(0, 1, 0, 0.25f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 32);
				c[id4] = new Color(0, 1, 0, 0.75f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 0);
				c[id4] = new Color(0, 1, 0, 1);
				uv[id4] = uvC;

				id4 -= 3;
				var id6 = id * 6;

				t[id6++] = id4;
				t[id6++] = id4 + 1;
				t[id6++] = id4 + 2;

				t[id6++] = id4;
				t[id6++] = id4 + 2;
				t[id6] = id4 + 3;
				id++;
			}

			m.vertices = v;
			m.uv = uv;
			m.colors = c;
			m.triangles = t;

			return m;
		}

		private Mesh CreateTypePE()
		{
			var m = new Mesh();
			var q = 381;
			var v = new Vector3[q * 4];
			var c = new Color[q * 4];
			var uv = new Vector2[q * 4];
			var t = new int[q * 6];
			var id = 0;
			for (var i = 0; i < 32; i++)
			{
				for (var j = 0; j < 4; j++)
				{
					var y = j * 32;
					var id4 = id * 4;
					var uvC = new Vector2(i, j);

					v[id4] = new Vector3(i, y, 0);
					c[id4] = new Color(1, 0, 0, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 0);
					c[id4] = new Color(1, 0, 0, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 32);
					c[id4] = new Color(1, 0, 0, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y, 32);
					c[id4] = new Color(1, 0, 0, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 31; i > 0; i--)
			{
				for (var j = 0; j < 4; j++)
				{
					var uvC = new Vector2(i, j + 4);
					var y = j * 32;
					var id4 = id * 4;

					v[id4] = new Vector3(0, y, i);
					c[id4] = new Color(0, 0, 1, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(0, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y, i);
					c[id4] = new Color(0, 0, 1, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 128; i > -1; i--)
			{
				var uvC = new Vector2((i - 1) & 31, ((i - 1) >> 5) + 8);
				var id4 = id * 4;
				v[id4] = new Vector3(0, i, 0);
				c[id4] = new Color(0, 1, 0, 0);
				uv[id4++] = uvC;

				v[id4] = new Vector3(0, i, 32);
				c[id4] = new Color(0, 1, 0, 0.25f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 32);
				c[id4] = new Color(0, 1, 0, 0.75f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 0);
				c[id4] = new Color(0, 1, 0, 1);
				uv[id4] = uvC;

				id4 -= 3;
				var id6 = id * 6;

				t[id6++] = id4;
				t[id6++] = id4 + 1;
				t[id6++] = id4 + 2;

				t[id6++] = id4;
				t[id6++] = id4 + 2;
				t[id6] = id4 + 3;
				id++;
			}

			m.vertices = v;
			m.uv = uv;
			m.colors = c;
			m.triangles = t;

			return m;
		}

		private Mesh CreateTypeEM()
		{
			var m = new Mesh();
			var q = 381;
			var v = new Vector3[q * 4];
			var c = new Color[q * 4];
			var uv = new Vector2[q * 4];
			var t = new int[q * 6];
			var id = 0;
			for (var i = 31; i > 0; i--)
			{
				for (var j = 0; j < 4; j++)
				{
					var y = j * 32;
					var id4 = id * 4;
					var uvC = new Vector2(i, j);

					v[id4] = new Vector3(i, y, 0);
					c[id4] = new Color(1, 0, 0, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 0);
					c[id4] = new Color(1, 0, 0, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 32);
					c[id4] = new Color(1, 0, 0, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y, 32);
					c[id4] = new Color(1, 0, 0, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 31; i > -1; i--)
			{
				for (var j = 0; j < 4; j++)
				{
					var uvC = new Vector2(i, j + 4);
					var y = j * 32;
					var id4 = id * 4;

					v[id4] = new Vector3(0, y, i);
					c[id4] = new Color(0, 0, 1, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(0, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y, i);
					c[id4] = new Color(0, 0, 1, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 128; i > -1; i--)
			{
				var uvC = new Vector2((i - 1) & 31, ((i - 1) >> 5) + 8);
				var id4 = id * 4;
				v[id4] = new Vector3(0, i, 0);
				c[id4] = new Color(0, 1, 0, 0);
				uv[id4++] = uvC;

				v[id4] = new Vector3(0, i, 32);
				c[id4] = new Color(0, 1, 0, 0.25f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 32);
				c[id4] = new Color(0, 1, 0, 0.75f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 0);
				c[id4] = new Color(0, 1, 0, 1);
				uv[id4] = uvC;

				id4 -= 3;
				var id6 = id * 6;

				t[id6++] = id4;
				t[id6++] = id4 + 1;
				t[id6++] = id4 + 2;

				t[id6++] = id4;
				t[id6++] = id4 + 2;
				t[id6] = id4 + 3;
				id++;
			}

			m.vertices = v;
			m.uv = uv;
			m.colors = c;
			m.triangles = t;

			return m;
		}

		private Mesh CreateTypeME()
		{
			var m = new Mesh();
			var q = 381;
			var v = new Vector3[q * 4];
			var c = new Color[q * 4];
			var uv = new Vector2[q * 4];
			var t = new int[q * 6];
			var id = 0;
			for (var i = 31; i > -1; i--)
			{
				for (var j = 0; j < 4; j++)
				{
					var y = j * 32;
					var id4 = id * 4;
					var uvC = new Vector2(i, j);

					v[id4] = new Vector3(i, y, 0);
					c[id4] = new Color(1, 0, 0, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 0);
					c[id4] = new Color(1, 0, 0, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 32);
					c[id4] = new Color(1, 0, 0, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y, 32);
					c[id4] = new Color(1, 0, 0, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 31; i > 0; i--)
			{
				for (var j = 0; j < 4; j++)
				{
					var uvC = new Vector2(i, j + 4);
					var y = j * 32;
					var id4 = id * 4;

					v[id4] = new Vector3(0, y, i);
					c[id4] = new Color(0, 0, 1, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(0, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y, i);
					c[id4] = new Color(0, 0, 1, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 128; i > -1; i--)
			{
				var uvC = new Vector2((i - 1) & 31, ((i - 1) >> 5) + 8);
				var id4 = id * 4;
				v[id4] = new Vector3(0, i, 0);
				c[id4] = new Color(0, 1, 0, 0);
				uv[id4++] = uvC;

				v[id4] = new Vector3(0, i, 32);
				c[id4] = new Color(0, 1, 0, 0.25f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 32);
				c[id4] = new Color(0, 1, 0, 0.75f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 0);
				c[id4] = new Color(0, 1, 0, 1);
				uv[id4] = uvC;

				id4 -= 3;
				var id6 = id * 6;

				t[id6++] = id4;
				t[id6++] = id4 + 1;
				t[id6++] = id4 + 2;

				t[id6++] = id4;
				t[id6++] = id4 + 2;
				t[id6] = id4 + 3;
				id++;
			}

			m.vertices = v;
			m.uv = uv;
			m.colors = c;
			m.triangles = t;

			return m;
		}

		private Mesh CreateTypeEE()
		{
			var m = new Mesh();
			var q = 377;
			var v = new Vector3[q * 4];
			var c = new Color[q * 4];
			var uv = new Vector2[q * 4];
			var t = new int[q * 6];
			var id = 0;
			for (var i = 31; i > 0; i--)
			{
				for (var j = 0; j < 4; j++)
				{
					var y = j * 32;
					var id4 = id * 4;
					var uvC = new Vector2(i, j);

					v[id4] = new Vector3(i, y, 0);
					c[id4] = new Color(1, 0, 0, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 0);
					c[id4] = new Color(1, 0, 0, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y + 32, 32);
					c[id4] = new Color(1, 0, 0, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(i, y, 32);
					c[id4] = new Color(1, 0, 0, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 31; i > 0; i--)
			{
				for (var j = 0; j < 4; j++)
				{
					var uvC = new Vector2(i, j + 4);
					var y = j * 32;
					var id4 = id * 4;

					v[id4] = new Vector3(0, y, i);
					c[id4] = new Color(0, 0, 1, 0);
					uv[id4++] = uvC;

					v[id4] = new Vector3(0, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.25f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y + 32, i);
					c[id4] = new Color(0, 0, 1, 0.75f);
					uv[id4++] = uvC;

					v[id4] = new Vector3(32, y, i);
					c[id4] = new Color(0, 0, 1, 1);
					uv[id4] = uvC;

					id4 -= 3;
					var id6 = id * 6;

					t[id6++] = id4;
					t[id6++] = id4 + 1;
					t[id6++] = id4 + 2;

					t[id6++] = id4;
					t[id6++] = id4 + 2;
					t[id6] = id4 + 3;
					id++;
				}
			}
			for (var i = 128; i > -1; i--)
			{
				var uvC = new Vector2((i - 1) & 31, ((i - 1) >> 5) + 8);
				var id4 = id * 4;
				v[id4] = new Vector3(0, i, 0);
				c[id4] = new Color(0, 1, 0, 0);
				uv[id4++] = uvC;

				v[id4] = new Vector3(0, i, 32);
				c[id4] = new Color(0, 1, 0, 0.25f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 32);
				c[id4] = new Color(0, 1, 0, 0.75f);
				uv[id4++] = uvC;

				v[id4] = new Vector3(32, i, 0);
				c[id4] = new Color(0, 1, 0, 1);
				uv[id4] = uvC;

				id4 -= 3;
				var id6 = id * 6;

				t[id6++] = id4;
				t[id6++] = id4 + 1;
				t[id6++] = id4 + 2;

				t[id6++] = id4;
				t[id6++] = id4 + 2;
				t[id6] = id4 + 3;
				id++;
			}

			m.vertices = v;
			m.uv = uv;
			m.colors = c;
			m.triangles = t;

			return m;
		}
	}
}