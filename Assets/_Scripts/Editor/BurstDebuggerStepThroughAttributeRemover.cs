using UnityEditor;
using UnityEngine;


class BurstDebuggerStepThroughAttributeRemover
{
    [MenuItem("Jobs/Comment out DebuggerStepThrough Attributes in Burst Intrinsics", priority = 1)]
    public static void RemoveDebugStepThrough()
    {
        var info = System.IO.Directory.CreateDirectory($"{Application.dataPath}/../Library/PackageCache/");
        var dirs = info.GetDirectories();
        System.IO.DirectoryInfo burstDir = null;
        foreach (var d in dirs)
        {
            if (d.Name.Contains("burst"))
            {
                burstDir = d;
                break;
            }
        }
        if (burstDir == null)
        {
            Debug.LogError("Could not find burst directory");
            return; 
        }

        void CommentOutAllDebuggerStepThroughAttributesInAllFiles(System.IO.FileInfo[] files)
        {
            foreach (var f in files)
            {
                string text = System.IO.File.ReadAllText(f.FullName);
                text = text.Replace("[DebuggerStepThrough]", "//[DebuggerStepThrough]");
                System.IO.File.WriteAllText(f.FullName, text);
            }
        }

        CommentOutAllDebuggerStepThroughAttributesInAllFiles(System.IO.Directory.CreateDirectory($"{burstDir.FullName}/Runtime/Intrinsics").GetFiles("*.cs"));
        CommentOutAllDebuggerStepThroughAttributesInAllFiles(System.IO.Directory.CreateDirectory($"{burstDir.FullName}/Runtime/Intrinsics/x86").GetFiles("*.cs"));

        Debug.Log("Commented out all DebuggerStepThrough Attributes in Burst Intrinsics. You need to run this command again if Unity refreshes the package cache");
    }
}

