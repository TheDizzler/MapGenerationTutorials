using AtomosZ.Voronoi.Regions;
using UnityEditor;

namespace AtomosZ.Voronoi.EditorTools
{
	[CustomEditor(typeof(Region))]
	public class RegionEditor : Editor
	{

		public override void OnInspectorGUI()
		{
			using (var check = new EditorGUI.ChangeCheckScope())
			{
				base.OnInspectorGUI();
				if (check.changed)
					((Region)target).HeightChanged();
			}
		}
	}
}