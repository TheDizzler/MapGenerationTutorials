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
			foreach (var centroid in dGraph.centroids)
			{
				Polygon poly = new Polygon(centroid);
				polygons.Add(poly);
			}
		}

	}
}