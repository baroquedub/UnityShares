/// <summary>
/// v 1.0.0 hacked together by baroquedub based on TransformCopier.cs v 1.2
/// If you want to say thanks: https://www.buymeacoffee.com/baroquedub

/// original about: http://wiki.unity3d.com/index.php/CopyTransform
/// You must place the script in a folder named Editor in your project's Assets folder for it to work properly.
/// Select an object in the scene hierarchy, then right click on the Transform component and select 'Copy To Console'. 
/// This copies the local position, rotation, and scale of the selected object to the clipboard, and paste the values as text to the console.
/// If you wish, you can then select another object and right click on its Transform component and select Paste Position / Rotation / Scale to apply any of these as independent values (instead of the entire component). 
/// NB Both objects must either be at the root of the scene hierarchy or within the same parent for the transform to be applied properly.
/// </summary>


 
using UnityEngine;
using UnityEditor;
using System.Collections;
 
public class TransformCopyToConsole : ScriptableObject {
 
	private static Vector3 position;
	private static Quaternion rotation;
	private static Vector3 scale;
 
	[MenuItem("CONTEXT/Transform/Copy To Console #&c",false,151)] // ShiftAltC
	static void DoCopyPaste () {
		position = Selection.activeTransform.localPosition;
		rotation = Selection.activeTransform.localRotation;
		scale = Selection.activeTransform.localScale;
		
		Debug.Log("Position: "+position.x+ " | "+position.y+ " | "+position.z);
		Debug.Log("Rotation: "+rotation.eulerAngles.x+ " | "+rotation.eulerAngles.y+ " | "+rotation.eulerAngles.z);
		Debug.Log("Scale: "+scale.x+ " | "+scale.y+ " | "+scale.z);
	}
 
	// PASTE POSITION:
	[MenuItem ("CONTEXT/Transform/Paste Position",false,200)]
	static void DoApplyPositionXYZ () {
		Transform[] selections  = Selection.transforms;
		foreach (Transform selection  in selections) {
			Undo.RecordObject(selection, "Paste Position" + selection.name);
			selection.localPosition = position;
		}
	}
	
	// PASTE ROTATION:
	[MenuItem ("CONTEXT/Transform/Paste Rotation",false,200)]
	static void DoApplyRotationXYZ () {
		Transform[] selections  = Selection.transforms;
		foreach (Transform selection  in selections){
			Undo.RecordObject(selection, "Paste Rotation" + selection.name);
			selection.localRotation = rotation;
		}
	}
	
	// PASTE SCALE:
	[MenuItem ("CONTEXT/Transform/Paste Scale",false,200)]
	static void DoApplyScaleXYZ () {
		Transform[] selections  = Selection.transforms;
		foreach (Transform selection  in selections){
			Undo.RecordObject(selection, "Paste Scale" + selection.name);
			selection.localScale = scale;
		}
	}
}