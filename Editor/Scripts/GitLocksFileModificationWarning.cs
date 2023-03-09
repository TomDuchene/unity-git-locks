// <copyright file="GitLocksFileModificationWarning.cs" company="Tom Duchene and Tactical Adventures">All rights reserved.</copyright>

using UnityEditor;

public class GitLocksFileModificationWarning : UnityEditor.AssetModificationProcessor
{
    private static string[] OnWillSaveAssets(string[] paths)
    {
        if (!GitLocks.IsEnabled())
        {
            return paths; // Early return if the whole tool is disabled
        }

        // Refresh the uncommited cache as it probably has changed
        GitLocks.SetUncommitedCacheDirty();

        // check if we should display a warning for these files
        if (GitLocks.GetDisplayLocksConflictWarning())
        {
            foreach (string path in paths)
            {
                GitLocksObject lo = GitLocks.GetObjectInLockedCache(path);
                if (lo != null && !lo.IsMine() && !GitLocks.IsFileInConflictIgnoreList(path))
                {
                    EditorUtility.DisplayDialog("Warning", "The following file you just saved is currently locked by " + lo.owner.name + ", you will not be able to push it.\n" + lo.path, "OK");
                    GitLocks.AddFileToConflictWarningIgnoreList(path);
                }
            }
        }

        return paths;
    }
}