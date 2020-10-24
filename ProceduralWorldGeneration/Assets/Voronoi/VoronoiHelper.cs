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

			if (!corner.polygons.Contains(polygon))
				corner.polygons.Add(polygon);
		}
	}
}