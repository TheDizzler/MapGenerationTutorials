using AtomosZ.Voronoi;
using UnityEditor;
using UnityEngine;

namespace AtomosZ.Tutorials.Voronoi.Editors
{
	[CustomEditor(typeof(VoronoiGenerator))]
	public class VoronoiGeneratorEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			VoronoiGenerator gen = (VoronoiGenerator)target;
			if (GUILayout.Button("Generate Voronoi"))
			{
				gen.GenerateMap();
			}

			if (GUILayout.Button("AddPoint"))
			{
				gen.AddPoint();
			}
		}

		void OnSceneGUI()
		{
			VoronoiGenerator gen = (VoronoiGenerator)target;

			if (gen.dGraph != null && gen.viewDelaunayTriangles)
			{
				Handles.color = Color.red;
				foreach (var edge in gen.dGraph.edges)
				{
					Handles.DrawDottedLine(edge.start.position, edge.end.position, 2);
				}
			}

			if (gen.vGraph != null && gen.viewVoronoiPolygons)
			{
				Handles.color = Color.yellow;
				foreach (var polygon in gen.vGraph.polygons)
				{
					foreach (var edge in polygon.voronoiEdges)
						Handles.DrawDottedLine(edge.start.position, edge.end.position, 2);
				}
			}
		}
	}

	[CustomEditor(typeof(VoronoiLibGenerator))]
	public class VoronoiLibGeneratorEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			VoronoiLibGenerator gen = (VoronoiLibGenerator)target;
			if (GUILayout.Button("Generate Voronoi") || DrawDefaultInspector())
			{
				gen.GeneratePoints();
			}
		}
	}
}