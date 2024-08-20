using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LobsterFramework
{
	/// <summary>
	/// This class manages the scene loading and unloading.
	/// </summary>
	public class SceneLoader : MonoBehaviour
	{
		[SerializeField] List<string> scenesToLoad;

        private void Awake()
        {
			HashSet<string> items = new HashSet<string>(scenesToLoad);
			for (int i = 0;i < SceneManager.sceneCount;i++) {
				Scene scene = SceneManager.GetSceneAt(i);
				items.Remove(scene.name);
			}
			foreach(string item in items) { 
				SceneManager.LoadScene(item, LoadSceneMode.Additive);
			}
        }
	}
}
