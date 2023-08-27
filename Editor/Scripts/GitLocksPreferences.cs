// <copyright file="GitLocksPreferences.cs" company="Tom Duchene and Tactical Adventures">All rights reserved.</copyright>

using UnityEditor;
using UnityEngine;

public class GitLocksPreferences : SettingsProvider
{
    public GitLocksPreferences(string path, SettingsScope scope = SettingsScope.User) : base(path, scope)
    {
    }

    [SettingsProvider]
    public static SettingsProvider RegisterSettingsProvider()
    {
        var provider = new GitLocksPreferences("Preferences/Git Locks", SettingsScope.User);

        // Automatically extract all keywords from the Styles.
        provider.keywords = new string[]
        {
                                            "Enable Git LFS lock tool",
                                            "Git host username",
                                            "Auto refresh locks",
                                            "Refresh locks interval (minutes)",
                                            "Display locks conflict warning",
                                            "Warn if I still own locks on quit",
                                            "Minimum number of my locks to show"
        };
        return provider;
    }

    public override void OnGUI(string searchContext)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("General settings", EditorStyles.boldLabel);

        float previousLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUI.indentLevel++;

        GUILayout.Space(5);

        EditorGUI.BeginChangeCheck();
        bool enabled = EditorGUILayout.ToggleLeft(new GUIContent("Enable Git LFS locks tool"), EditorPrefs.GetBool("gitLocksEnabled"), GUILayout.Width(180));
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool("gitLocksEnabled", enabled);
        }

        if (enabled)
        {
            GUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            string username = EditorGUILayout.TextField(new GUIContent("Git host username"), EditorPrefs.GetString("gitLocksHostUsername"));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetString("gitLocksHostUsername", username);
            }

            if (GUILayout.Button("Go on github"))
            {
                UnityEngine.Application.OpenURL("https://github.com/");
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            int maxFilesNumPerRequest = EditorGUILayout.IntField(new GUIContent("Max number of files grouped per request"), EditorPrefs.GetInt("gitLocksMaxFilesNumPerRequest"));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetInt("gitLocksMaxFilesNumPerRequest", maxFilesNumPerRequest);
            }

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            bool autoRefresh = EditorGUILayout.ToggleLeft(new GUIContent("Auto refresh locks"), EditorPrefs.GetBool("gitLocksAutoRefreshLocks"), GUILayout.Width(135));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("gitLocksAutoRefreshLocks", autoRefresh);
            }

            EditorGUI.BeginDisabledGroup(!autoRefresh);
            EditorGUILayout.LabelField("every", GUILayout.Width(50));
            EditorGUI.BeginChangeCheck();
            int interval = EditorGUILayout.IntField(EditorPrefs.GetInt("gitLocksRefreshLocksInterval"), GUILayout.Width(40));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetInt("gitLocksRefreshLocksInterval", interval);
            }

            EditorGUILayout.LabelField("minutes", GUILayout.Width(70));

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            // Notifications
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Notifications", EditorStyles.boldLabel);
            previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            bool displayLocksConflictWarning = EditorGUILayout.ToggleLeft(new GUIContent("Warn if a file I modified is already locked"), EditorPrefs.GetBool("displayLocksConflictWarning"));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("displayLocksConflictWarning", displayLocksConflictWarning);
            }

            EditorGUI.BeginChangeCheck();
            bool warnIfIStillOwnLocksOnQuit = EditorGUILayout.ToggleLeft(new GUIContent("Warn if I still own locks on quit"), EditorPrefs.GetBool("warnIfIStillOwnLocksOnQuit"));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("warnIfIStillOwnLocksOnQuit", warnIfIStillOwnLocksOnQuit);
            }

            EditorGUI.BeginChangeCheck();
            bool warnIfFileHasBeenModifiedOnServer = EditorGUILayout.ToggleLeft(new GUIContent("Warn if the file has already been modified on server when locking"), EditorPrefs.GetBool("warnIfFileHasBeenModifiedOnServer"));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("warnIfFileHasBeenModifiedOnServer", warnIfFileHasBeenModifiedOnServer);
            }

            if (EditorPrefs.GetBool("warnIfFileHasBeenModifiedOnServer"))
            {
                EditorGUI.BeginChangeCheck();
                string branchesToCheck = EditorGUILayout.TextField(new GUIContent("Other branches to check (',' separated)"), EditorPrefs.GetString("gitLocksBranchesToCheck"));
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetString("gitLocksBranchesToCheck", branchesToCheck);
                }
            }

            EditorGUI.BeginChangeCheck();
            bool notifyNewLocks = EditorGUILayout.ToggleLeft(new GUIContent("Notify when there are new locks and when launching Unity"), EditorPrefs.GetBool("notifyNewLocks"));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("notifyNewLocks", notifyNewLocks);
            }

            // Display
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);
            previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            int numOfMyLocksDisplayed = EditorGUILayout.IntField(new GUIContent("Number of my locks displayed"), EditorPrefs.GetInt("numOfMyLocksDisplayed"));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetInt("numOfMyLocksDisplayed", numOfMyLocksDisplayed);
            }

            EditorGUI.BeginChangeCheck();
            bool colorblindMode = EditorGUILayout.ToggleLeft(new GUIContent("Colorblind mode"), EditorPrefs.GetBool("gitLocksColorblindMode"));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("gitLocksColorblindMode", colorblindMode);
                GitLocksDisplay.RefreshLockIcons();
            }

            EditorGUI.BeginChangeCheck();
            bool debugMode = EditorGUILayout.ToggleLeft(new GUIContent("Show debug logs"), EditorPrefs.GetBool("gitLocksDebugMode"));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("gitLocksDebugMode", debugMode);
            }

            EditorGUI.BeginChangeCheck();
            bool showForceButtons = EditorGUILayout.ToggleLeft(new GUIContent("Show Force buttons"), EditorPrefs.GetBool("gitLocksShowForceButtons"));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("gitLocksShowForceButtons", showForceButtons);
            }
        }

        GUILayout.Space(10);

        EditorGUI.indentLevel--;
        EditorGUIUtility.labelWidth = previousLabelWidth;

        // Misc
        EditorGUILayout.LabelField("Misc", EditorStyles.boldLabel);

        EditorGUIUtility.labelWidth = 250;
        EditorGUI.indentLevel++;

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        bool showHistoryInBrowser = EditorGUILayout.ToggleLeft(new GUIContent("Show file history in browser"), EditorPrefs.GetBool("gitLocksShowHistoryInBrowser"), GUILayout.Width(195));
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetBool("gitLocksShowHistoryInBrowser", showHistoryInBrowser);
        }

        EditorGUI.BeginDisabledGroup(!showHistoryInBrowser);
        EditorGUI.BeginChangeCheck();
        string url = EditorGUILayout.TextField(EditorPrefs.GetString("gitLocksShowHistoryInBrowserUrl"));
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString("gitLocksShowHistoryInBrowserUrl", url);
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        EditorGUI.indentLevel--;
        EditorGUIUtility.labelWidth = previousLabelWidth;

        // Git config and troubleshooting
        EditorGUILayout.LabelField("Git config and troubleshooting", EditorStyles.boldLabel);

        EditorGUIUtility.labelWidth = 250;
        EditorGUI.indentLevel++;

        EditorGUILayout.Space();

        EditorGUILayout.LabelField(new GUIContent(GitLocks.GetGitVersion()), GUILayout.Height(25));
        if (GitLocks.IsGitOutdated())
        {
            EditorGUILayout.LabelField(new GUIContent("Your git version seems outdated (2.30.0 minimum), you may need to update it and then setup the Credentials Manager for the authentication to work properly"), EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("Update Git for Windows"))
            {
                GitLocks.ExecuteProcessTerminal("git", "update-git-for-windows", true);
            }
        }

        if (GUILayout.Button("Setup credentials manager (when using HTTPS)"))
        {
            GitLocks.ExecuteProcessTerminalWithConsole("git", "config --local credential.helper manager");
        }

        EditorGUI.indentLevel--;
    }
}