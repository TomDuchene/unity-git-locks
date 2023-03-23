// <copyright file="GitLocksObject.cs" company="Tom Duchene and Tactical Adventures">All rights reserved.</copyright>

using System;
using UnityEditor;

[System.Serializable]
public class GitLocksObject
{
    public int id { get; set; }

    public string path { get; set; }

    public LockedObjectOwner owner { get; set; }

    public string locked_at { get; set; }

    public UnityEngine.Object objectRef = null;

    public UnityEngine.Object GetObjectReference()
    {
        if (this.objectRef != null)
        {
            return this.objectRef;
        }
        else
        {
            this.objectRef = AssetDatabase.LoadMainAssetAtPath(this.path);
            return this.objectRef;
        }
    }

    public bool IsMine()
    {
        return this.owner.name == GitLocks.GetGitUsername();
    }

    public string GetLockDateTimeString()
    {
        DateTime dt = DateTime.Parse(locked_at);
        string r = dt.ToShortDateString() + " - " + dt.ToShortTimeString();
        return r;
    }
}

public class LockedObjectOwner
{
    public string name;
}