using InsaneScatterbrain.ScriptGraph.Editor;
using UnityEngine;

namespace InsaneScatterbrain.MapGraph.Editor
{
    [ScriptNodeView(typeof(PerlinNoiseFillTextureNode))]
    public class PerlinNoiseFillTextureNodeView : ScriptNodeView
    {
        public PerlinNoiseFillTextureNodeView(PerlinNoiseFillTextureNode fillTextureNode, ScriptGraphView graphView) : base(fillTextureNode, graphView)
        {
            this.AddPreview<PerlinNoiseFillTextureNode>(GetPreviewTexture);
        }

        private Texture2D GetPreviewTexture(PerlinNoiseFillTextureNode node) => node.TextureData.ToTexture2D();
    }
}