using UnityEngine;

namespace AtomosZ.Voronoi
{
	[System.Serializable]
	public class VoronoiTextureData
	{
		public Color[] baseColors;
		[Range(0, 1)]
		public float[] baseStartHeights;

		float savedMinHeight;
		float savedMaxHeight;


		public void ApplyToMaterial(Material material)
		{
			material.SetInt("layerCount", baseColors.Length);
			material.SetColorArray("baseColors", baseColors);
			material.SetFloatArray("baseStartHeights", baseStartHeights);
		}

		public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
		{
			savedMinHeight = minHeight;
			savedMaxHeight = maxHeight;
			material.SetFloat("minHeight", minHeight);
			material.SetFloat("maxHeight", maxHeight);
		}
	}
}