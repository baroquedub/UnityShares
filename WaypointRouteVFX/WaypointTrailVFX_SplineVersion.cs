// For https://assetstore.unity.com/packages/vfx/particles/spells/waypoint-route-vfx-277813

using System.Collections;
//using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Splines; // Import the Unity Spline package

namespace jdp.utils.spline.fx
{
    public class WaypointTrailVFX_SplineVersion : MonoBehaviour
    {
        public enum MovementMode
        {
            PlayOnce,  // Move along the spline once and stop
            Loop,      // Move along the spline back and forth
            Repeat     // Repeat from the start each time
        }

        [SerializeField] private GameObject pathStartVFX;
        [SerializeField] private GameObject pathVFX;

        [SerializeField] private SplineContainer splineContainer; // Reference to the Unity Spline
        [SerializeField] private float speed;
        public MovementMode movementMode = MovementMode.PlayOnce; // Default to PlayOnce
        [SerializeField] private float duration = 8.0f;
        [SerializeField] private float repeatInterval = 8.0f;
        [SerializeField] private bool autoStart = false;

        private float dstTravelled;
        private float totalDistance;
        private bool movingForward = true; // For Loop mode
        private bool pathOn = false;


        void Start()
        {
            pathStartVFX.SetActive(false);
            pathVFX.SetActive(false);

            // Calculate the total length of the spline at the start
            totalDistance = splineContainer.Spline.GetLength();

            if (autoStart)
            {
                StartPath();
            }
        }

        void Update()
        {
            if (pathOn)
            {
                switch (movementMode)
                {
                    case MovementMode.PlayOnce:
                        MovePlayOnce();
                        break;
                    case MovementMode.Loop:
                        MoveLoop();
                        break;
                    case MovementMode.Repeat:
                        MoveRepeat();
                        break;
                }

                // Update the pathVFX position based on calculated distance
                float t = dstTravelled / totalDistance;
                Vector3 positionOnSpline = splineContainer.EvaluatePosition(t);
                pathVFX.transform.position = positionOnSpline;
            }
        }
        //[HorizontalGroup("Buttons"), Button("Start Path")] // requires OdinInspector
        public void StartPath()
        {
            if (!pathOn)
            {
                dstTravelled = 0;
                pathOn = true;
                movingForward = true; // Reset forward direction for Loop
                pathVFX.SetActive(true);  // Ensure pathVFX stays active
                StartCoroutine(ShowPath());
            }
        }
        //[HorizontalGroup("Buttons"), Button("Stop Path")] // requires OdinInspector
        public void StopPath()
        {
            pathOn = false;
            pathVFX.SetActive(false);
            pathStartVFX.SetActive(false);
        }

        void MovePlayOnce()
		{
        // Move along the spline only once
        if (dstTravelled < totalDistance)
        {
            dstTravelled += speed * Time.deltaTime;
        }
        else
        {
            pathOn = false; // Stop at the end of the spline
        }
		}

        void MoveLoop()
        {
            // Move along the spline back and forth
            if (movingForward)
            {
                dstTravelled += speed * Time.deltaTime;
                if (dstTravelled >= totalDistance)
                {
                    dstTravelled = totalDistance;
                    movingForward = false; // Reverse direction at the end
                }
            }
            else
            {
                dstTravelled -= speed * Time.deltaTime;
                if (dstTravelled <= 0)
                {
                    dstTravelled = 0;
                    movingForward = true; // Reverse direction at the start
                }
            }
        }

        void MoveRepeat()
        {
            // Move along the spline, looping back to the start each time
            dstTravelled += speed * Time.deltaTime;

            // When we reach the end, reset back to the start
            if (dstTravelled >= totalDistance)
            {
                dstTravelled = 0; // Jump back to the start
            }
        }

        IEnumerator ShowPath()
        {
            pathVFX.SetActive(true); // Ensure pathVFX stays active

            yield return new WaitForSeconds(0.001f);

            pathStartVFX.SetActive(true);
            pathStartVFX.transform.position = pathVFX.transform.position;

            yield return new WaitForSeconds(duration);

            StopPath();

            yield return new WaitForSeconds(repeatInterval);

            if (movementMode == MovementMode.Repeat || movementMode == MovementMode.Loop)
            {
                StartPath();
            }
        }

    }
}