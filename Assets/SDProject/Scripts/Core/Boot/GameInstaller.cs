// Assets/SDProject/Scripts/Core/Boot/GameInstaller.cs
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SDProject.Core.Boot
{
    /// <summary>
    /// Minimal boot installer.
    /// Single responsibility: load the next scene (e.g., "Battle") from Boot scene.
    /// Extend later for settings/saves/localization/Addressables.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameInstaller : MonoBehaviour
    {
        [SerializeField] private string battleSceneName = "Battle"; // must match the Scene name

        private void Start()
        {
            if (string.IsNullOrWhiteSpace(battleSceneName))
            {
                Debug.LogError("[Boot] Target scene name is empty.");
                return;
            }

#if UNITY_EDITOR
            // Warn if Boot is not the first scene in Build Settings (editor only).
            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0 || !scenes[0].path.EndsWith("/Boot.unity"))
                Debug.LogWarning("[Boot] Boot is not the first scene in Build Settings.");
#endif

            // Safety: let user know if the scene isn't added to Build Settings.
            if (!Application.CanStreamedLevelBeLoaded(battleSceneName))
                Debug.LogWarning($"[Boot] Scene '{battleSceneName}' is not in Build Settings (File > Build Settings¡¦).");

            Debug.Log($"[Boot] Loading scene: {battleSceneName}");
            // KISS: blocking load is fine for now. Replace with async when you add a loading UI.
            SceneManager.LoadScene(battleSceneName);
        }

        // Example for future use:
        // private IEnumerator LoadNextAsync()
        // {
        //     var op = SceneManager.LoadSceneAsync(battleSceneName);
        //     while (!op.isDone) yield return null;
        // }
    }
}