using System.Collections.Generic;
using AtomosZ.Tutorials.Voronoi;

namespace AtomosZ.Voronoi
{
	public class VoronoiGraph
	{
		public static HashSet<Corner> uniqueCorners;
		public static HashSet<VEdge> uniqueVEdges;
		public static int cornerCount = 0;

		public List<Polygon> polygons;


		public VoronoiGraph(DelaunayGraph dGraph)
		{
			uniqueVEdges = new HashSet<VEdge>();
			uniqueCorners = new HashSet<Corner>();
			cornerCount = 0;

			polygons = new List<Polygon>();
			for (int i = 4; i < dGraph.centroids.Count; ++i) // first 4 centroids are map corners
			{
				Polygon poly = new Polygon(dGraph.centroids[i]);
				polygons.Add(poly);
			}

			CheckMapBounds();
		}

		/// <summary>
		/// Remove corners outside of map bounds and replace with new corners on border.
		/// </summary>
		private void CheckMapBounds()
		{
			HashSet<VEdge> oobEdges = new HashSet<VEdge>();
			List<Corner> oobCorners = new List<Corner>();
			// find all corners that have atleast one neighbour also out of bounds
			foreach (var corner in uniqueCorners)
			{
				if (!VoronoiGenerator.mapBounds.Contains(corner.position))
				{
					oobCorners.Add(corner);
					//var neighbourEdges = corner.connectedEdges;
					foreach (var edge in corner.connectedEdges)
						if (!VoronoiGenerator.mapBounds.Contains(edge.start.position)
							&& !VoronoiGenerator.mapBounds.Contains(edge.end.position))
							oobEdges.Add(edge);
					//{
					//	Corner other = edge.GetOppositeSite(corner);
					//	if (!VoronoiGenerator.mapBounds.Contains(other.position))
					//	{
					//		// both sites are OOB so merge them
					//		MergeCorners(corner, other); // 
					//	}
					//}
				}
			}

			VEdge[] cullEdges = new VEdge[oobEdges.Count];
			oobEdges.CopyTo(cullEdges);

			//foreach (var edge in cullEdges)
			//{
			//	MergeCorners(edge.start, edge.end);
			//}
		}

		private void MergeCorners(Corner mergeInto, Corner remove)
		{
			if (!mergeInto.TryGetEdgeWith(remove, out VEdge connectingEdge))
				throw new System.Exception("Can't merge two corners that don't share an edge");


			foreach (var edge in remove.connectedEdges)
			{
				edge.ReplaceSite(remove, mergeInto); // this may create overlapping edges in case of small triangle polygon?
			}

			foreach (var poly in remove.polygons)
			{
				if (!mergeInto.polygons.Contains(poly))
				{
					mergeInto.polygons.Add(poly);
					poly.corners.Add(mergeInto);
				}

				poly.corners.Remove(remove);
			}


			uniqueCorners.Remove(remove);
		}
	}
}