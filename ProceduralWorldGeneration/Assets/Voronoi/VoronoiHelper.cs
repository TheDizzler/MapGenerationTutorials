﻿using System;

namespace AtomosZ.Voronoi.Helpers
{
	public static class VoronoiHelper
	{
		public static void Associate(Polygon polygon, Corner corner)
		{
			if (!polygon.corners.Contains(corner))
			{
				polygon.corners.Add(corner);
				if (corner.isOOB)
					polygon.oobCorners.Add(corner);
			}
			else
				throw new Exception("polygon already contains this corner");
			if (!corner.polygons.Contains(polygon))
				corner.polygons.Add(polygon);
		}

		public static void Associate(Polygon polygon, VEdge edge)
		{
			if (polygon.voronoiEdges.Contains(edge))
				throw new Exception("polygon already contains this edge");
			else
				polygon.voronoiEdges.Add(edge);

			if (edge.polygons.Contains(polygon))
				throw new Exception("edge already contains this polygon");
			else
				edge.polygons.Add(polygon);
		}
	}
}