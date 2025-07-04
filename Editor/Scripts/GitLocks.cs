// <copyright file="GitLocks.cs" company="Tom Duchene and Tactical Adventures">All rights reserved.</copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class GitLocks : ScriptableObject
{
    private static List<GitLocksObject> lockedObjectsCache;
    private static List<string> uncommitedFilesCache;
    private static List<string> modifiedOnServerFilesCache;
    private static bool uncommitedFilesCacheDirty = true;
    private static List<string> conflictWarningIgnoreList;
    private static DateTime lastRefresh = DateTime.MinValue;
    private static bool currentlyRefreshing = false;
    private static string gitVersion;

    private static float requestTimeout = 30; // seconds

    private static string refreshCallbackResult;
    private static string refreshCallbackError;

    // Ignore meta files to check lockable/unlockable files
    static string[] ignoredExtensions = new[] { ".meta" };

    static GitLocks()
    {
        EditorApplication.update += Update;
        EditorApplication.wantsToQuit += WantsToQuit;

        // Preferences default
        if (!EditorPrefs.HasKey("gitLocksEnabled"))
        {
            EditorPrefs.SetBool("gitLocksEnabled", true);
        }

        if (!EditorPrefs.HasKey("gitLocksAutoRefreshLocks"))
        {
            EditorPrefs.SetBool("gitLocksAutoRefreshLocks", true);
        }

        if (!EditorPrefs.HasKey("gitLocksRefreshLocksInterval"))
        {
            EditorPrefs.SetInt("gitLocksRefreshLocksInterval", 5);
        }

        if (!EditorPrefs.HasKey("gitLocksMaxFilesNumPerRequest"))
        {
            EditorPrefs.SetInt("gitLocksMaxFilesNumPerRequest", 15);
        }

        if (!EditorPrefs.HasKey("displayLocksConflictWarning"))
        {
            EditorPrefs.SetBool("displayLocksConflictWarning", true);
        }

        if (!EditorPrefs.HasKey("warnIfIStillOwnLocksOnQuit"))
        {
            EditorPrefs.SetBool("warnIfIStillOwnLocksOnQuit", true);
        }

        if (!EditorPrefs.HasKey("warnIfFileHasBeenModifiedOnServer"))
        {
            EditorPrefs.SetBool("warnIfFileHasBeenModifiedOnServer", true);
        }

        if (!EditorPrefs.HasKey("notifyNewLocks"))
        {
            EditorPrefs.SetBool("notifyNewLocks", false);
        }

        if (!EditorPrefs.HasKey("numOfMyLocksDisplayed"))
        {
            EditorPrefs.SetInt("numOfMyLocksDisplayed", 5);
        }

        if (!EditorPrefs.HasKey("gitLocksColorblindMode"))
        {
            EditorPrefs.SetBool("gitLocksColorblindMode", false);
        }

        if (!EditorPrefs.HasKey("gitLocksDebugMode"))
        {
            EditorPrefs.SetBool("gitLocksDebugMode", false);
        }

        if (!EditorPrefs.HasKey("gitLocksShowForceButtons"))
        {
            EditorPrefs.SetBool("gitLocksShowForceButtons", false);
        }

        conflictWarningIgnoreList = new List<string>();

        GetGitVersion();
    }

    // Properties
    public static List<GitLocksObject> LockedObjectsCache => lockedObjectsCache;

    public static DateTime LastRefresh => lastRefresh;

    public static bool CurrentlyRefreshing => currentlyRefreshing;

    // Methods 
    public static void CheckLocksRefresh()
    {
        if (EditorPrefs.GetBool("gitLocksAutoRefreshLocks", true) && DateTime.Now > lastRefresh.AddMinutes(EditorPrefs.GetInt("gitLocksRefreshLocksInterval", 5)) && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            RefreshLocks();
        }
    }

    public static void RefreshLocks()
    {
        lastRefresh = DateTime.Now;

        // Get the locks asynchronously
        currentlyRefreshing = true;
        ExecuteNonBlockingProcessTerminal("git", "lfs locks --json");
    }

    public static void RefreshCallback(string result)
    {
        // If empty result, start a simple git lfs locks (no json) to catch potential errors
        if (result == "[]")
        {
            ExecuteNonBlockingProcessTerminal("git", "lfs locks");
        }

        // Check that we're receiving what seems to be a JSON result
        if (result[0] == '[')
        {
            // Save old locks
            List<GitLocksObject> oldLocks;
            if (lockedObjectsCache != null)
            {
                oldLocks = new List<GitLocksObject>(lockedObjectsCache);
            }
            else
            {
                oldLocks = new List<GitLocksObject>();
            }

            // Parse the string into a nice list of objects
            lockedObjectsCache = ParseJsonIntoLockedObjects(result);

            // Check if we should warn the user about conflicts between uncommited and locked files 
            BuildUncommitedCache();
            if (GetDisplayLocksConflictWarning())
            {
                string conflictMessage = "The following files are currently locked and you have uncommited changes on them that you'll probably not be able to push:";
                bool conflictFound = false;
                foreach (GitLocksObject lo in lockedObjectsCache)
                {
                    if (IsLockedObjectConflictingWithUncommitedFile(lo) && !IsFileInConflictIgnoreList(lo.path))
                    {
                        conflictFound = true;
                        conflictMessage += "\n" + lo.path;
                        AddFileToConflictWarningIgnoreList(lo.path);
                    }
                }
                if (conflictFound)
                {
                    EditorUtility.DisplayDialog("Warning", conflictMessage, "OK");
                }
            }

            // Remove files from ignored warnings list if they're not locked anymore
            for (int i = conflictWarningIgnoreList.Count - 1; i >= 0; i--)
            {
                GitLocksObject lo = GetObjectInLockedCache(conflictWarningIgnoreList[i]);
                if (lo == null || lo.IsMine())
                {
                    conflictWarningIgnoreList.RemoveAt(i);
                }
            }

            // Check if we should warn the user that there are new locks
            if (EditorPrefs.GetBool("notifyNewLocks", false))
            {
                string newLocksString = string.Empty;
                foreach (GitLocksObject potentialNewLock in lockedObjectsCache)
                {
                    // Was the lock already there ?
                    bool lockAlreadyThere = false;
                    foreach (GitLocksObject lo in oldLocks)
                    {
                        if (lo.path == potentialNewLock.path)
                        {
                            lockAlreadyThere = true;
                            break;
                        }
                    }

                    if (!lockAlreadyThere && potentialNewLock.owner.name != EditorPrefs.GetString("gitLocksHostUsername"))
                    {
                        if (newLocksString != string.Empty)
                        {
                            newLocksString += "\n";
                        }

                        newLocksString += "[" + potentialNewLock.owner.name + "] " + potentialNewLock.path;
                    }
                }

                if (newLocksString != string.Empty)
                {
                    EditorUtility.DisplayDialog("New locks", newLocksString, "OK");
                }
            }

            // Sort the locks to show mine first
            if (lockedObjectsCache.Count > 0)
            {
                lockedObjectsCache.Sort(delegate (GitLocksObject a, GitLocksObject b)
                {
                    // ^ is exclusive OR, compare if only one is equal to the git username
                    if (a.IsMine() ^ b.IsMine())
                    {
                        return a.IsMine() ? -1 : 1; // Put my locks first
                    }
                    else
                    {
                        return a.path.CompareTo(b.path); // When it's the same owner, sort by path
                    }
                });
            }
        }

        GitLocksDisplay.RepaintAll();
    }

    public static string ExecuteProcessTerminal(string processName, string processArguments, out string errorString, bool openTerminal = false)
    {
        DebugLog("ExecuteProcessTerminal: " + processName + " with the following parameters:\n" + processArguments);

        if (openTerminal)
        {
            Process p = new Process();
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = "/k " + processName + " " + processArguments;
            p.StartInfo = psi;
            p.Start();

            errorString = String.Empty;
            return String.Empty;
        }
        else
        {
            try
            {
                using (System.Diagnostics.Process p = new System.Diagnostics.Process())
                {
                    // Redirect the output stream of the child process.
                    p.StartInfo.CreateNoWindow = !openTerminal;
                    p.StartInfo.UseShellExecute = openTerminal;
                    p.StartInfo.RedirectStandardOutput = !openTerminal;
                    p.StartInfo.RedirectStandardError = !openTerminal;
                    p.StartInfo.FileName = processName;
                    p.StartInfo.Arguments = processArguments;

                    System.Text.StringBuilder output = new System.Text.StringBuilder();
                    System.Text.StringBuilder error = new System.Text.StringBuilder();

                    using (System.Threading.AutoResetEvent outputWaitHandle = new System.Threading.AutoResetEvent(false))
                    using (System.Threading.AutoResetEvent errorWaitHandle = new System.Threading.AutoResetEvent(false))
                    {
                        p.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                outputWaitHandle.Set();
                            }
                            else
                            {
                                output.AppendLine(e.Data);
                            }
                        };
                        p.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                errorWaitHandle.Set();
                            }
                            else
                            {
                                error.AppendLine(e.Data);
                            }
                        };

                        p.Start();

                        if (!openTerminal)
                        {
                            p.BeginOutputReadLine();
                            p.BeginErrorReadLine();
                        }

                        int timeout = (int)(requestTimeout * 1000);
                        if (p.WaitForExit(timeout) &&
                            outputWaitHandle.WaitOne(timeout) &&
                            errorWaitHandle.WaitOne(timeout))
                        {
                            errorString = error.ToString();
                            return output.ToString();
                        }
                        else
                        {
                            string err = "Error: Process timed out (" + processName + " " + processArguments + ")";
                            errorString = err;
                            UnityEngine.Debug.LogError(err);
                            return err;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e);
                errorString = "e";
                return null;
            }
        }
    }

    public static string ExecuteProcessTerminal(string processName, string processArguments, bool openTerminal = false)
    {
        string errorStr;
        string output = ExecuteProcessTerminal(processName, processArguments, out errorStr, openTerminal);
        return output;
    }

    public static string ExecuteProcessTerminalWithConsole(string processName, string processArguments)
    {
        string errorStr;
        string output = ExecuteProcessTerminal(processName, processArguments, out errorStr);
        if (errorStr.Length > 0)
        {
            EditorUtility.DisplayDialog("Git Lfs Locks Error", errorStr, "OK");
            UnityEngine.Debug.LogError(errorStr);
        }

        return output;
    }

    public static void ExecuteNonBlockingProcessTerminal(string processName, string processArguments)
    {
        DebugLog("ExecuteNonBlockingProcessTerminal: " + processName + " with the following parameters:\n" + processArguments);

        try
        {
            // Start the child process.
            System.Diagnostics.Process p = new System.Diagnostics.Process();

            // Redirect the output stream of the child process.
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.FileName = processName;
            p.StartInfo.Arguments = processArguments;
            p.EnableRaisingEvents = true;
            p.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler((sender, e) =>
            {
                // Store the results to be treated on the main thread
                if (e.Data != null)
                {
                    refreshCallbackResult = e.Data;
                }
            });
            p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler((sender, e) =>
            {
                // Store the errors to be treated on the main thread
                if (e.Data != null)
                {
                    refreshCallbackError = e.Data;
                }
            });
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
        }
        catch (Exception e)
        {
            UnityEngine.Debug.Log("Error :" + e);
        }
    }

    public static List<GitLocksObject> ParseJsonIntoLockedObjects(string jsonString)
    {
        JArray o = JArray.Parse(jsonString);
        List<GitLocksObject> llo = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GitLocksObject>>(jsonString);
        return llo;
    }

    public static bool IsObjectAvailableToUnlock(UnityEngine.Object obj)
    {
        return IsObjectAvailableToUnlock(AssetDatabase.GetAssetPath(obj.GetInstanceID()));
    }

    public static bool IsObjectAvailableToUnlock(string path)
    {
        if (lockedObjectsCache == null)
        {
            return false;
        }

        if (ignoredExtensions.Any(s => path.Contains(s)))
        {
            return false;
        }

        foreach (GitLocksObject lo in lockedObjectsCache)
        {
            if (path.Replace("\\", "/") == lo.path.Replace("\\", "/"))
            {
                return lo.IsMine();
            }
        }

        return false;
    }

    public static bool IsObjectAvailableToUnlock(GitLocksObject lo)
    {
        if (lockedObjectsCache == null)
        {
            return false;
        }

        return lo.IsMine();
    }

    public static bool IsObjectAvailableToLock(UnityEngine.Object obj)
    {
        return IsObjectAvailableToLock(AssetDatabase.GetAssetPath(obj.GetInstanceID()));
    }

    public static bool IsObjectAvailableToLock(string path)
    {
        if (lockedObjectsCache == null)
        {
            return false;
        }

        if (ignoredExtensions.Any(s => path.Contains(s)))
        {
            return false;
        }

        foreach (GitLocksObject lo in lockedObjectsCache)
        {
            if (path.Replace("\\","/") == lo.path.Replace("\\", "/"))
            {
                return false;
            }
        }

        return true;
    }

    public static GitLocksObject GetObjectInLockedCache(UnityEngine.Object obj)
    {
        if (lockedObjectsCache == null)
        {
            return null;
        }

        foreach (GitLocksObject lo in lockedObjectsCache)
        {
            if (obj == lo.GetObjectReference())
            {
                return lo;
            }
        }

        return null;
    }

    public static GitLocksObject GetObjectInLockedCache(string path)
    {
        if (lockedObjectsCache == null)
        {
            return null;
        }

        foreach (GitLocksObject lo in lockedObjectsCache)
        {
            if (path == lo.path)
            {
                return lo;
            }
        }

        return null;
    }

    public static void LockFile(string path)
    {
        List<string> list = new List<string>();
        list.Add(path);
        LockFiles(list);
    }

    public static void LockFiles(List<string> paths)
    {
        // Logs
        if (paths.Count > 1)
        {
            DebugLog("Trying to lock " + paths.Count + " files");
        }
        else
        {
            DebugLog("Trying to lock " + paths[0]);
        }

        // Split into multiple requests if there are too many files (prevents triggering timeout)
        int numOfRequests = (int)Math.Ceiling((float)paths.Count / (float)EditorPrefs.GetInt("gitLocksMaxFilesNumPerRequest", 15));
        List<string> pathsStrings = new List<string>(new string[numOfRequests]);
        for (int i = 0; i < paths.Count; i++)
        {
            string p = paths[i];
            // Optionally, check if the file we want to lock has been modified on the server
            if (EditorPrefs.GetBool("warnIfFileHasBeenModifiedOnServer") && HasFileBeenModifiedOnServer(p))
            {
                if (EditorUtility.DisplayDialog("File modified on the server", "Warning! The file you want to lock has been modified on the server already, you REALLY should pull before locking or you'll almost certainly get merge conflicts.", "OK, don't lock yet", "I know what I'm doing, lock anyway"))
                {
                    return;
                }
            }

            int stringIndex = (int)Math.Floor((float)i / (float)EditorPrefs.GetInt("gitLocksMaxFilesNumPerRequest", 15) + Mathf.Epsilon);
            pathsStrings[stringIndex] += "\"" + p + "\" ";
        }

        // Send each request
        foreach (string pathsString in pathsStrings)
        {
            ExecuteProcessTerminalWithConsole("git", "lfs lock " + pathsString);
        }
    }

    public static void UnlockFile(string path, bool force = false)
    {
        List<string> list = new List<string>();
        list.Add(path);
        UnlockFiles(list, force);
    }

    public static void UnlockFiles(List<string> paths, bool force = false)
    {
        if (paths.Count > 1)
        {
            DebugLog("Trying to unlock " + paths.Count + " files");
        }
        else
        {
            DebugLog("Trying to unlock " + paths[0]);
        }

        // Split into multiple requests if there are too many files (prevents triggering timeout)
        int numOfRequests = (int)Math.Ceiling((float)paths.Count / (float)EditorPrefs.GetInt("gitLocksMaxFilesNumPerRequest", 15));
        List<string> pathsStrings = new List<string>(new string[numOfRequests]);
        for (int i = 0; i < paths.Count; i++)
        {
            string p = paths[i];

            int stringIndex = (int)Math.Floor((float)i / (float)EditorPrefs.GetInt("gitLocksMaxFilesNumPerRequest", 15) + Mathf.Epsilon);
            pathsStrings[stringIndex] += "\"" + p + "\" ";
        }

        // Send each request
        foreach (string pathsString in pathsStrings)
        {
            ExecuteProcessTerminalWithConsole("git", "lfs unlock " + pathsString + (force ? "--force" : string.Empty));
        }
    }

    public static void UnlockAllMyLocks()
    {
        List<string> paths = new List<string>();
        foreach (GitLocksObject lo in lockedObjectsCache)
        {
            if (lo.IsMine())
            {
                paths.Add(lo.path);
            }
        }

        UnlockFiles(paths);
        RefreshLocks();
    }

    public static void UnlockMultipleLocks(List<GitLocksObject> toUnlock)
    {
        List<string> paths = new List<string>();
        foreach (GitLocksObject lo in toUnlock)
        {
            // Sanity check
            if (lo.IsMine())
            {
                paths.Add(lo.path);
            }
        }

        UnlockFiles(paths);
        RefreshLocks();
    }

    public static List<GitLocksObject> GetMyLocks()
    {
        if (lockedObjectsCache == null || lockedObjectsCache.Count == 0)
        {
            return null;
        }

        List<GitLocksObject> myLocks = new List<GitLocksObject>();
        foreach (GitLocksObject lo in lockedObjectsCache)
        {
            if (lo.IsMine())
            {
                myLocks.Add(lo);
            }
        }

        return myLocks;
    }

    public static List<GitLocksObject> GetOtherLocks()
    {
        if (lockedObjectsCache == null || lockedObjectsCache.Count == 0)
        {
            return null;
        }

        List<GitLocksObject> myLocks = new List<GitLocksObject>();
        foreach (GitLocksObject lo in lockedObjectsCache)
        {
            if (!lo.IsMine())
            {
                myLocks.Add(lo);
            }
        }

        return myLocks;
    }

    public static string GetGitUsername()
    {
        return EditorPrefs.GetString("gitLocksHostUsername", string.Empty);
    }

    public static string GetGitVersion()
    {
        if (gitVersion == null || gitVersion == string.Empty)
        {
            gitVersion = ExecuteProcessTerminalWithConsole("git", "--version");
        }

        return gitVersion;
    }

    public static bool IsGitOutdated()
    {
        string[] split = GetGitVersion().Split('.');

        int verNum1, verNum2;
        bool parse1 = int.TryParse(split[0].Substring(split[0].Length - 1), out verNum1);
        bool parse2 = int.TryParse(split[1], out verNum2);
        if (parse1 && parse2)
        {
            // Git for windows version should be greater than 2.30.0 to have the latest authentication manager
            return verNum1 < 2 || verNum2 < 30;
        }
        else
        {
            UnityEngine.Debug.LogWarning("GitLocks couldn't parse the Git for Windows version properly");
            return true;
        }
    }

    public static bool GetDisplayLocksConflictWarning()
    {
        return EditorPrefs.GetBool("displayLocksConflictWarning");
    }

    public static string GetAssetPathFromPrefabGameObject(int instanceID)
    {
        var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        return GetAssetPathFromPrefabGameObject(gameObject);
    }

    public static string GetAssetPathFromPrefabGameObject(GameObject gameObject)
    {
        if (gameObject != null)
        {
            return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
        }

        return string.Empty;
    }

    public static bool IsObjectPrefabRoot(int instanceID)
    {
        var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        return IsObjectPrefabRoot(gameObject);
    }

    public static bool IsObjectPrefabRoot(GameObject gameObject)
    {
        if (gameObject != null)
        {
            return PrefabUtility.GetNearestPrefabInstanceRoot(gameObject) == gameObject;
        }

        return false;
    }

    public static Scene GetSceneFromInstanceID(int instanceID)
    {
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            var scene = SceneManager.GetSceneAt(index);
            if (scene.handle == instanceID)
            {
                return scene;
            }
        }

        return default;
    }

    public static bool IsLockedObjectConflictingWithUncommitedFile(GitLocksObject lo)
    {
        if (uncommitedFilesCache == null || uncommitedFilesCache.Count == 0)
        {
            return false;
        }

        if (GetGitUsername() == null || GetGitUsername() == string.Empty)
        {
            return false;
        }

        foreach (string uncommitedPath in uncommitedFilesCache)
        {
            if (lo.path == uncommitedPath && !lo.IsMine())
            {
                return true;
            }
        }

        return false;
    }

    public static void SetUncommitedCacheDirty()
    {
        uncommitedFilesCacheDirty = true;
    }

    public static void BuildUncommitedCache()
    {
        if (uncommitedFilesCache != null)
        {
            uncommitedFilesCache.Clear();
        }
        else
        {
            uncommitedFilesCache = new List<string>();
        }

        char[] splitter = { '\n' };

        // Staged
        string output = ExecuteProcessTerminal("git", "diff --name-only --staged");
        string[] lines = output.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
        List<string> filesCandidates = new List<string>(lines);

        // Not staged
        output = ExecuteProcessTerminal("git", "diff --name-only");
        lines = output.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
        filesCandidates.AddRange(lines);

        // Check all lines to see if they correspond to files and add them to the cache if so
        foreach (string file in filesCandidates)
        {
            // Clean and format strings
            string tmpFile = file;
            tmpFile = tmpFile.Trim();
            tmpFile = tmpFile.Replace("\\r", string.Empty);
            tmpFile = tmpFile.Replace("\\n", string.Empty);
            string fullPath = Application.dataPath.Replace("Assets", string.Empty) + tmpFile;
            fullPath = fullPath.Replace("/", "\\");

            if (System.IO.File.Exists(fullPath))
            {
                uncommitedFilesCache.Add(tmpFile);
            }
        }

        uncommitedFilesCacheDirty = false;

        GitLocksDisplay.RepaintAll();
    }

    public static void BuildModifiedOnServerCache()
    {
        if (modifiedOnServerFilesCache != null)
        {
            modifiedOnServerFilesCache.Clear();
        }
        else
        {
            modifiedOnServerFilesCache = new List<string>();
        }

        char[] splitter = { '\n' };

        // Construct list of branches to check
        HashSet<string> branchesToCheck = new HashSet<string>();

        // Add optional branches to check set in preferences
        if (EditorPrefs.HasKey("gitLocksBranchesToCheck"))
        {
            string fullString = EditorPrefs.GetString("gitLocksBranchesToCheck");
            string[] array = fullString.Split(',');
            foreach (string branch in array)
            {
                if (branch != string.Empty)
                {
                    branchesToCheck.Add(branch);
                }
            }
        }

        // Add current branch name
        string currentBranch = GetCurrentBranch();
        branchesToCheck.Add(currentBranch);

        foreach (string branch in branchesToCheck)
        {
            // Fetch
            ExecuteProcessTerminal("git", "git fetch origin " + branch);

            // List all distant commits
            string output = ExecuteProcessTerminal("git", "rev-list " + currentBranch + "..origin/" + branch);
            string[] lines = output.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
            List<string> commits = new List<string>(lines);

            // Check all commits
            foreach (string commit in commits)
            {
                // Add all files in commit to the list
                string filesOutput = ExecuteProcessTerminal("git", "diff-tree --no-commit-id --name-only -r " + commit.Replace("\r", ""));
                string[] files = filesOutput.Split(splitter, StringSplitOptions.RemoveEmptyEntries);

                foreach (string file in files)
                {
                    // Check that every file exists (sanity)
                    if (System.IO.File.Exists(file.Replace("\r", "")))
                    {
                        modifiedOnServerFilesCache.Add(file.Replace("\r", ""));
                    }
                }
            }
        }
    }

    public static bool HasFileBeenModifiedOnServer(string filePath)
    {
        BuildModifiedOnServerCache();
        return modifiedOnServerFilesCache.Contains(filePath);
    }

    public static void AddFileToConflictWarningIgnoreList(string path)
    {
        conflictWarningIgnoreList.Add(path);
    }

    public static bool IsFileInConflictIgnoreList(string path)
    {
        return conflictWarningIgnoreList.Contains(path);
    }

    public static bool IsEnabled()
    {
        return EditorPrefs.GetBool("gitLocksEnabled", false);
    }

    public static string GetCurrentBranch()
    {
        char[] splitter = { '\n' };
        string currentBranch = ExecuteProcessTerminal("git", "rev-parse --abbrev-ref HEAD");
        currentBranch = currentBranch.Split(splitter)[0].Replace("\r", "");
        return currentBranch;
    }

    private static void Update()
    {
        if (!IsEnabled() || EditorApplication.isUpdating || EditorApplication.isCompiling || EditorApplication.isPlaying)
        {
            return; // Early return if the whole tool is disabled of if the Editor is not available
        }

        if (uncommitedFilesCacheDirty)
        {
            BuildUncommitedCache();
        }

        if (refreshCallbackResult != null)
        {
            if (refreshCallbackResult != string.Empty)
            {
                RefreshCallback(refreshCallbackResult);
            }

            currentlyRefreshing = false;
            refreshCallbackResult = null;
        }

        if (refreshCallbackError != null && refreshCallbackError != string.Empty)
        {
            if (!EditorUtility.DisplayDialog("Git Lfs Locks Error", "Git lfs locks error :\n\n" + refreshCallbackError + "\n\nIf it's your first time using the tool, you should probably setup the credentials manager", "OK", "Setup credentials"))
            {
                DebugLog("Setup credentials manager");
                ExecuteProcessTerminalWithConsole("git", "config --global credential.helper manager");
            }

            refreshCallbackError = null;
        }
    }

    private static bool WantsToQuit()
    {
        if (!IsEnabled())
        {
            return true; // Early return if the whole tool is disabled
        }

        if (EditorPrefs.GetBool("warnIfIStillOwnLocksOnQuit") && GetMyLocks() != null && GetMyLocks().Count > 0)
        {
            if (EditorUtility.DisplayDialog("Remaining locks", "You still own locks on some files, do you want to quit anyway ?", "Yes", "No, take me back"))
            {
                return true;
            }
            else
            {
                GitLocksDisplay locksWindow = (GitLocksDisplay)EditorWindow.GetWindow(typeof(GitLocksDisplay), false, "Git Locks");
                return false;
            }
        }
        else
        {
            return true;
        }
    }

    private static void DebugLog(string s)
    {
        if (EditorPrefs.GetBool("gitLocksDebugMode", false))
        {
            UnityEngine.Debug.Log(s);
        }
    }
}