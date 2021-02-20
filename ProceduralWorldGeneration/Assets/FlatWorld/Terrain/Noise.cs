using UnityEngine;

namespace AtomosZ.Tutorials.FlatWorld
{
	public class Noise
	{
		public enum NormalizeMode { Local, Global }

		public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings noiseSettings, Vector2 sampleCenter)
		{
			System.Random rnd = new System.Random(noiseSettings.seed);

			float maxPossibleHeight = 0;
			float amplitude = 1;
			float frequency = 1;
			Vector2[] octaveOffsets = new Vector2[noiseSettings.numOctaves];

			for (int i = 0; i < noiseSettings.numOctaves; ++i)
			{
				float offsetX = rnd.Next(-100000, 100000) + noiseSettings.offset.x + sampleCenter.x;
				float offsetY = rnd.Next(-100000, 100000) - noiseSettings.offset.y - sampleCenter.y;
				octaveOffsets[i] = new Vector2(offsetX, offsetY);

				maxPossibleHeight += amplitude;
				amplitude *= noiseSettings.persistance;
			}

			float maxLocalNoiseHeight = float.MinValue;
			float minLocalNoiseHeight = float.MaxValue;

			float halfWidth = mapWidth * .5f;
			float halfHeight = mapHeight * .5f;


			float[,] noiseMap = new float[mapWidth, mapHeight];

			for (int y = 0; y < mapHeight; ++y)
			{
				for (int x = 0; x < mapWidth; ++x)
				{
					amplitude = 1;
					frequency = 1;
					float noiseHeight = 0;

					for (int i = 0; i < noiseSettings.numOctaves; ++i)
					{
						float sampleX = (x - halfWidth + octaveOffsets[i].x) / noiseSettings.scale * frequency;
						float sampleY = (y - halfHeight + octaveOffsets[i].y) / noiseSettings.scale * frequency;

						float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
						noiseHeight += perlinValue * amplitude;

						amplitude *= noiseSettings.persistance;
						frequency *= noiseSettings.lacunarity;
					}

					if (noiseHeight > maxLocalNoiseHeight)
						maxLocalNoiseHeight = noiseHeight;
					if (noiseHeight < minLocalNoiseHeight)
						minLocalNoiseHeight = noiseHeight;
					noiseMap[x, y] = noiseHeight;

					if (noiseSettings.normalizeMode == NormalizeMode.Global)
					{
						float normalizedHeight = (noiseMap[x, y] + maxPossibleHeight) / (2f * maxPossibleHeight);
						noiseMap[x, y] = normalizedHeight;
						//float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f);
						//noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
					}
				}
			}

			if (noiseSettings.normalizeMode == NormalizeMode.Local)
			{
				for (int y = 0; y < mapHeight; ++y)
				{
					for (int x = 0; x < mapWidth; ++x)
					{
						noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
					}
				}
			}

			return noiseMap;
		}
	}

	[System.Serializable]
	public class NoiseSettings
	{
		public Noise.NormalizeMode normalizeMode;

		[Range(0.0015f, 100)]
		public float scale = 50;
		[Range(1, 29)]
		public int numOctaves = 6;
		[Range(0, 1)]
		public float persistance = .6f;
		[Min(1)]
		public float lacunarity = 2;

		public int seed;
		public Vector2 offset;
	}
}