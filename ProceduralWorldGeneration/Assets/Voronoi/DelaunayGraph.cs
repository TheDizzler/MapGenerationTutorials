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
		public List<Edge> edges;
		public List<Site> centroids;


		public DelaunayGraph(List<Vector2> centroidPositions)
		{
			centroids = new List<Site>();
			foreach (var pos in centroidPositions)
				centroids.Add(new Site(pos));
			DelaunayTriangulation();
		}

		public void AddSite(Vector2 sitePosition)
		{
			Site site = new Site(sitePosition);
			centroids.Add(site);
			List<DelaunayTriangle> remove = new List<DelaunayTriangle>();
			List<Site> needsRetriangulation = new List<Site>();
			foreach (var triangle in triangles)
			{
				if ((site.position - triangle.center).sqrMagnitude <= triangle.radiusSqr)
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

		private void DelaunayTriangulation()
		{
			triangles = new List<DelaunayTriangle>();
			edges = new List<Edge>();

			CalculateTriangulations(centroids);
		}

		private void CalculateTriangulations(List<Site> sites)
		{
			for (int i = 0; i < sites.Count - 2; ++i)
			{
				Site p1 = sites[i];
				for (int j = i + 1; j < sites.Count - 1; ++j)
				{
					Site p2 = sites[j];
					for (int k = j + 1; k < sites.Count; ++k)
					{
						Site p3 = sites[k];
						DelaunayTriangle circle = GetCircle(p1, p2, p3);

						if (circle == null)
							continue;
						foreach (var centroid in centroids)
						{
							if (centroid == p1 || centroid == p2 || centroid == p3)
								continue;
							if ((centroid.position - circle.center).sqrMagnitude <= circle.radiusSqr)
							{
								// this triangle is invalid
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

		public class DelaunayTriangle
		{
			public Site p1, p2, p3;
			public Vector2 center;
			public float radius;
			public float radiusSqr;
			public List<Edge> edges;


			public DelaunayTriangle(Site p1, Site p2, Site p3, Vector2 center, float radius)
			{
				this.p1 = p1;
				this.p2 = p2;
				this.p3 = p3;
				this.center = center;
				this.radius = radius;
				radiusSqr = radius * radius;
			}

			/// <summary>
			/// Returns any edges that have not been found yet.
			/// </summary>
			public List<Edge> CalculateEdges()
			{
				List<Edge> newEdges = new List<Edge>();
				edges = new List<Edge>();
				bool edgeFound = false;
				foreach (var edge in p1.connectedEdges)
				{
					if (edge.Contains(p2))
					{
						edges.Add(edge);
						edgeFound = true;
						break;
					}
				}

				if (!edgeFound)
				{
					Edge edge = new Edge(p1, p2);
					p1.connectedEdges.Add(edge);
					p2.connectedEdges.Add(edge);
					newEdges.Add(edge);
					edges.Add(edge);
				}
				else
					edgeFound = false;

				foreach (var edge in p1.connectedEdges)
				{
					if (edge.Contains(p3))
					{
						edges.Add(edge);
						edgeFound = true;
						break;
					}
				}

				if (!edgeFound)
				{
					Edge edge = new Edge(p1, p3);
					p1.connectedEdges.Add(edge);
					p3.connectedEdges.Add(edge);
					newEdges.Add(edge);
					edges.Add(edge);
				}
				else
					edgeFound = false;

				foreach (var edge in p2.connectedEdges)
				{
					if (edge.Contains(p3))
					{
						edges.Add(edge);
						edgeFound = true;
						break;
					}
				}

				if (!edgeFound)
				{
					Edge edge = new Edge(p2, p3);
					p2.connectedEdges.Add(edge);
					p3.connectedEdges.Add(edge);
					newEdges.Add(edge);
					edges.Add(edge);
				}
				else
					edgeFound = false;

				return newEdges;
			}
		}

		private DelaunayTriangle GetCircle(Site site1, Site site2, Site site3)
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