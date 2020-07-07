using UnityEngine;

namespace AtomosZ.Tutorials.Planets
{
	public class RigidNoiseFilter : INoiseFilter
	{
		NoiseSettings.RigidNoiseSettings settings;
		Noise noise = new Noise();


		public RigidNoiseFilter(NoiseSettings.RigidNoiseSettings settings)
		{
			this.settings = settings;
		}

		public float Evaluate(Vector3 point)
		{
			float noiseValue = 0;
			float freq = settings.baseRoughness;
			float amp = 1;
			float weight = 1;

			for (int i = 0; i < settings.numLayers; ++i)
			{
				float v = 1 - Mathf.Abs(noise.Evaluate(point * freq + settings.center));
				v *= v;
				v *= weight;
				weight = Mathf.Clamp01(v * settings.weightMultiplier);

				noiseValue += v * amp;
				freq *= settings.roughness;
				amp *= settings.persistance;
			}

			noiseValue = noiseValue - settings.minValue;
			return noiseValue * settings.strength;
		}
	}
}