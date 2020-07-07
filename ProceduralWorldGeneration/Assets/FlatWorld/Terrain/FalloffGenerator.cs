using UnityEngine;

namespace AtomosZ.Tutorials.FlatWorld
{
	public static class FalloffGenerator
	{
		public static float[,] GenerateFalloffMap(int mapSize, AnimationCurve falloffCurve)
		{
			AnimationCurve falloffCurve_threadsafe = new AnimationCurve(falloffCurve.keys);
			float[,] map = new float[mapSize, mapSize];
			for (int i = 0; i < mapSize; ++i)
			{
				for (int j = 0; j < mapSize; ++j)
				{
					float x = i / (float)mapSize * 2 - 1;
					float y = j / (float)mapSize * 2 - 1;
					float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
					map[i, j] = falloffCurve_threadsafe.Evaluate(value);
				}
			}

			return map;
		}
	}
}