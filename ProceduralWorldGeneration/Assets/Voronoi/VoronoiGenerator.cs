using System.Collections.Generic;
using AtomosZ.Voronoi;
using AtomosZ.Voronoi.Regions;
using UnityEngine;
using Random = System.Random;


namespace AtomosZ.Tutorials.Voronoi
{
	public class VoronoiGenerator : MonoBehaviour
	{
		public static Random rng;
		public static RectInt mapBounds;
		public static HashSet<Corner> uniqueCorners;
		public static int cornerCount = 0;

		public bool useSeed = true;
		public string randomSeed = "Seed";
		[Range(1, 1024)]
		public int mapWidth = 512;
		[Range(1, 1024)]
		public int mapHeight = 512;
		[Range(0, 256)]
		public int regionAmount = 50;
		[Range(.1f, .75f)]
		public float minSqrDistanceBetweenSites;

		public bool viewDelaunayCircles = false;
		public bool viewDelaunayTriangles = true;
		public bool viewVoronoiPolygons = true;

		public DelaunayGraph dGraph;
		public VoronoiGraph vGraph;
		public GameObject regionPrefab;
		public Transform regionHolder;
		public List<Region> regions;


		public void GenerateMap()
		{
			if (useSeed)
				rng = new Random(randomSeed.GetHashCode());
			else
				rng = new Random();

			mapBounds = new RectInt(0, 0, mapWidth, mapHeight);
			uniqueCorners = new HashSet<Corner>();
			cornerCount = 0;

			List<Vector2> sites = new List<Vector2>();
			// create corner sites
			sites.Add(new Vector2(mapBounds.xMin, mapBounds.yMin));
			sites.Add(new Vector2(mapBounds.xMax, mapBounds.yMin));
			sites.Add(new Vector2(mapBounds.xMax, mapBounds.yMax));
			sites.Add(new Vector2(mapBounds.xMin, mapBounds.yMax));

			int retryAttempts = 0;
			for (int i = 0; i < regionAmount; i++)
			{
				Vector2 site = new Vector2((float)(rng.NextDouble() * mapHeight), (float)(rng.NextDouble() * mapWidth));
				if (IsTooNear(sites, site))
				{   // try again

					++retryAttempts;
					if (retryAttempts > 25)
					{
						Debug.Log("Unable to place new site within exceptable distance parameters."
							+ " Aborting creating new sites. Created " + i + " out of " + regionAmount);
						break;
					}

					--i;
					Debug.Log("retry");
				}
				else
					sites.Add(site);
			}

			dGraph = new DelaunayGraph(sites);
			vGraph = new VoronoiGraph(dGraph);

			CreateRegions();
		}


		public void AddPoint()
		{
			if (dGraph == null)
			{
				Debug.LogWarning("No graph to add points to");
				return;
			}

			Vector2 site = new Vector2((float)(rng.NextDouble() * mapHeight), (float)(rng.NextDouble() * mapWidth));
			while (IsTooNear(dGraph.centroids, site))
			{
				site = new Vector2((float)(rng.NextDouble() * mapHeight), (float)(rng.NextDouble() * mapWidth));
			}

			dGraph.AddSite(site);
			Debug.Log(dGraph.triangles.Count + " triangles");
		}


		private void CreateRegions()
		{
			if (regions != null)
				foreach (var region in regions)
					DestroyImmediate(region.gameObject);

			regions = new List<Region>();

			foreach (var polygon in vGraph.polygons)
			{
				GameObject region = Instantiate(regionPrefab, regionHolder);
				regions.Add(region.GetComponent<Region>());
				region.GetComponent<Region>().CreateRegion(polygon);
			}
		}

		private bool IsTooNear(List<Centroid> sites, Vector2 site)
		{
			for (int i = 0; i < sites.Count; ++i)
			{
				Vector2 check = sites[i].position;
				if (check == site)
					continue;
				if ((check - site).sqrMagnitude < minSqrDistanceBetweenSites)
					return true;
			}

			return false;
		}

		private bool IsTooNear(List<Vector2> sites, Vector2 site)
		{
			for (int i = 0; i < sites.Count; ++i)
			{
				Vector2 check = sites[i];
				if (check == site)
					continue;
				if ((check - site).sqrMagnitude < minSqrDistanceBetweenSites)
					return true;
			}

			return false;
		}

		private void CheckDistanceAndEliminate(List<Vector2> centroids, float minSqrDistanceBetweenPoints)
		{
			List<Vector2> remove = new List<Vector2>();
			int amtChecked = 0;
			for (int i = 0; i < centroids.Count - 1; ++i)
			{
				Vector2 check = centroids[i];
				for (int j = i + 1; j < centroids.Count; ++j)
				{
					if ((check - centroids[j]).sqrMagnitude < minSqrDistanceBetweenPoints)
					{
						remove.Add(centroids[j]);
					}
					++amtChecked;
				}
			}

			Debug.Log("removing " + remove.Count + " centroids.");
			foreach (var rem in remove)
				centroids.Remove(rem);
		}

		private void OnDrawGizmos()
		{
			if (dGraph != null && viewDelaunayTriangles)
			{
				foreach (var triangle in dGraph.triangles)
				{
					Gizmos.color = Color.green;
					Gizmos.DrawLine(triangle.p1.position, triangle.p2.position);
					Gizmos.DrawLine(triangle.p2.position, triangle.p3.position);
					Gizmos.DrawLine(triangle.p3.position, triangle.p1.position);

					if (viewDelaunayCircles)
					{
						Gizmos.color = Color.blue;
						Gizmos.DrawWireSphere(triangle.center.position, triangle.radius);
					}
				}

				Gizmos.color = Color.white;
				foreach (var centroid in dGraph.centroids)
					Gizmos.DrawCube(centroid.position, Vector3.one * .125f);
			}

			if (vGraph != null && viewVoronoiPolygons)
			{
				// draw polygon corners
				Gizmos.color = Color.white;
				foreach (var corner in uniqueCorners)
					Gizmos.DrawSphere(corner.position, .125f);
			}

			// draw map bounds
			Gizmos.color = Color.black;
			Gizmos.DrawLine(new Vector2(mapBounds.xMin, mapBounds.yMin), new Vector2(mapBounds.xMin, mapBounds.yMax));
			Gizmos.DrawLine(new Vector2(mapBounds.xMin, mapBounds.yMax), new Vector2(mapBounds.xMax, mapBounds.yMax));
			Gizmos.DrawLine(new Vector2(mapBounds.xMax, mapBounds.yMax), new Vector2(mapBounds.xMax, mapBounds.yMin));
			Gizmos.DrawLine(new Vector2(mapBounds.xMax, mapBounds.yMin), new Vector2(mapBounds.xMin, mapBounds.yMin));
		}
	}
}