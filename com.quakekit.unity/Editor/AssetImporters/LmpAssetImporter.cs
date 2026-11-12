using UnityEditor.AssetImporters;
using UnityEngine;
using System.IO;

namespace QuakeKit
{
    /// <summary>
    /// Imports .lmp palettes: 256 RGB triplets.
    /// </summary>
    [ScriptedImporter(1, "lmp", AllowCaching = true)]
    public sealed class LmpAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var bytes = File.ReadAllBytes(ctx.assetPath);
            var pal = ScriptableObject.CreateInstance<LmpPalette>();
            for (int i = 0; i + 2 < bytes.Length; i += 3)
            {
                var color = (Color)new Color32(bytes[i], bytes[i + 1], bytes[i + 2], 255);
                int index = i / 3;
                pal.colors.Add(color);
                if (index == LmpPalette.TransparentIndex)
                    pal.transparentColor = color;
                else if (index >= LmpPalette.FullbrightFirst)
                    pal.brightColors.Add(color);
            }

            ctx.AddObjectToAsset("Palette", pal);
            ctx.SetMainObject(pal);
        }
    }
}
