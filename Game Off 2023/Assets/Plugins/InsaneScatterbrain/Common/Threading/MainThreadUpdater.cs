#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

namespace InsaneScatterbrain.Threading
{
    /// <summary>
    /// This class makes sure that the main thread command handler is actually run on update.
    /// </summary>
    public class MainThreadUpdater : MonoBehaviour
    {
        private static MainThreadUpdater instance;
    
#if UNITY_EDITOR
        /// <summary>
        /// Initializes the main thread updater in the editor.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void InitializeEditor()
        {
            EditorApplication.update += () =>
            {
                if (Application.isPlaying) return;

                MainThread.Update();
            };
        }
#endif
    
        /// <summary>
        /// Initializes the main thread updater in a standalone build.
        /// </summary>
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MainThreadUpdater>();
            }

            if (instance != null) return;
            
            var gameObject = new GameObject("[Main Thread Updater]")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            
            gameObject.AddComponent<MainThreadUpdater>();
        }
    
        private void Update()
        {
            MainThread.Update(); 
        }
    }
}
