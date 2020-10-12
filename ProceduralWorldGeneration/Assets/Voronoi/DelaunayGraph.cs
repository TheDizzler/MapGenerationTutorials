using System.Collections.Generic;
using UnityEngine;

namespace AtomosZ.Voronoi
{
	/// <summary>
	/// My implementation of a DelaunayGraph. Noticeable delays from about 120+ points.
	/// </summary>
	public class DelaunayGraph
	{
		public List<DelaunayTriangle> triangles;
		public List<DEdge> edges;
		public List<Centroid> centroids;


		public DelaunayGraph(List<Vector2> centroidPositions)
		{
			centroids = new List<Centroid>();
			foreach (var pos in centroidPositions)
				centroids.Add(new Centroid(pos));

			triangles = new List<DelaunayTriangle>();
			edges = new List<DEdge>();

			CalculateTriangulations(centroids);
		}


		/// <summary>
		/// TODO: edges are not being added properly.
		/// </summary>
		/// <param name="sitePosition"></param>
		public void AddSite(Vector2 sitePosition)
		{
			Centroid site = new Centroid(sitePosition);
			centroids.Add(site);
			List<DelaunayTriangle> remove = new List<DelaunayTriangle>();
			List<Centroid> needsRetriangulation = new List<Centroid>();
			foreach (var triangle in triangles)
			{
				if ((site.position - triangle.realCenter).sqrMagnitude <= triangle.radiusSqr)
				{
					// this triangle is invalid
					remove.Add(triangle);
					if (!needsRetriangulation.Contains(triangle.p1))
						needsRetriangulation.Add(triangle.p1);
					if (!needsRetriangulation.Contains(triangle.p2))
						needsRetriangulation.Add(triangle.p2);
					if (!needsRetriangulation.Contains(triangle.p3))
						needsRetriangulation.Add(triangle.p3);
				}
			}

			if (remove.Count > 0)
			{
				foreach (var rem in remove)
				{
					triangles.Remove(rem);
					rem.edges.Clear();
				}


				foreach (var retri in needsRetriangulation)
				{
					foreach (var edge in retri.connectedEdges)
					{
						if (needsRetriangulation.Contains(edge.start) && needsRetriangulation.Contains(edge.end))
							edges.Remove(edge); // this still removes edges that shouldn't be removed
					}
				}

				needsRetriangulation.Add(site);
			}
			else
			{
				needsRetriangulation = centroids;
				triangles.Clear();
			}

			CalculateTriangulations(needsRetriangulation);
		}


		private void CalculateTriangulations(List<Centroid> sites)
		{
			for (int i = 0; i < sites.Count - 2; ++i)
			{
				Centroid p1 = sites[i];
				for (int j = i + 1; j < sites.Count - 1; ++j)
				{
					Centroid p2 = sites[j];
					for (int k = j + 1; k < sites.Count; ++k)
					{
						Centroid p3 = sites[k];
						DelaunayTriangle circle = GetCircle(p1, p2, p3);

						if (circle == null)
							continue;
						foreach (var centroid in centroids)
						{
							if (centroid == p1 || centroid == p2 || centroid == p3)
								continue;
							if ((centroid.position - circle.realCenter).sqrMagnitude <= circle.radiusSqr)
							{
								// this triangle is invalid
								circle.Destroy();
								circle = null;
								break;
							}
						}


						if (circle != null)
						{
							triangles.Add(circle);
							edges.AddRange(circle.CalculateEdges());
						}
					}
				}
			}
		}


		private DelaunayTriangle GetCircle(Centroid site1, Centroid site2, Centroid site3)
		{
			Vector2 p1 = site1.position;
			Vector2 p2 = site2.position;
			Vector2 p3 = site3.position;

			Vector2 d1 = new Vector2(p2.y - p1.y, p1.x - p2.x);
			Vector2 d2 = new Vector2(p3.y - p1.y, p1.x - p3.x);
			float k = d2.x * d1.y - d2.y * d1.x;
			if (k > 0.0001f && k < 0.0001f)
			{
				Debug.Log("K is too small. Invalidate this triangulation?");
				return null;
			}

			Vector2 s1 = new Vector2(p1.x + p2.x, p1.y + p2.y) * .5f;
			Vector2 s2 = new Vector2(p1.x + p3.x, p1.y + p3.y) * .5f;
			float l = d1.x * (s2.y - s1.y) - d1.y * (s2.x - s1.x);
			float m = l / k;
			Vector2 center = new Vector2(s2.x + m * d2.x, s2.y + m * d2.y);
			float dx = center.x - p1.x;
			float dy = center.y - p1.y;
			float radius = Mathf.Sqrt(dx * dx + dy * dy);

			return new DelaunayTriangle(site1, site2, site3, center, radius);
		}
	}
}