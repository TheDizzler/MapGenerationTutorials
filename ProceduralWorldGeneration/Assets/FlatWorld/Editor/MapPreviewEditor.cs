using UnityEditor;
using UnityEngine;

namespace AtomosZ.Tutorials.FlatWorld.Editors
{
	[CustomEditor(typeof(MapPreview))]
	public class MapPreviewEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			MapPreview mapGen = (MapPreview)target;

			if (GUILayout.Button("Generate"))
			{
				mapGen.DrawMapInEditor();
			}
			else if (DrawDefaultInspector())
				mapGen.DrawMapInEditor();

		}
	}
}