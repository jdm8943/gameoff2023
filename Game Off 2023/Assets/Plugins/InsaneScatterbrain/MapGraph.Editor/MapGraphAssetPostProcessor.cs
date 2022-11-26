using System.Linq;
using System.Threading.Tasks;
using InsaneScatterbrain.Editor.Services;
using InsaneScatterbrain.ScriptGraph;
using InsaneScatterbrain.ScriptGraph.Editor;
using UnityEditor;

namespace InsaneScatterbrain.MapGraph.Editor
{
    internal class MapGraphAssetPostProcessor : AssetPostprocessor
    {
        private static async void ReloadWindows()
        {
            await Task.Delay(1);    // Wait a bit for the rename/move actions to complete properly. 
            ScriptGraphViewWindow.ReloadAll();
        }
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var movedAsset in movedAssets)
            {
                var graph = AssetDatabase.LoadAssetAtPath<ScriptGraphGraph>(movedAsset);

                if (graph == null) continue;

                ReloadWindows();
                break;
            }
            
            var mapGraphUpdated = false;
            foreach (var asset in importedAssets)
            {
                if (asset != $"Assets{MapGraphEditorInfo.VersionFilePathRelative}") continue;

                // If the version.txt file has been imported, that means either Map Graph was just installed or updated,
                // so we want to show the about window.
                mapGraphUpdated = true;
            }

            if (deletedAssets.Length > 0)
            {
                // Linked sets might have been deleted, make sure they're unlinked.
                var namedColorSets = Assets.Find<NamedColorSet>();
                foreach (var namedColorSet in namedColorSets)
                {
                    namedColorSet.Update();
                }

                // A graph used as sub graph may have been deleted, reload the editor windows so it's immediately shown
                // where these nodes are now missing, without having to reload the editor windows by hand.

                var instances = ScriptGraphViewWindow.Instances.ToArray();
                foreach (var instance in instances)
                {
                    if (instance == null) continue;
                    
                    if (instance.GraphView == null || instance.GraphView.Graph == null)
                    {
                        // This instance no longer exists, it might be the asset that's just been deleted. Close the
                        // corresponding windows.
                        instance.Close();
                    }
                    instance.Reload();
                }
            }

            if (!mapGraphUpdated) return;
            
            AboutWindow.ShowWindow();
        }
    }
}