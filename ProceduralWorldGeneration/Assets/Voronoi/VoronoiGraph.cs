using System;
using System.Collections.Generic;

namespace AtomosZ.Voronoi
{
	public class VoronoiGraph
	{
		public List<Polygon> polygons;


		public VoronoiGraph(DelaunayGraph dGraph)
		{
			polygons = new List<Polygon>();
			for (int i = 4; i < dGraph.centroids.Count; ++i) // first 4 centroids are map corners
			{
				Polygon poly = new Polygon(dGraph.centroids[i]);
				polygons.Add(poly);
			}
		}

	}
}