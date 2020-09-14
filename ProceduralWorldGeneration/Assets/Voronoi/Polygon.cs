using System;
using System.Collections.Generic;
using UnityEngine;

namespace AtomosZ.Voronoi
{
	public class Polygon : MonoBehaviour
	{
		public Color color;

		public Site centroid;
		/// <summary>
		/// Connections to neighbouring polygons.
		/// </summary>
		public List<Edge> delaunayEdges;
		public List<Site> corners;
		/// <summary>
		/// Edges that seperate polygons
		/// </summary>
		public List<Edge> voronoiEdges;


		public Polygon(Vector2 centroidPos)
		{
			centroid = new Site(centroidPos);
		}
	}


	public class Site
	{
		public Vector2 position;
		public List<Edge> connectedEdges = new List<Edge>();


		public Site(Vector2 pos)
		{
			position = pos;
		}
	}


	/// <summary>
	/// A line that connects to sites.
	/// </summary>
	public class Edge
	{
		public Site start, end;

		public Edge(Site p1, Site p2)
		{
			start = p1;
			end = p2;
		}

		public bool Contains(Site p2)
		{
			return start == p2 || end == p2;
		}
	}
}