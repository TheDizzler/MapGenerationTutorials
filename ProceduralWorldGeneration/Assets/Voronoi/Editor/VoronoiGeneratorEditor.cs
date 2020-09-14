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
			Handles.color = Color.red;
			if (gen.graph != null)
				foreach (var edge in gen.graph.edges)
				{
					Handles.DrawDottedLine(edge.start.position, edge.end.position, 2);
				}
		}
	}
}