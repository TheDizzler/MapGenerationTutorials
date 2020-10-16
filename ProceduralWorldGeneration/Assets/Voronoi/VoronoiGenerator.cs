using System.Collections.Generic;
using AtomosZ.Voronoi;
using AtomosZ.Voronoi.Regions;
using UnityEngine;
using Random = System.Random;


namespace AtomosZ.Tutorials.Voronoi
{
	public class VoronoiGenerator : MonoBehaviour
	{
		public const byte TopRight = 3;
		public const byte BottomRight = 6;
		public const byte TopLeft = 9;
		public const byte BottomLeft = 12;

		public enum MapSide
		{
			Inside = 0, // 0000
			Top = 1,    // 0001
			Right = 2,  // 0010
			Bottom = 4, // 0100
			Left = 8    // 1000
		};

		public static Random rng;
		public static Rect mapBounds;
		public static bool fixCorners;

		private static float mapBoundsTolerance = .0001f;


		public bool useRandomSeed = true;
		public string randomSeed = "Seed";

		[Range(1, 1024)]
		public int mapWidth = 512;
		[Range(1, 1024)]
		public int mapHeight = 512;
		[Range(0, 256)]
		public int regionAmount = 50;
		[Range(.5f, 5.00f)]
		public float minSqrDistanceBetweenSites;

		public bool viewDelaunayCircles = false;
		public bool viewDelaunayTriangles = true;
		public bool viewCenteroids = true;
		public bool viewVoronoiPolygons = true;

		public DelaunayGraph dGraph;
		public VoronoiGraph vGraph;
		public GameObject regionPrefab;
		public Transform regionHolder;
		public List<Region> regions;

		public bool fixOOBCorners = false;

		private static Vector2 topLeft;
		private static Vector2 topRight;
		private static Vector2 bottomRight;
		private static Vector2 bottomLeft;


		public void GenerateMap()
		{
			fixCorners = fixOOBCorners;
			if (useRandomSeed)
				randomSeed = System.DateTime.Now.Ticks.ToString();
			rng = new Random(randomSeed.GetHashCode());

			mapBounds = new Rect(0, 0, mapWidth, mapHeight);

			topLeft = new Vector2(mapBounds.xMin, mapBounds.yMax);
			topRight = new Vector2(mapBounds.xMax, mapBounds.yMax);
			bottomRight = new Vector2(mapBounds.xMax, mapBounds.yMin);
			bottomLeft = new Vector2(mapBounds.xMin, mapBounds.yMin);
			List<Vector2> sites = new List<Vector2>();
			// create corner sites

			sites.Add(topLeft);
			sites.Add(topRight);
			sites.Add(bottomRight);
			sites.Add(bottomLeft);

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

		
		public static bool IsInMapBounds(Vector2 position)
		{
			if (mapBounds.Contains(position))
				return true; // no argument that it is within bounds

			// check if close enough
			if (position.x + mapBoundsTolerance > mapBounds.xMin
				&& position.y + mapBoundsTolerance > mapBounds.yMin
				&& position.x - mapBoundsTolerance < mapBounds.xMax
				&& position.y - mapBoundsTolerance < mapBounds.yMax)
				return true;

			return false;
		}

		public static List<Vector2> FindMapBoundsIntersection(Vector2 lineStart, Vector2 lineEnd)
		{
			List<Vector2> intersections = new List<Vector2>();
			if (TryGetLineIntersection(topLeft, bottomLeft, lineStart, lineEnd, out Vector2 leftSide))
				intersections.Add(leftSide);
			if (TryGetLineIntersection(topRight, topLeft, lineStart, lineEnd, out Vector2 topSide))
				intersections.Add(topSide);
			if (TryGetLineIntersection(bottomRight, topRight, lineStart, lineEnd, out Vector2 rightSide))
				intersections.Add(rightSide);
			if (TryGetLineIntersection(bottomLeft, bottomRight, lineStart, lineEnd, out Vector2 bottomSide))
				intersections.Add(bottomSide);
			return intersections;
		}

		public static bool TryGetFirstMapBoundsIntersection(Vector2 lineStart, Vector2 lineEnd, out Vector2 intersectPoint)
		{
			if (TryGetLineIntersection(topLeft, bottomLeft, lineStart, lineEnd, out intersectPoint))
				return true;
			if (TryGetLineIntersection(topRight, topLeft, lineStart, lineEnd, out intersectPoint))
				return true;
			if (TryGetLineIntersection(bottomRight, topRight, lineStart, lineEnd, out intersectPoint))
				return true;
			if (TryGetLineIntersection(bottomLeft, bottomRight, lineStart, lineEnd, out intersectPoint))
				return true;
			return false;
		}


		public static bool TryGetBoundsIntersection(Vector2 lineStart, Vector2 lineEnd, out Dictionary<MapSide, Vector2> intersections, out byte corner)
		{
			intersections = new Dictionary<MapSide, Vector2>();
			corner = (int)MapSide.Inside;

			if (TryGetLineIntersection(topRight, topLeft, lineStart, lineEnd, out Vector2 top))
			{
				intersections.Add(MapSide.Top, top);
				corner |= (int)MapSide.Top;
			}

			if (TryGetLineIntersection(bottomLeft, bottomRight, lineStart, lineEnd, out Vector2 bottom))
			{
				intersections.Add(MapSide.Bottom, bottom);
				corner |= (int)MapSide.Bottom;
			}

			if (TryGetLineIntersection(topLeft, bottomLeft, lineStart, lineEnd, out Vector2 left))
			{
				intersections.Add(MapSide.Left, left);
				corner |= (int)MapSide.Left;
			}

			if (TryGetLineIntersection(bottomRight, topRight, lineStart, lineEnd, out Vector2 right))
			{
				intersections.Add(MapSide.Right, right);
				corner |= (int)MapSide.Right;
			}

			return intersections.Count > 0;
		}


		/// <summary>
		/// Algo modified from http://csharphelper.com/blog/2014/08/determine-where-two-lines-intersect-in-c/.
		/// </summary>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <param name="p3"></param>
		/// <param name="p4"></param>
		/// <param name="intersectPoint"></param>
		/// <returns></returns>
		private static bool TryGetLineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersectPoint)
		{
			// Get the segments' parameters.
			float dx12 = p1.x - p2.x;
			float dy12 = p1.y - p2.y;
			float dx34 = p4.x - p3.x;
			float dy34 = p4.y - p3.y;

			float denominator = (dy12 * dx34 - dx12 * dy34);
			float t1 = ((p1.x - p3.x) * dy34 + (p3.y - p1.y) * dx34) / denominator;

			if (float.IsInfinity(t1))
			{
				// The lines are parallel (or close enough to it).
				intersectPoint = Vector2.positiveInfinity;
				return false;
			}

			float t2 = ((p3.x - p1.x) * dy12 + (p1.y - p3.y) * dx12) / -denominator;


			if ((t2 > 0) && (t2 < 1))
			{
				// Find the point of intersection.
				intersectPoint = new Vector2(p1.x + dx12 * t1, p1.y + dy12 * t1); // this is intersect point on line 1
				return true;
			}

			// if (t1 > 0) && (t1 < 1)))
			//{ // this is intersect point on line 2.
			//	// The result is the same Vector as line 1 but the value of t2 may be useful in the future.
			//	intersectPoint = new Vector2(p3.x + dx34 * t2, p3.y + dy34 * t2); 
			//	return true;
			//}

			// segments do not intersect
			intersectPoint = Vector2.positiveInfinity;
			return false;
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
			if (dGraph != null)
			{
				if (viewDelaunayTriangles)
				{
					foreach (var triangle in dGraph.triangles)
					{
						if (triangle.isInvalidated)
							continue;
						Gizmos.color = Color.green;
						Gizmos.DrawLine(triangle.p1.position, triangle.p2.position);
						Gizmos.DrawLine(triangle.p2.position, triangle.p3.position);
						Gizmos.DrawLine(triangle.p3.position, triangle.p1.position);

						if (viewDelaunayCircles)
						{
							Gizmos.color = Color.blue;
							Gizmos.DrawWireSphere(triangle.realCenter, triangle.radius);
						}
					}
				}
				else if (viewDelaunayCircles)
				{
					foreach (var triangle in dGraph.triangles)
					{
						Gizmos.color = Color.blue;
						Gizmos.DrawWireSphere(triangle.realCenter, triangle.radius);
					}
				}

				if (viewCenteroids)
				{
					Gizmos.color = Color.white;
					foreach (var centroid in dGraph.centroids)
						Gizmos.DrawSphere(centroid.position, .25f);
				}
			}

			if (vGraph != null && viewVoronoiPolygons)
			{
				// draw polygon corners

			}

			// draw map bounds
			Gizmos.color = Color.black;
			Gizmos.DrawLine(topLeft, topRight);
			Gizmos.DrawLine(topRight, bottomRight);
			Gizmos.DrawLine(bottomRight, bottomLeft);
			Gizmos.DrawLine(bottomLeft, topLeft);

		}
	}
}