using UnityEngine;

namespace AtomosZ.Voronoi
{
	[System.Serializable]
	public class VoronoiHeightMapSettings
	{
		[Min(1)]
		public float heightMultiplier;
		public AnimationCurve heightCurve;

		public bool useFalloff;
		public AnimationCurve falloffCurve;

		public VoronoiTextureData textureData;


		public float minHeight
		{
			get { return heightMultiplier * heightCurve.Evaluate(0); }
		}

		public float maxHeight
		{
			get { return heightMultiplier * heightCurve.Evaluate(1); }
		}
	}
}