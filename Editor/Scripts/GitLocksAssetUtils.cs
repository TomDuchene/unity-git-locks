using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GitLocksAssetUtils
{
	public static string GetAssetPath(this UnityEngine.Object obj)
	{
#if UNITY_6000_5_OR_NEWER
        return AssetDatabase.GetAssetPath(obj.GetEntityId());
#else
		return AssetDatabase.GetAssetPath(obj.GetInstanceID());
#endif
	}

	public static string GetObjectFromId(this UnityEngine.Object obj)
	{
#if UNITY_6000_5_OR_NEWER
		return AssetDatabase.GetAssetPath(obj.GetEntityId());
#else
		return AssetDatabase.GetAssetPath(obj.GetInstanceID());
#endif
	}

	public static string GetAssetPathFromPrefabGameObject(UnityEngine.Object obj)
	{
#if UNITY_6000_5_OR_NEWER
		var gameObject = EditorUtility.EntityIdToObject(obj.GetEntityId()) as GameObject;
#else
		var gameObject = EditorUtility.InstanceIDToObject(obj.GetInstanceID()) as GameObject;
#endif

		return GetAssetPathFromPrefabGameObject(gameObject);
	}

#if UNITY_6000_5_OR_NEWER
	public static string GetAssetPathFromPrefabGameObject(EntityId id)
	{
		var gameObject = EditorUtility.EntityIdToObject(id) as GameObject;
		return GetAssetPathFromPrefabGameObject(gameObject);
	}
#else
	public static string GetAssetPathFromPrefabGameObject(int id)
	{
		var gameObject = EditorUtility.InstanceIDToObject(id) as GameObject;
		return GetAssetPathFromPrefabGameObject(gameObject);
	}
#endif

	public static string GetAssetPathFromPrefabGameObject(GameObject gameObject)
	{
		if (gameObject != null)
		{
			return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
		}

		return string.Empty;
	}

#if UNITY_6000_5_OR_NEWER
	public static bool IsObjectPrefabRoot(EntityId id)
	{
		var gameObject = EditorUtility.EntityIdToObject(id) as GameObject;
		return IsObjectPrefabRoot(gameObject);
	}
#else
	public static bool IsObjectPrefabRoot(int id)
	{
		var gameObject = EditorUtility.InstanceIDToObject(id) as GameObject;
		return IsObjectPrefabRoot(gameObject);
	}
#endif

	public static bool IsObjectPrefabRoot(GameObject gameObject)
	{
		if (gameObject != null)
		{
			return PrefabUtility.GetNearestPrefabInstanceRoot(gameObject) == gameObject;
		}

		return false;
	}

#if UNITY_6000_5_OR_NEWER
	public static Scene GetSceneFromId(EntityId entityId)
	{
		for (int index = 0; index < SceneManager.sceneCount; index++)
		{
			var scene = SceneManager.GetSceneAt(index);
			if (scene.handle.GetRawData() == EntityId.ToULong(entityId))
			{
				return scene;
			}
		}

		return default;
	}
#else
	public static Scene GetSceneFromId(int instanceID)
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
#endif

#if UNITY_6000_5_OR_NEWER
	public static List<string> SelectionToPaths()
	{
		List<string> paths = new List<string>();

		Dictionary<ulong, Scene> _loadedScenesByHash = new Dictionary<ulong, Scene>();
		int countLoaded = SceneManager.sceneCount;
		for (int i = 0; i < countLoaded; i++)
		{
			Scene loadedScene = SceneManager.GetSceneAt(i);
			_loadedScenesByHash[loadedScene.handle.GetRawData()] = loadedScene;
		}

		foreach (EntityId entityId in Selection.entityIds)
		{
			UnityEngine.Object obj = EditorUtility.EntityIdToObject(entityId);
			if (obj != null)
			{
				string path = GitLocksAssetUtils.GetAssetPathFromPrefabGameObject(obj.GetEntityId());
				paths.Add(path);
			}
			else if (_loadedScenesByHash.TryGetValue(EntityId.ToULong(entityId), out Scene scene))
			{
				paths.Add(scene.path);
			}
		}

		return paths;
	}
#else
	public static List<string> SelectionToPaths()
	{
		List<string> paths = new List<string>();

		Dictionary<int, Scene> _loadedScenesByHash = new Dictionary<int, Scene>();
		int countLoaded = SceneManager.sceneCount;
		for (int i = 0; i < countLoaded; i++)
		{
			Scene loadedScene = SceneManager.GetSceneAt(i);
			_loadedScenesByHash[loadedScene.GetHashCode()] = loadedScene;
		}

		foreach (int instanceID in Selection.instanceIDs)
		{
			UnityEngine.Object obj = EditorUtility.InstanceIDToObject(instanceID);
			if (obj != null)
			{
				string path = GitLocksAssetUtils.GetAssetPathFromPrefabGameObject(instanceID);
				paths.Add(path);
			}
			else if (_loadedScenesByHash.TryGetValue(instanceID, out Scene scene))
			{
				paths.Add(scene.path);
			}
		}

		return paths;
	}
#endif

}