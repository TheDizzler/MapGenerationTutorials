using UnityEngine;

namespace AtomosZ.Tutorials.Planets
{
	public class SimpleNoiseFilter : INoiseFilter
	{
		NoiseSettings.SimpleNoiseSettings settings;
		Noise noise = new Noise();


		public SimpleNoiseFilter(NoiseSettings.SimpleNoiseSettings settings)
		{
			this.settings = settings;
		}

		public float Evaluate(Vector3 point)
		{
			float noiseValue = 0;
			float freq = settings.baseRoughness;
			float amp = 1;

			for (int i = 0; i < settings.numLayers; ++i)
			{
				float v = noise.Evaluate(point * freq + settings.center);
				noiseValue += (v + 1) * .5f * amp;
				freq *= settings.roughness;
				amp *= settings.persistance;
			}

			noiseValue = noiseValue - settings.minValue;
			return noiseValue * settings.strength;
		}
	}
}