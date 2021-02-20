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
				Region region = ((Region)target);
				base.OnInspectorGUI();
				if (check.changed && region.polygon != null)
					region.UpdateMeshHeights();
			}
		}
	}
}