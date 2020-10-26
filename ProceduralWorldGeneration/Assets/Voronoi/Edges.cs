using System.Collections.Generic;
using AtomosZ.Voronoi.Helpers;

namespace AtomosZ.Voronoi
{
	public class VEdge : Edge<Corner>
	{
		private static int count = 0;
		public HashSet<Polygon> polygons;

		public VEdge(Corner p1, Corner p2) : base(p1, p2)
		{
			num = count++;
			p1.connectedEdges.Add(this);
			p2.connectedEdges.Add(this);
			polygons = new HashSet<Polygon>();
		}

		/// <summary>
		/// Adds edge polygons to newSite.
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

			foreach(var polygon in polygons)
				if (!polygon.corners.Contains(newSite))
				VoronoiHelper.Associate(polygon, newSite);
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


		//public void ReplaceSite(TSite oldSite, TSite newSite)
		//{
		//	if (!Contains(oldSite))
		//		throw new System.Exception("This edge does not contain input oldSite");
		//	if (start == oldSite)
		//		start = newSite;
		//	else
		//		end = newSite;
		//}
	}
}