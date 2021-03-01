using System.Collections.Generic;
using UnityEngine;

namespace AtomosZ.Voronoi.Regions
{
	public class BiomeSettings : MonoBehaviour
	{
		public static Color RiverColor = Color.blue;

		public enum Basic
		{
			/// <summary>
			/// Lake or Ocean
			/// </summary>
			Water,
			/// <summary>
			/// Beach, cliffs...
			/// </summary>
			Coast,
			Land
		}


		public enum ElevationZone
		{
			SeaLevel = 0,
			Low = 1,
			Medium = 2,
			High = 3,
		}

		public enum MoistureZone
		{
			NoMoisture = 0,
			VeryDry,
			Dry,
			Moist,
			ModeratlyWet,
			Wet = 5,
		}

		public enum BiomeType
		{
			// Water only
			DeepOcean,
			ShallowOcean,
			Lake,

			// Moisture level 0
			Scorched,
			DesertTemperate,
			DesertSubTropical,

			// Moisture level 1
			Bare,
			Grassland,

			// Moisture level 2
			Tundra,
			Shrubland,
			ForestSeasonalTropical,

			// Moisture level 3
			Snow,
			ForestDeciduousTemperate,

			// Moisture level 4
			Taiga,
			ForestRainTropical,

			// Moisture level 5
			ForestRainTemperate,
		}

		public List<Color> biomeColors;
		public float[] elevationStartHeights = new float[]
		{
			0.5f, // Sea-level
			1,
			2,
			3
		};

		public bool isElevationFoldout;
		public BiomeDictionary biomeDictionary;

		[Range(0, 10)]
		public int riverSubdivisions;
		[Range(0, 1)]
		public float riverAmplitude;

		[System.Serializable]
		public class BiomeDictionary
		{
			public static string[] test = new string[] { "tr", "ere", "4" };
			/// <summary>
			/// [elevation][moisture level] == BiomeType
			/// </summary>
			public static int[][] biomeLookup = new int[][]
			{
				new int[] // SeaLevel
				{
					(int)BiomeType.DesertSubTropical,
					(int)BiomeType.Grassland,
					(int)BiomeType.ForestSeasonalTropical,
					(int)BiomeType.ForestSeasonalTropical,
					(int)BiomeType.ForestRainTropical,
					(int)BiomeType.ForestRainTropical,
				},
				new int[] // Low
				{
					(int)BiomeType.ForestRainTemperate,
					(int)BiomeType.ForestDeciduousTemperate,
					(int)BiomeType.ForestDeciduousTemperate,
					(int)BiomeType.Grassland,
					(int)BiomeType.Grassland,
					(int)BiomeType.DesertTemperate,
				},
				new int[] // Medium
				{
					(int)BiomeType.Taiga,
					(int)BiomeType.Taiga,
					(int)BiomeType.Shrubland,
					(int)BiomeType.Shrubland,
					(int)BiomeType.DesertTemperate,
					(int)BiomeType.DesertTemperate,
				},
				new int[] // High
				{
					(int)BiomeType.Snow,
					(int)BiomeType.Snow,
					(int)BiomeType.Snow,
					(int)BiomeType.Tundra,
					(int)BiomeType.Bare,
					(int)BiomeType.Scorched,
				},
			};
		}
	}
}