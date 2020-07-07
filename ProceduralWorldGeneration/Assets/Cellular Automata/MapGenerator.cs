using System.Collections.Generic;
using UnityEngine;

namespace AtomosZ.Tutorials.CellAuto
{
	public class MapGenerator : MonoBehaviour
	{
		public string seed;
		public bool useRandomSeed;
		public int width;
		public int height;
		[Tooltip("Minimum neighbours 4:\n\t40 to 45: Large caverns.\n\t45 to 50: caves." +
			"\n\t50 to 55: small caves & rooms.\n\t55 to 60: small rooms." +
			"\n\tValues below 30 are too open and above 60 are to filled.")]
		[Range(10, 100)]
		public int randomFillPercent;
		public int smoothSteps = 5;
		[Tooltip("A value of 4 is standard. A value of 5 with randomFillPercent" +
			"around 63 generates very eerie-looking platforms after 6 or 7 smooth steps. (try seed = Test Seed)" +
			"\nValues of 3 or 6 creates The Nothing.")]
		[Range(3, 6)]
		public int minNeighboursToSurvive = 4;
		[Tooltip("Minimum size of wall or pillar that can exist (will be filled in with empty space)")]
		public int wallThresholdSize = 15;
		[Tooltip("Minimum size of room that can exist (will be filled in with wall)")]
		public int roomThresholdSize = 50;
		public bool keepWalls;

		private int[,] map;


		private void Start()
		{
			GenerateMap();
		}

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.Return))
			{
				GenerateMap();
			}
		}


		public bool IsMapExist()
		{
			return map != null;
		}

		public void GenerateMap()
		{
			map = new int[width, height];
			RandomFillMap();

			for (int i = 0; i < smoothSteps; ++i)
			{
				if (!SmoothMap())
					break;
			}

			ProcessMap();

			MeshGenerator meshGen = GetComponent<MeshGenerator>();
			meshGen.GenerateMesh(map, 1);
		}


		/// <summary>
		/// Returns false when no changes found.
		/// </summary>
		/// <returns></returns>
		public bool SmoothMap(bool regenerateMeshImmediately = false)
		{
			int[,] last = (int[,])map.Clone();
			for (int x = keepWalls ? 1 : 0; x < (keepWalls ? width - 1 : width); ++x)
			{
				for (int y = keepWalls ? 1 : 0; y < (keepWalls ? height - 1 : height); ++y)
				{
					if (GetSurroundingWallCount(last, x, y) < minNeighboursToSurvive)
						map[x, y] = 0;
					else if (GetSurroundingWallCount(last, x, y) > minNeighboursToSurvive)
						map[x, y] = 1;
				}
			}



			for (int x = 0; x < width; ++x)
			{
				for (int y = 0; y < height; ++y)
				{
					if (last[x, y] != map[x, y])
					{
						if (regenerateMeshImmediately)
						{
							MeshGenerator meshGen = GetComponent<MeshGenerator>();
							meshGen.GenerateMesh(map, 1);
						}

						return true;
					}
				}
			}

			return false;
		}

		private void ProcessMap()
		{
			List<List<Coord>> wallRegions = GetRegions(1);

			foreach (List<Coord> wallRegion in wallRegions)
				if (wallRegion.Count < wallThresholdSize)
					foreach (Coord tile in wallRegion)
						map[tile.tileX, tile.tileY] = 0;

			List<List<Coord>> roomRegions = GetRegions(0);

			foreach (List<Coord> roomRegion in roomRegions)
				if (roomRegion.Count < roomThresholdSize)
					foreach (Coord tile in roomRegion)
						map[tile.tileX, tile.tileY] = 1;
		}


		private List<List<Coord>> GetRegions(int tileType)
		{
			List<List<Coord>> regions = new List<List<Coord>>();
			int[,] mapFlags = new int[width, height];
			for (int x = 0; x < width; ++x)
			{
				for (int y = 0; y < height; ++y)
				{
					if (mapFlags[x, y] == 0 && map[x, y] == tileType)
					{
						List<Coord> newRegion = GetRegionTiles(x, y);
						regions.Add(newRegion);
						foreach (Coord tile in newRegion)
							mapFlags[tile.tileX, tile.tileY] = 1;
					}
				}
			}

			return regions;
		}


		private List<Coord> GetRegionTiles(int startX, int startY)
		{
			List<Coord> tiles = new List<Coord>();
			int[,] mapFlags = new int[width, height];
			int tileType = map[startX, startY];

			Queue<Coord> queue = new Queue<Coord>();
			queue.Enqueue(new Coord(startX, startY));
			mapFlags[startX, startY] = 1;

			while (queue.Count > 0)
			{
				Coord tile = queue.Dequeue();
				tiles.Add(tile);
				for (int x = tile.tileX - 1; x <= tile.tileX + 1; ++x)
				{
					for (int y = tile.tileY - 1; y <= tile.tileY + 1; ++y)
					{
						if (IsInMapRange(x, y) && (y == tile.tileY || x == tile.tileX))
						{
							if (mapFlags[x, y] == 0 && map[x, y] == tileType)
							{
								mapFlags[x, y] = 1;
								queue.Enqueue(new Coord(x, y));
							}
						}
					}
				}
			}

			return tiles;
		}


		private int GetSurroundingWallCount(int[,] map, int checkX, int checkY)
		{
			int wallCount = 0;
			for (int x = checkX - 1; x <= checkX + 1; ++x)
			{
				for (int y = checkY - 1; y <= checkY + 1; ++y)
				{
					if (x < 0 || x == width || y < 0 || y == height
						|| (x == checkX && y == checkY))
						continue;
					if (map[x, y] == 1)
						++wallCount;
				}
			}

			return wallCount;
		}


		private bool IsInMapRange(int x, int y)
		{
			return x >= 0 && x < width && y >= 0 && y < height;
		}


		private void RandomFillMap()
		{
			string randomSeed;
			if (useRandomSeed)
				randomSeed = Time.time.ToString();
			else
				randomSeed = seed;

			System.Random rng = new System.Random(randomSeed.GetHashCode());

			for (int x = 0; x < width; ++x)
			{
				for (int y = 0; y < height; ++y)
				{
					if (y == 0 || x == 0 || y == height - 1 || x == width - 1)
						map[x, y] = 1;
					else
						map[x, y] = rng.Next(0, 100) > randomFillPercent ? 0 : 1;

				}
			}
		}


		struct Coord
		{
			public int tileX, tileY;

			public Coord(int tileX, int tileY)
			{
				this.tileX = tileX;
				this.tileY = tileY;
			}
		}


		//void OnDrawGizmos()
		//{
			//if (map != null)
			//{
			//	for (int x = 0; x < width; ++x)
			//	{
			//		for (int y = 0; y < height; ++y)
			//		{
			//			Gizmos.color = (map[x, y] == 1) ? Color.black : Color.white;
			//			Vector3 pos = new Vector3(-width / 2 + x + .5f, 0, -height / 2 + y + .5f);
			//			Gizmos.DrawCube(pos, Vector3.one);
			//		}
			//	}
			//}
		//}
	}
}