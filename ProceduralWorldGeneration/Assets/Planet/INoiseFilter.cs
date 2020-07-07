using UnityEngine;

namespace AtomosZ.Tutorials.Planets
{
	public interface INoiseFilter
	{
		float Evaluate(Vector3 point);
	}
}