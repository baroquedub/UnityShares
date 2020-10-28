/// <summary>
/// v 1.0.0 hacked together by baroquedub based on a solutoin suggested by 'float' on the Unity Forums
/// If you want to say thanks: https://www.buymeacoffee.com/baroquedub

/// original post: https://forum.unity.com/threads/hdrp-fade-to-black-in-vr.836677/#post-5528884
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
//using Sirenix.OdinInspector;

// Requires the Color Adjustments override to be added to the Volume, and make sure you tick the Color Filter option to turn it on.
public class URPScreenFade : MonoBehaviour
{
    public Volume ppGlobalVolume; // Ref to the PostProcessing Volume
    private ColorParameter cp = null;

    private IEnumerator coroutine;

    enum FadingDirection {FadeIn, FadeOut}
 
    void Awake()
    {
                ColorAdjustments colorAdjustments = null;
                if(!ppGlobalVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
                {
                    Debug.LogWarning("No color adjustments found!");
                }
                else
                    cp = colorAdjustments.colorFilter;
    }
 
    IEnumerator FadeScreen(Color from, Color to, float timing)
        {
            cp.value = from;
            float elapsedTime = 0;
            while (elapsedTime < timing)
            {
                cp.Interp(from, to, elapsedTime / timing);
                elapsedTime += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            cp.value = to;
            yield return new WaitForEndOfFrame();
        }

        //[Button]
        public void SceneFadeOut(float timeSecs = 2f){
            DoFade(FadingDirection.FadeOut, timeSecs);
        }
        //[Button]
        public void SceneFadeIn(float timeSecs = 2f){
            DoFade(FadingDirection.FadeIn, timeSecs);
        }

        private void DoFade(FadingDirection fadingDir, float timeSecs){

            Color fromColor = Color.white;
            Color toColor = Color.black * -5; // -5 ensures full darkness

            if(fadingDir == FadingDirection.FadeIn){
                fromColor = Color.black * -5; // -5 ensures full darkness
                toColor = Color.white;
            }

            // interrupt started fade and grab current value
            if (coroutine != null){
                StopCoroutine(coroutine);
                fromColor = cp.value;
            }
            coroutine = FadeScreen(fromColor, toColor, timeSecs) ;
            StartCoroutine( coroutine );
        }
}
