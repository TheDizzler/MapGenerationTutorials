using UnityEditor;
using UnityEngine;

namespace AtomosZ.Tutorials.FlatWorld.Editors
{
	[CustomEditor(typeof(HeightMapSettings))]
	public class NoiseDataEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			if (DrawDefaultInspector() || GUILayout.Button("Update"))
			{
				var mapGen = FindObjectOfType<MapPreview>();
				if (mapGen != null)
					mapGen.DrawMapInEditor();

			}
		}
	}

	[CustomEditor(typeof(MeshSettings))]
	public class TerrainDataEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			if (DrawDefaultInspector() || GUILayout.Button("Update"))
			{
				var mapGen = FindObjectOfType<MapPreview>();
				if (mapGen != null)
					mapGen.DrawMapInEditor();
			}
		}
	}

	[CustomEditor(typeof(TextureData))]
	public class TextureDataEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			if (DrawDefaultInspector() || GUILayout.Button("Update"))
			{
				var mapGen = FindObjectOfType<MapPreview>();
				if (mapGen != null)
					mapGen.DrawMapInEditor();

			}
		}
	}
}