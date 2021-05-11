using UnityEditor;
using UnityEngine;

// by Jason Weimann / Unity3DCollege / https://www.youtube.com/channel/UCX_b3NNQN5bzExm-22-NVVg
public class SnapToGround : MonoBehaviour
{
    [MenuItem("Custom/Snap To Ground &q")] // alt Q
    public static void Ground()
    {
        foreach(var transform in Selection.transforms)
        {
            var hits = Physics.RaycastAll(transform.position + Vector3.up, Vector3.down, 10f);
            foreach(var hit in hits)
            {
                if (hit.collider.gameObject == transform.gameObject)
                    continue;

                transform.position = hit.point;
                break;
            }
        }
    }

}