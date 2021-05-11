using UnityEditor;
using UnityEngine;

public static class GroupCommand
{
	[MenuItem("GameObject/Group Selected %q")]
	private static void GroupSelected()
	{
		if (!Selection.activeTransform) return;
		var go = new GameObject("_" + Selection.activeTransform.name + " Group");
		Undo.RegisterCreatedObjectUndo(go, "Group Selected");
		go.transform.SetParent(Selection.activeTransform.parent, false);
		foreach (var transform in Selection.transforms) Undo.SetTransformParent(transform, go.transform, "Group Selected");
		Selection.activeGameObject = go;
	}
}
