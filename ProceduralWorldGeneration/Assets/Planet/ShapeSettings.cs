using UnityEngine;

namespace AtomosZ.Tutorials.Planets
{
	[CreateAssetMenu(menuName = "Terrain/Planet/ShapeSettings")]
	public class ShapeSettings : ScriptableObject
	{
		[Min(.001f)]
		public float planetRadius = 1;
		public NoiseLayer[] noiseLayers;

		[System.Serializable]
		public class NoiseLayer
		{
			public bool isEnabled = true;
			public bool useFirstLayerAsMask;
			public NoiseSettings noiseSettings;
		}
	}
}