using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace QuakeKit
{
    /// <summary>
    /// Finds the textures and materials for map textures and builds the materials of an import.
    /// Lookup order: custom material in the material folder, texture in the texture folder, imported WADs
    /// (the ones the map asks for first).
    /// </summary>
    public class MaterialManager
    {
        private const string Pkgpath = "Packages/com.quakekit.unity/Assets/Materials/";

        private static MaterialManager _instance;
        private Material _baseMaterial;
        private Material _skyMaterial;
        private Material _alphaMaterial;
        private Shader _lightmappedShader;
        private string _baseTextureParameter;
        private readonly List<WadTexture2DCollection> _wadCollections = new List<WadTexture2DCollection>();

        public static MaterialManager Instance => _instance ??= new MaterialManager();

        private MaterialManager()
        {
            SetupBaseMaterials();
            foreach (var guid in AssetDatabase.FindAssets("t:WadTexture2DCollection"))
            {
                var wad = AssetDatabase.LoadAssetAtPath<WadTexture2DCollection>(AssetDatabase.GUIDToAssetPath(guid));
                if (wad != null)
                    _wadCollections.Add(wad);
            }
        }

        public Material GetBaseMaterial()
        {
            return _baseMaterial;
        }

        public void AddWad(WadTexture2DCollection wad)
        {
            if (!_wadCollections.Contains(wad))
                _wadCollections.Add(wad);
        }

        /// <summary>
        /// The texture a map texture will use, null if there is none. Used for the texture size.
        /// </summary>
        public Texture GetTexture(string name, QLibTextureFlags flags, QKitMapConfigData config,
            IReadOnlyList<string> priorityWadNames)
        {
            var found = Find(name, flags, config, priorityWadNames);
            var tex = BaseTexture(found.Template);
            return tex != null ? tex : found.Texture;
        }

        /// <summary>
        /// A new material for a map texture, the caller owns it. flags are libquake's (sky and alpha follow the name).
        /// Skies get the sky material with both layers. '{' textures get the alpha material, culled unless the config
        /// asks for double sided alpha. With lightmapped set, materials based on the package base or alpha material
        /// use the QuakeKit/Lightmapped shader. Custom materials from the material folder keep their shader; they can
        /// read _QKitLightmap from uv2 themselves.
        /// </summary>
        public Material CreateMaterial(string name, QLibTextureFlags flags, QKitMapConfigData config,
            IReadOnlyList<string> priorityWadNames, bool lightmapped)
        {
            var found = Find(name, flags, config, priorityWadNames);
            var mat = new Material(found.Template) { name = "mat_" + name };

            if (found.Template == _skyMaterial)
            {
                mat.SetTexture("_MainTex", found.Texture);
                mat.SetTexture("_SkyFront", found.SkyFront);
                return mat;
            }

            if (found.Texture != null && BaseTexture(mat) == null)
                mat.SetTexture(_baseTextureParameter, found.Texture);

            if (found.Emission != null)
                SetEmissionMap(mat, found.Emission);

            bool alpha = (flags & QLibTextureFlags.Alpha) != 0;
            if (lightmapped)
            {
                mat.name += "_lm";
                if (!found.IsCustom && _lightmappedShader != null)
                {
                    var albedo = BaseTexture(mat);
                    var emission = GetEmissionMap(mat);
                    mat.shader = _lightmappedShader;
                    mat.SetTexture("_MainTex", albedo);
                    mat.SetTexture("_EmissionMap", emission);
                    if (alpha)
                        EnableAlphaTest(mat);
                }
            }

            if (alpha && !found.IsCustom)
                SetDoubleSided(mat, config.doubleSidedAlpha);

            return mat;
        }

        private struct Lookup
        {
            public Material Template;
            public bool IsCustom;
            public Texture2D Texture;
            public Texture2D Emission;
            public Texture2D SkyFront;
        }

        private Lookup Find(string name, QLibTextureFlags flags, QKitMapConfigData config,
            IReadOnlyList<string> priorityWadNames)
        {
            var found = new Lookup { Template = _baseMaterial };
            // there is no sky material for every pipeline
            if ((flags & QLibTextureFlags.Sky) != 0 && _skyMaterial != null)
                found.Template = _skyMaterial;
            else if ((flags & QLibTextureFlags.Alpha) != 0 && _alphaMaterial != null)
                found.Template = _alphaMaterial;

            if (!string.IsNullOrEmpty(config.materialFolder))
            {
                var materialPath = QPathTools.GetTextureAssetPath(name, config.materialFolder, "Material");
                var custom = materialPath != "" ? AssetDatabase.LoadAssetAtPath<Material>(materialPath) : null;
                if (custom != null)
                {
                    found.Template = custom;
                    found.IsCustom = true;
                }
            }

            if (found.Template == null)
                throw new InvalidOperationException($"QuakeKit base material not found in {Pkgpath}");

            if (!string.IsNullOrEmpty(config.textureFolder))
            {
                var texturePath = QPathTools.GetTextureAssetPath(name, config.textureFolder, "Texture2D");
                if (texturePath != "")
                {
                    found.Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    // exported wad textures come with "<name>_emission" and "<name>_front" next to them
                    found.Emission = LoadFolderTexture(name + WadTexture2DCollection.EmissionSuffix, config);
                    found.SkyFront = LoadFolderTexture(name + WadTexture2DCollection.SkyFrontSuffix, config);
                }
            }

            if (found.Texture == null)
            {
                var wad = FindWad(name, priorityWadNames);
                var entry = wad != null ? wad.Find(name) : null;
                if (entry != null)
                {
                    found.Texture = entry.texture;
                    found.Emission = entry.emission;
                    found.SkyFront = entry.skyFront;
                }
            }

            return found;
        }

        private static Texture2D LoadFolderTexture(string name, QKitMapConfigData config)
        {
            var path = QPathTools.GetTextureAssetPath(name, config.textureFolder, "Texture2D");
            return path != "" ? AssetDatabase.LoadAssetAtPath<Texture2D>(path) : null;
        }

        /// <summary>
        /// The first wad the map asks for that has the texture, otherwise any wad that has it.
        /// </summary>
        private WadTexture2DCollection FindWad(string textureName, IReadOnlyList<string> priorityWadNames)
        {
            _wadCollections.RemoveAll(w => w == null);
            if (priorityWadNames != null)
            {
                foreach (var wadName in priorityWadNames)
                {
                    var file = Path.GetFileNameWithoutExtension(wadName);
                    foreach (var wad in _wadCollections)
                    {
                        if (string.Equals(Path.GetFileNameWithoutExtension(wad.wadName), file,
                                StringComparison.OrdinalIgnoreCase) && wad.Find(textureName) != null)
                            return wad;
                    }
                }
            }

            foreach (var wad in _wadCollections)
            {
                if (wad.Find(textureName) != null)
                    return wad;
            }

            return null;
        }

        private Texture BaseTexture(Material mat) =>
            mat.HasProperty(_baseTextureParameter) ? mat.GetTexture(_baseTextureParameter) : null;

        /// <summary>
        /// Fullbright pixels as emission, for the built-in Standard, URP Lit and HDRP Lit shaders. A material that
        /// already has an emission map keeps it.
        /// </summary>
        private static void SetEmissionMap(Material mat, Texture2D emission)
        {
            if (mat.HasProperty("_EmissionMap"))
            {
                if (mat.GetTexture("_EmissionMap") != null)
                    return;
                mat.SetTexture("_EmissionMap", emission);
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", Color.white);
                mat.EnableKeyword("_EMISSION");
                // the lightmap comes from libquake, unity's GI does not need to know about the emission
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else if (mat.HasProperty("_EmissiveColorMap"))
            {
                if (mat.GetTexture("_EmissiveColorMap") != null)
                    return;
                mat.SetTexture("_EmissiveColorMap", emission);
                if (mat.HasProperty("_EmissiveColor"))
                    mat.SetColor("_EmissiveColor", Color.white);
                mat.EnableKeyword("_EMISSIVE_COLOR_MAP");
            }
        }

        /// <summary>
        /// Alpha test on the QuakeKit/Lightmapped shader: '{' textures are transparent where alpha is 0.
        /// </summary>
        private static void EnableAlphaTest(Material mat)
        {
            mat.SetFloat("_AlphaTest", 1);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
        }

        /// <summary>
        /// Culling of alpha materials. Quake culls back faces: a fence brush has '{' on both sides, drawing both
        /// would show the far side through the holes. Double sided is for '{' on one face and skip on the other.
        /// Covers QuakeKit/Lightmapped, the built-in alpha shader, URP Lit (_Cull) and HDRP Lit.
        /// </summary>
        private static void SetDoubleSided(Material mat, bool doubleSided)
        {
            var cull = (float)(doubleSided ? CullMode.Off : CullMode.Back);
            if (mat.HasProperty("_Cull"))
                mat.SetFloat("_Cull", cull);
            if (mat.HasProperty("_DoubleSidedEnable"))
            {
                mat.SetFloat("_DoubleSidedEnable", doubleSided ? 1 : 0);
                mat.SetFloat("_CullMode", cull);
                mat.SetFloat("_CullModeForward", cull);
                if (doubleSided)
                    mat.EnableKeyword("_DOUBLESIDED_ON");
                else
                    mat.DisableKeyword("_DOUBLESIDED_ON");
            }

            mat.doubleSidedGI = doubleSided;
        }

        private static Texture GetEmissionMap(Material mat)
        {
            if (mat.HasProperty("_EmissionMap"))
                return mat.GetTexture("_EmissionMap");
            if (mat.HasProperty("_EmissiveColorMap"))
                return mat.GetTexture("_EmissiveColorMap");
            return null;
        }

        private void SetupBaseMaterials()
        {
            QPathTools.GetPipelineFolder(out var pipeline, out _baseTextureParameter);
            _baseMaterial = AssetDatabase.LoadAssetAtPath<Material>(Pkgpath + pipeline + "/Base.mat");
            _skyMaterial = AssetDatabase.LoadAssetAtPath<Material>(Pkgpath + pipeline + "/Sky.mat");
            _alphaMaterial = AssetDatabase.LoadAssetAtPath<Material>(Pkgpath + pipeline + "/Alpha.mat");

            // the lightmapped shader is a plain cg pass, hdrp cannot render it
            if (pipeline != "HDRP")
                _lightmappedShader = AssetDatabase.LoadAssetAtPath<Shader>(Pkgpath + "Base/Lightmapped.shader");
        }
    }
}
