// <copyright file="GitLocksAssetPostprocessor.cs" company="TacticalAdventures">All rights reserved.</copyright>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GitLocksAssetPostprocessor : UnityEditor.AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string str in importedAssets)
        {
            if (GitLocks.GetObjectInLockedCache(str) != null)
            {
                GitLocks.SetUncommitedCacheDirty();
            }
        }
    }
}
