using UnityEngine;

namespace AtomosZ.Tutorials.FlatWorld
{
	[CreateAssetMenu(menuName = "Terrain/FlatWorld/MeshSettings")]
	public class MeshSettings : ScriptableObject
	{
		public const int NumSupportedLODs = 5;
		public const int NumSupportedChunkSizes = 9;
		public const int NumSupportedFlatshadedChunkSizes = 3;

		public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

		public float meshScale = 1f;
		public bool useFlatShading = false;

		[Range(0, NumSupportedChunkSizes - 1)]
		public int chunkSizeIndex;

		[Range(0, NumSupportedFlatshadedChunkSizes - 1)]
		public int flatshadedChunkSizeIndex;

		/// <summary>
		/// Num verts per line of mesh rendered at LOD = 0. 
		/// Includes the 2 extra verts that excluded from final mesh but used for calculating normals.
		/// Must be divisble by all possible MeshGenerator.meshSimplificationIncrement
		/// AND must be below 240 or 96 if using flat shading.
		/// </summary>
		public int NumVertsPerLine
		{
			get
			{
				if (useFlatShading)
					return supportedChunkSizes[flatshadedChunkSizeIndex] + 5;
				return supportedChunkSizes[chunkSizeIndex] + 5;
			}
		}

		public float meshWorldSize
		{
			get { return (NumVertsPerLine - 3) * meshScale; }
		}
	}
}