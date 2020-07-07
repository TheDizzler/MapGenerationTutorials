using UnityEngine;

namespace AtomosZ.Tutorials.FlatWorld
{
	[CreateAssetMenu(menuName = "Terrain/FlatWorld/HeightMapSettingsData")]
	public class HeightMapSettings : ScriptableObject
	{
		public NoiseSettings noiseSettings;

		[Min(1)]
		public float heightMultiplier;
		public AnimationCurve heightCurve;

		public bool useFalloff;
		public AnimationCurve falloffCurve;


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