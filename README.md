# Unity Git Locks

Provides an extensive Unity integration for Git LFS locks, which are are essentials when working in teams to prevent conflicts, especially on binary files.

<img src=".docs\img\lockswindow.PNG" alt="Locks window" width="50%"/>

## Philosophy

The plugin uses simple calls to the Git LFS locks commands, which means you have to make sure that your setup is configured correctly to use LFS locks through the CLI. It also makes it independent from any authentication process: if you can use it through command line it will work in Unity!

This tool is intended to be used by developers, designers and artists alike, providing a flexible, easy and safe way of working as a team.

&nbsp;
&nbsp;

## How to use

### Installation

The easiest way is probably to add this repository as a package, either:

• [Automatically](https://docs.unity3d.com/Manual/upm-ui-giturl.html) (with url https://github.com/TomDuchene/unity-git-locks.git)

• Manually, by cloning this repository in your repository's Packages folder

### Repository configuration recommendations

We recommend that you setup your repository to **not** set as read-only the files that haven't been locked:

`git config lfs.setlockablereadonly false`

This allows users to modify any file and only prevents them from pushing changes to a locked file. The tool displays (opt-out) warnings when modifying a file that has been locked by someone else.

It also means you won't have to declare which file types are lockable.

You could in theory use the tool with the read-only mode enabled but it has not been tested.

### Initial setup

The only thing you absolutely need to setup before use is your provider's (e.g. Github) username, so the tool knows that it's you.
You have to input it in the Preferences window (`Edit/Preferences/Git Locks/Git host username`), which is also accessible through the "Git Locks" window.

<img src=".docs\img\username.PNG" alt="Set Username" width="50%" />

### Troubleshooting

Every git setup is a little bit different, so here's a [troubleshooting wiki page](https://github.com/TomDuchene/unity-git-locks/wiki/Troubleshooting) to help if you have errors.

&nbsp;
&nbsp;

## Features

### Main window

<img src=".docs\img\lockswindow.PNG" alt="Main locks window" width="50%" />

Accessible through `Window/Git Locks`, this window will show you :

-   Refresh time information
-   A setup button to go back to the preferences
-   A button to manually trigger the locks refresh
-   An object field and button to lock an asset
-   A checkbox to select/unselect all your locks at once
-   A button to unlock all selected locks
-   A view of all the objects you have locked, with clickable object fields that focus the object in the project window, hoverable paths, and unlock buttons to release the lock on a file
-   A similar view of the locks owned by other team members

### Contextual menus

<img src=".docs\img\context_hierarchy.jpg" alt="Hierarchy" width="40%" /><img src=".docs\img\context_project.jpg" alt="Project" width="50%" />

On a right-click, you can lock/unlock any file in the project window, and lock/unlock prefabs in the hierarchy window.

Multi-selection and folder selection are supported, provided that the files in the selection are all lockable / all unlockable.

In the project window, you can also show the file's git history, in a terminal (default) or in a browser (configurable through the settings).

### Lock icons

Lock icons are displayed on all locked files and folders in the Git locks, Project, and Hierarchy windows. They can be clicked to obtain information on their status and unlock them directly (if they are yours).

All locks are color-coded :

<img src="Editor\Textures\greenLock.png" alt="Lock" width="20"/>**File locked by you**

You’re safe working on it (don’t forget to unlock it when you’re done). Also displayed on a folder containing at least one of your locks.

<img src="Editor\Textures\orangeLock.png" alt="Lock" width="20"/>**File locked by someone else**

You shouldn’t modify it (if you need to you can ask the owner of the lock to release it, if he/she’s drinking coconut water in Miami for the next two months you could probably force the unlock in the Git Locks window). Also displayed on a folder containing at least one of someone else's locks.

<img src="Editor\Textures\redLock.png" alt="Lock" width="20"/>**File locked by someone else conflicting with your changes**

Someone has locked this file but you’ve made changes to it anyway which you will not be able to push (if you’re just testing things out it’s ok, but you’ll have to discard changes before you push)

<img src="Editor\Textures\mixedLock.png" alt="Lock" width="20"/>**Folder containing locks by you and someone else**

This folder contains both locks of yours and others.

### Preferences

<img src=".docs\img\preferences.PNG" alt="Set Username" width="50%" />

Many options are available to customize your use of the tool:

- **Max number of files grouped per request:** the bigger the number, the faster the overall locking/unlocking operation will be when processing dozens of files. If you go too high it might trigger the process timeout. Some users have errors where only one file can be processed at a time, it this case keep the value at 1
- **Auto refresh toggle and time:** the refresh is done asynchronously, so you can set it pretty low if there are a lot of people working simultaneously. It is recommended to keep the auto refresh on.
- **Notifications:** while not mandatory, most are really important for a smooth experience
- **Other branches to check:** what branches to check in addition to the current one when verifying that the file you're about to lock hasn't been modified on the server. No spaces, comma separated.
- **Number of my locks displayed:** used to show a minimum number of your own locks before the others'
- **Colorblind mode:** changes all lock icons to have different shapes and a more inclusive color scheme
- **Show debug logs:** show additional logs, especially the commands called
- **Show Force buttons:** display additional Force Unlock buttons, which will probably work only if you have the rights to do so
- **Show file history in browser:** Instead of showing the file history in a terminal, show it in your browser. You have to enter a URL corresponding to you host's format, for example "https://github.com/MyUserName/MyRepo/blob/$branch/$assetPath" (GitHub)
- **Git version and update button:** you'll need to update if the version is too old
- **Setup credentials manager button:** helps some users configure their CLI to authenticate if their credentials manager is not set correctly

### Keyboard shortcuts

In the project window, you can use Ctrl + Maj + **L** or **U** to **L**ock/**U**nlock a file or selection of files.

If you prefer having shortcuts to lock/unlock prefabs in the hierarchy, you can add some in `Edit / Shortcuts / “Main Menu/GameObject/Git LFS Lock” (or “Unlock”)`

&nbsp;
&nbsp;

## Misc

This tool has been created by [Tom Duchêne](http://tomduchene.fr) at [Tactical Adventures](http://tactical-adventures.com), and is under the M.I.T. license.
We might examine bug reports and pull requests but we can't offer you any kind of warranty or active support.
We hope this tool helps your team as much as it helped ours ♥

