using System.Collections.Generic;
using UnityEngine;

namespace AtomosZ.Voronoi
{
	[System.Serializable]
	public class VoronoiTextureData
	{
		public const int MAX_LAYERS = 10;

		public List<ColorHeightMapData> colorHeightMaps;

		float savedMinHeight;
		float savedMaxHeight;


		public void ApplyToMaterial(Material material)
		{
			if (colorHeightMaps.Count == 0)
			{
				Debug.LogWarning("No color map set");
				return;
			}

			material.SetColorArray("baseColors", new Color[MAX_LAYERS]); // this is needed to prevent shader errors
			material.SetFloatArray("baseStartHeights", new float[MAX_LAYERS]);

			material.SetInt("layerCount", colorHeightMaps.Count);
			material.SetColorArray("baseColors", GetBaseColors());
			material.SetFloatArray("baseStartHeights", GetBaseStartHeights());
		}

		public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
		{
			if (colorHeightMaps.Count == 0)
			{
				return;
			}

			savedMinHeight = minHeight;
			savedMaxHeight = maxHeight;
			material.SetFloat("minHeight", minHeight);
			material.SetFloat("maxHeight", maxHeight);
		}

		private List<float> GetBaseStartHeights()
		{
			List<float> startHeights = new List<float>();
			foreach (var chm in colorHeightMaps)
				startHeights.Add(chm.baseStartHeight);
			return startHeights;
		}

		private List<Color> GetBaseColors()
		{
			List<Color> colors = new List<Color>();
			foreach (var chm in colorHeightMaps)
				colors.Add(chm.baseColor);
			return colors;
		}
	}

	[System.Serializable]
	public class ColorHeightMapData
	{
		public Color baseColor;
		[Range(0, 1)]
		public float baseStartHeight;
	}
}