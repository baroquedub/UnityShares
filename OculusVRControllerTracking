/*
    by Joe Strout https://forum.unity.com/members/joestrout.32300/
    from https://forum.unity.com/threads/oculus-go-controller.555949/#post-3681937
    
    <summary>
      Attach to some object in scene.
      Create whatever you want to represent the controller visually (I typically start in a new project with just a scaled cube, with maybe another cube sticking out of one face like a wand). 
      Drag that into this script's controller slot
    </summary>
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class OculusVRControllerTracking : MonoBehaviour {
 
    public Transform controller;
   
    public static bool leftHanded { get; private set; }
 
    System.IO.StreamWriter recording;
 
    void Awake() {
        #if UNITY_EDITOR
        leftHanded = false;        // (whichever you want to test here)
        #else
        leftHanded = OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch);
        #endif
    }
 
    void Update() {
        OVRInput.Controller c = leftHanded ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
        if (OVRInput.GetControllerPositionTracked(c)) {
            controller.localRotation = OVRInput.GetLocalControllerRotation(c);
            controller.localPosition = OVRInput.GetLocalControllerPosition(c);
        }
    }  
}
 
