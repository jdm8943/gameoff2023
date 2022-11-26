using InsaneScatterbrain.Extensions;
using InsaneScatterbrain.ScriptGraph.Editor;
using InsaneScatterbrain.Services;
using UnityEngine;

namespace InsaneScatterbrain.MapGraph.Editor
{
    [ScriptNodeView(typeof(RandomBspRoomsNode))]
    public class RandomBspRoomsNodeView : ScriptNodeView
    {
        public RandomBspRoomsNodeView(RandomBspRoomsNode node, ScriptGraphView graphView) : base(node, graphView)
        {
            this.AddPreview<RandomBspRoomsNode>(GetPreviewTexture);
        }

        private Texture2D GetPreviewTexture(RandomBspRoomsNode node)
        {
            var rootBounds = node.Tree.Root.Bounds;

            var texture = Texture2DFactory.CreateDefault(rootBounds.width, rootBounds.height);
            texture.Fill(Color.black);
                
            var roomBounds = node.Bounds;

            foreach (var bounds in roomBounds)
            {
                var x = bounds.x;
                var y = bounds.y;
                var width = bounds.width;
                var height = bounds.height;

                var fillColor = (Color32) Color.white;
                var colors = fillColor.CreateArray(width * height);
                    
                texture.SetPixels32(x, y, width, height, colors);
            }
                
            texture.Apply();

            return texture;
        }
    }
}