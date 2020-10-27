using System;

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
				{
					polygon.oobCorners.Add(corner);
					polygon.isOnBorder = true;
				}
				else if (corner.isOnBorder)
					polygon.isOnBorder = true;
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

			if (edge.Contains(polygon))
				throw new System.Exception("edge already contains this polygon");
			edge.AddPolygon(polygon);
		}


		public static void MergeCorners(Corner deprecatedCorner, Corner mergedCorner)
		{
			if (!(deprecatedCorner.isOnBorder ^ mergedCorner.isOnBorder)) // if they are both on the border or both off
			{
				mergedCorner.position = (deprecatedCorner.position + mergedCorner.position) / 2; // get midpoint
				mergedCorner.isOnBorder = deprecatedCorner.isOnBorder;
			}
			else if (deprecatedCorner.isOnBorder) // if only old corner is on the border use its position
			{
				mergedCorner.position = deprecatedCorner.position;
				mergedCorner.isOnBorder = true;
			}
			// otherwise mergedCorner is on the border so leave as it is

			for (int i = deprecatedCorner.connectedEdges.Count - 1; i >= 0; --i)
				deprecatedCorner.connectedEdges[i].ReplaceSite(deprecatedCorner, mergedCorner);

			deprecatedCorner.isInvalidated = true;
		}

	}
}