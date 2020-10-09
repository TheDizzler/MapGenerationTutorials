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
				if (centroid.dTriangles.Count == 2) // check if a proper centroid and not a map corner
					continue;
				Polygon poly = new Polygon(centroid);
				polygons.Add(poly);
			}
		}

	}
}