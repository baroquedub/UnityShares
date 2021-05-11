using UnityEditor;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections;

// Turns off Jobs Debugging

[InitializeOnLoad]
class JobsAutoFix
{
    static JobsAutoFix()
    {
        JobsUtility.JobDebuggerEnabled = false;
        NativeLeakDetection.Mode = NativeLeakDetectionMode.Disabled;
    }
}
