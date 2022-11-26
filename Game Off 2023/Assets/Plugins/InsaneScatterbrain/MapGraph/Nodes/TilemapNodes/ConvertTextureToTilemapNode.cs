using System;
using InsaneScatterbrain.Extensions;
using InsaneScatterbrain.ScriptGraph;
using InsaneScatterbrain.Services;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace InsaneScatterbrain.MapGraph
{
    /// <summary>
    /// Generates a tilemap from a texture.
    /// </summary>
    [ScriptNode("Texture To Tilemap", "Tilemaps"), Serializable]
    public class ConvertTextureToTilemapNode : ProcessorNode
    {
        [InPort("Texture", typeof(TextureData), true), SerializeReference]
        private InPort textureIn = null;
         
        [InPort("Tileset", typeof(Tileset), true), SerializeReference]
        private InPort tilesetIn = null;
        
        [InPort("Tilemap", typeof(Tilemap), true), SerializeReference]
        private InPort tilemapIn = null;

        [InPort("Offset", typeof(Vector2Int)), SerializeReference]
        private InPort offsetIn = null;

        [InPort("Prevent Clear?", typeof(bool)), SerializeReference]
        private InPort preventClearIn = null;
        

        /// <inheritdoc cref="ProcessorNode.OnProcess"/>
        protected override void OnProcessMainThread()
        {
            var rng = Get<Rng>();
            var namedColorSet = Get<NamedColorSet>();
            
            var textureData = textureIn.Get<TextureData>();
            var tileset = tilesetIn.Get<Tileset>();
            
            var width = textureData.Width;
            var tilemap = tilemapIn.Get<Tilemap>();

            var offset = offsetIn.Get<Vector2Int>();
            var preventClear = preventClearIn.Get<bool>();
            
            if (!preventClear) tilemap.ClearAllTiles();
            
            var tiles = new TileBase[textureData.ColorCount];

            for (var i = 0; i < textureData.ColorCount; ++i)
            {
                var color = textureData[i];
                if (color.IsEqualTo(default)) continue; // Empty space, skip it 
                
                var x = i % width;
                var y = i / width;
                
                var tileType = namedColorSet.GetName(color);
                if (tileType == null)
                {
                    Debug.LogError($"Unknown color: {color}");
                    tilemap.SetTile(new Vector3Int(x, y, 0), null);
                    continue;
                }

                var tile = tileset.GetRandomObject(tileType, rng);
            
                tiles[i] = tile;
            }
            
            tilemap.SetTilesBlock(new BoundsInt(offset.x,offset.y,0,width, textureData.Height, 1), tiles);

#if UNITY_EDITOR
            EditorUtility.SetDirty(tilemap);
#endif
        }
    }
}