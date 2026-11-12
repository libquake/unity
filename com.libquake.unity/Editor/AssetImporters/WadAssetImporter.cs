using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace LibQuake
{
    /// <summary>
    /// Imports WAD2 files through libquake as a WadTexture2DCollection with one entry per miptex: the texture, an
    /// emission map for textures with fullbright pixels and the cloud layer of skies (the texture is the back
    /// layer).
    /// </summary>
    [ScriptedImporter(4, "wad", AllowCaching = true)]
    public sealed class WadAssetImporter : ScriptedImporter
    {
        [Header("Export")]
        public string folder = "Assets/textures";
        [Space(10)]
        [InspectorButton("ExportTextures")]
        public bool exportTextures;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var wadCollection = ScriptableObject.CreateInstance<WadTexture2DCollection>();
            wadCollection.wadName = ctx.assetPath;

            using (var wad = new NativeQFWad())
            {
                // libquake rows go top to bottom unless flipped, Unity's bottom to top
                wad.Load(ctx.assetPath, flipHorizontal: true);
                for (int i = 0; i < wad.TextureCount; i++)
                {
                    var tex = wad.GetTexture(i);
                    if (tex.Data.Length == 0)
                    {
                        ctx.LogImportWarning($"[LibQuake] Texture '{tex.Name}' has no pixel data, skipped");
                        continue;
                    }

                    wadCollection.textures.Add(new WadTexture2DCollection.Entry
                    {
                        name = tex.Name,
                        texture = AddTexture(ctx, tex.Name, tex, tex.Data),
                        emission = tex.HasEmission
                            ? AddTexture(ctx, tex.Name + WadTexture2DCollection.EmissionSuffix, tex, tex.Emission)
                            : null,
                        skyFront = tex.SkyFront.Length > 0
                            ? AddTexture(ctx, tex.Name + WadTexture2DCollection.SkyFrontSuffix, tex, tex.SkyFront)
                            : null,
                        hasAlpha = tex.HasAlpha
                    });
                }
            }

            ctx.AddObjectToAsset("collection", wadCollection);
            ctx.SetMainObject(wadCollection);
            MaterialManager.Instance.AddWad(wadCollection);
        }

        private static Texture2D AddTexture(AssetImportContext ctx, string name, WadTexture tex, byte[] rgba)
        {
            var image = CreateTexture(name, tex, rgba);
            ctx.AddObjectToAsset(name, image);
            return image;
        }

        private static Texture2D CreateTexture(string name, WadTexture tex, byte[] rgba)
        {
            var image = new Texture2D((int)tex.Width, (int)tex.Height, TextureFormat.RGBA32, true)
            {
                name = name,
                filterMode = FilterMode.Point
            };
            image.SetPixelData(rgba, 0);
            image.Apply(true);
            return image;
        }

        public void ExportTextures()
        {
            var collection = AssetDatabase.LoadAssetAtPath<WadTexture2DCollection>(assetPath);
            if (collection == null)
            {
                Debug.LogWarning($"[LibQuake] {assetPath} has not been imported yet, nothing to export.");
                return;
            }

            folder = folder.Trim('/').TrimEnd('\\');
            Directory.CreateDirectory(folder);
            foreach (var tex in collection.AllTextures())
            {
                var fullPath = folder + "/" + tex.name.Replace("*", "").Replace("+", "") + ".png";
                File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            }
        }
    }
}
