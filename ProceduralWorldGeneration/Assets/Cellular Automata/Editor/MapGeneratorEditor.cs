using UnityEditor;
using UnityEngine;

namespace AtomosZ.Tutorials.CellAuto.Editors
{
	[CustomEditor(typeof(MapGenerator))]
	public class MapGeneratorEditor : Editor
	{

		public override void OnInspectorGUI()
		{
			MapGenerator mapGen = (MapGenerator)target;
			if (!mapGen.IsMapExist())
				mapGen.GenerateMap();

			if (GUILayout.Button("Generate") || DrawDefaultInspector())
			{
				mapGen.GenerateMap();
			}

			if (GUILayout.Button("Next Step"))
			{
				mapGen.SmoothMap(true);
				SceneView.RepaintAll();
			}
		}

	}
}