using UdonSharp;
using UnityEngine;

public class RecursionSpeedTest : UdonSharpBehaviour
{
	public int size = 32;

	private byte[] voxels;

	private int[] stackX;
	private int[] stackY;
	private int[] stackZ;

	void Start()
	{
		int volume = size * size * size;

		voxels = new byte[volume];

		stackX = new int[volume * 6];
		stackY = new int[volume * 6];
		stackZ = new int[volume * 6];

		Test();
	}

	public void Test()
	{
		for (int i = 0; i < voxels.Length; i++)
			voxels[i] = 0;

		int stackCount = 0;

		float startTime = Time.realtimeSinceStartup;

		stackX[0] = size / 2;
		stackY[0] = size / 2;
		stackZ[0] = size / 2;
		stackCount = 1;

		int filled = 0;

		while (stackCount > 0)
		{
			stackCount--;

			int x = stackX[stackCount];
			int y = stackY[stackCount];
			int z = stackZ[stackCount];

			if (x < 0 || y < 0 || z < 0 ||
				x >= size || y >= size || z >= size)
				continue;

			int index = x + y * size + z * size * size;

			if (voxels[index] != 0)
				continue;

			voxels[index] = 1;
			filled++;

			stackX[stackCount] = x + 1;
			stackY[stackCount] = y;
			stackZ[stackCount] = z;
			stackCount++;

			stackX[stackCount] = x - 1;
			stackY[stackCount] = y;
			stackZ[stackCount] = z;
			stackCount++;

			stackX[stackCount] = x;
			stackY[stackCount] = y + 1;
			stackZ[stackCount] = z;
			stackCount++;

			stackX[stackCount] = x;
			stackY[stackCount] = y - 1;
			stackZ[stackCount] = z;
			stackCount++;

			stackX[stackCount] = x;
			stackY[stackCount] = y;
			stackZ[stackCount] = z + 1;
			stackCount++;

			stackX[stackCount] = x;
			stackY[stackCount] = y;
			stackZ[stackCount] = z - 1;
			stackCount++;
		}

		float time = (Time.realtimeSinceStartup - startTime) * 1000f;

		Debug.Log("Filled: " + filled);
		Debug.Log("Time: " + time + " ms");
	}
}