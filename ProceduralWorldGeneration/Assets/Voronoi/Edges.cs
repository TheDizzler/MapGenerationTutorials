using System;
using System.Collections.Generic;
using AtomosZ.Voronoi.Helpers;
using UnityEngine;

namespace AtomosZ.Voronoi
{
	public class VEdge : Edge<Corner>
	{
		private static int count = 0;

		private List<Polygon> polygons = new List<Polygon>();


		public VEdge(Corner p1, Corner p2) : base(p1, p2)
		{
			num = count++;
			p1.connectedEdges.Add(this);
			p2.connectedEdges.Add(this);
		}


		public void AddPolygon(Polygon polygon)
		{
			polygons.Add(polygon);
			if (polygons.Count > 2)
				throw new System.Exception("An edge may not have more than 2 polygons!");
		}

		public void Remove(Polygon polygon)
		{
			polygons.Remove(polygon);
			Debug.Log("Edge has no polygons - deleting");
			VoronoiGraph.uniqueVEdges.Remove(this);
		}

		public List<Polygon> GetPolygons()
		{
			return polygons;
		}

		public bool Contains(Polygon polygon)
		{
			return polygons.Contains(polygon);
		}

		public int GetPolygonCount()
		{
			return polygons.Count;
		}

		/// <summary>
		/// Replaces oldSite with newSite in this edge, using newSites position and all other properties.
		/// Connects newSite with polygons this edge is border of but does not remove oldSite from polygons.
		/// </summary>
		/// <param name="oldSite"></param>
		/// <param name="newSite"></param>
		public void ReplaceSite(Corner oldSite, Corner newSite)
		{
			if (!Contains(oldSite))
				throw new System.Exception("This edge does not contain input oldSite");
			if (start == oldSite)
			{
				start.connectedEdges.Remove(this);
				start = newSite;
				start.connectedEdges.Add(this);
			}
			else
			{
				end.connectedEdges.Remove(this);
				end = newSite;
				end.connectedEdges.Add(this);
			}

			foreach (var polygon in polygons)
			{
				if (!polygon.corners.Contains(newSite))
					VoronoiHelper.Associate(polygon, newSite);
			}
		}

		public bool SharesCorner(VEdge lastEdge, out Corner sharedCorner)
		{
			sharedCorner = null;
			if (lastEdge.Contains(start))
				sharedCorner = start;
			else if (lastEdge.Contains(end))
				sharedCorner = end;

			return sharedCorner != null;
		}

		public bool HasCornerOnBorder(out List<Corner> borderCorners)
		{
			borderCorners = new List<Corner>();
			if (start.isOnBorder)
				borderCorners.Add(start);
			if (end.isOnBorder)
				borderCorners.Add(end);

			return borderCorners.Count > 0;
		}
	}

	public class DEdge : Edge<Centroid>
	{
		private static int count = 0;

		public DEdge(Centroid p1, Centroid p2) : base(p1, p2)
		{
			num = count++;
			p1.connectedEdges.Add(this);
			p2.connectedEdges.Add(this);
		}
	}

	/// <summary>
	/// A line that connects to sites.
	/// </summary>
	public abstract class Edge<TSite> where TSite : Site
	{
		public TSite start, end;
		public int num;


		public Edge(TSite p1, TSite p2)
		{
			start = p1;
			end = p2;
		}

		public bool Contains(TSite p2)
		{
			return start == p2 || end == p2;
		}

		public TSite GetOppositeSite(TSite site)
		{
			if (!Contains(site))
				throw new System.Exception("This edge does not contain input site");
			if (start == site)
				return end;
			return start;
		}
	}
}