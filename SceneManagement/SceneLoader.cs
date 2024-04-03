using NUnit.Framework;
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
		[SerializeField] private SceneEventChannel loadChannel;
		[SerializeField] private SceneEventChannel unloadChannel;
		[SerializeField] List<string> scenesToLoad;

        private void Awake()
        {
            loadChannel.OnEventRaised += LoadScene;
            unloadChannel.OnEventRaised += UnloadScene;
			foreach (string scene in scenesToLoad)
			{
				if (!SceneManager.GetSceneByName(scene).isLoaded) {
                    SceneManager.LoadScene(scene, LoadSceneMode.Additive);
                }
			}
        }

		private void LoadScene(Scene scene)
		{
			SceneManager.LoadScene(scene.ToString(), LoadSceneMode.Additive);
		}

		private void UnloadScene(Scene scene)
		{
			SceneManager.UnloadSceneAsync(scene.ToString());
		}
	}
}
