using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEditor;

public class JobsMenu
{
    const string UseJobThreadsPath = "Jobs/Use Job Threads";

    [MenuItem(UseJobThreadsPath, priority = 100)]
    public static void UseJobThreads()
    {
        if (JobsUtility.JobWorkerCount > 0)
            JobsUtility.JobWorkerCount = 0;
        else
            JobsUtility.ResetJobWorkerCount();
    }

    [MenuItem(UseJobThreadsPath, true, priority = 100)]
    public static bool UseJobThreadsValidate()
    {
        Menu.SetChecked(UseJobThreadsPath, JobsUtility.JobWorkerCount > 0);
        return true;
    }

    const string JobsDebuggerPath = "Jobs/JobsDebugger";

    [MenuItem(JobsDebuggerPath, priority = 101)]
    public static void JobsDebugger()
    {
        JobsUtility.JobDebuggerEnabled = !JobsUtility.JobDebuggerEnabled;
    }

    [MenuItem(JobsDebuggerPath, true, priority = 101)]
    public static bool JobsDebuggerValidate()
    {
        Menu.SetChecked(JobsDebuggerPath, JobsUtility.JobDebuggerEnabled);
        return true;
    }

    const string LeakDetectionOffPath = "Jobs/LeakDetection/Off";

    [MenuItem(LeakDetectionOffPath, priority = 102)]
    public static void LeakDetectionOff()
    {
        NativeLeakDetection.Mode = NativeLeakDetectionMode.Disabled;
    }

    [MenuItem(LeakDetectionOffPath, true, priority = 102)]
    public static bool LeakDetectionOffValidate()
    {
        Menu.SetChecked(LeakDetectionOffPath, NativeLeakDetection.Mode == NativeLeakDetectionMode.Disabled);
        return true;
    }

    const string LeakDetectionOnPath = "Jobs/LeakDetection/On";

    [MenuItem(LeakDetectionOnPath, priority = 103)]
    public static void LeakDetectionOn()
    {
        NativeLeakDetection.Mode = NativeLeakDetectionMode.Enabled;
    }

    [MenuItem(LeakDetectionOnPath, true, priority = 103)]
    public static bool LeakDetectionOnValidate()
    {
        Menu.SetChecked(LeakDetectionOnPath, NativeLeakDetection.Mode == NativeLeakDetectionMode.Enabled);
        return true;
    }

    const string LeakDetectionFullStackTracesPath = "Jobs/LeakDetection/Full Stack Traces (Expensive)";

    [MenuItem(LeakDetectionFullStackTracesPath, priority = 104)]
    public static void LeakDetectionFullStackTraces()
    {
        NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
    }

    [MenuItem(LeakDetectionFullStackTracesPath, true, priority = 104)]
    public static bool LeakDetectionFullStackTracesValidate()
    {
        Menu.SetChecked(LeakDetectionFullStackTracesPath, NativeLeakDetection.Mode == NativeLeakDetectionMode.EnabledWithStackTrace);
        return true;
    }
}