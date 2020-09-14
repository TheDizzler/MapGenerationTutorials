using System.Collections.Generic;
using AtomosZ.Voronoi;
using UnityEditor;
using UnityEngine;
using Random = System.Random;


namespace AtomosZ.Tutorials.Voronoi
{
	public class VoronoiGenerator : MonoBehaviour
	{
		public static Random rng;

		public bool useSeed = true;
		public string randomSeed = "Seed";
		[Range(1, 1024)]
		public int mapWidth = 512;
		[Range(1, 1024)]
		public int mapHeight = 512;
		[Range(1, 256)]
		public int regionAmount = 50;
		[Range(.1f, .75f)]
		public float minSqrDistanceBetweenSites;

		public bool viewDelaunayTriangulation = false;

		public DelaunayGraph graph;


		public void GenerateMap()
		{
			if (useSeed)
				rng = new Random(randomSeed.GetHashCode());
			else
				rng = new Random();

			List<Vector2> sites = new List<Vector2>();
			for (int i = 0; i < regionAmount; i++)
			{
				Vector2 site = new Vector2((float)(rng.NextDouble() * mapHeight), (float)(rng.NextDouble() * mapWidth));
				if (IsTooNear(sites, site))
				{
					--i; // try again
					Debug.Log("retry");
				}
				else
					sites.Add(site);
			}

			graph = new DelaunayGraph(sites);
			Debug.Log(graph.triangles.Count + " triangles");
		}


		public void AddPoint()
		{
			if (graph == null)
			{
				Debug.LogWarning("No graph to add points to");
				return;
			}

			Vector2 site = new Vector2((float)(rng.NextDouble() * mapHeight), (float)(rng.NextDouble() * mapWidth));
			while (IsTooNear(graph.centroids, site))
			{
				site = new Vector2((float)(rng.NextDouble() * mapHeight), (float)(rng.NextDouble() * mapWidth));
			}

			graph.AddSite(site);
			Debug.Log(graph.triangles.Count + " triangles");
		}

		private bool IsTooNear(List<Site> sites, Vector2 site)
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
			if (graph != null)
			{
				foreach (var triangle in graph.triangles)
				{
					Gizmos.color = Color.green;
					Gizmos.DrawLine(triangle.p1.position, triangle.p2.position);
					Gizmos.DrawLine(triangle.p2.position, triangle.p3.position);
					Gizmos.DrawLine(triangle.p3.position, triangle.p1.position);

					if (viewDelaunayTriangulation)
					{
						Gizmos.color = Color.blue;
						Gizmos.DrawWireSphere(triangle.center, triangle.radius);
					}
				}

				Gizmos.color = Color.white;
				foreach (var centroid in graph.centroids)
					Gizmos.DrawCube(centroid.position, Vector3.one * .25f);
			}
		}
	}
}