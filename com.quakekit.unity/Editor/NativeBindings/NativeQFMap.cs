using System;
using System.Collections.Generic;

#nullable enable

namespace QuakeKit
{
    /// <summary>
    /// A .map file loaded through libquake.
    /// Order of calls: Load, RegisterTextureSize / SetFaceType(s), GenerateGeometry, ExportData, GetLightmap.
    /// All positions are Quake map coordinates (z up, map units).
    /// </summary>
    public class NativeQFMap : IDisposable
    {
        private IntPtr _level = IntPtr.Zero;
        private bool _geometryGenerated;

        public List<string> TextureNames { get; private set; } = new List<string>();
        public List<string> RequiredWads { get; private set; } = new List<string>();
        private Dictionary<string, QLibTextureFlags> _textureFlags = new Dictionary<string, QLibTextureFlags>();
        public List<MapSolidEntity> SolidEntities { get; } = new List<MapSolidEntity>();
        public List<MapPointEntity> PointEntities { get; } = new List<MapPointEntity>();

        /// <summary>
        /// Parses a .map file. Throws a QLibException if the file cannot be loaded.
        /// </summary>
        /// <param name="luxelSize">map units per lightmap texel, 16 is what Quake uses</param>
        public void Load(string mapPath, bool enableCSG = true, bool convertToOpenGL = false, float luxelSize = 16f,
            uint lightmapPadding = 1)
        {
            QLibNative.EnsureCompatible();
            Release();

            QLibNative.QLibMap_GetDefaultOptions(out var options);
            options.enableCSG = enableCSG ? 1u : 0u;
            options.convertToOpenGL = convertToOpenGL ? 1u : 0u;
            options.luxelSize = luxelSize;
            options.lightmapPadding = lightmapPadding;
            QLibNative.Check(QLibNative.QLibMap_Load(mapPath, ref options, out _level), $"Loading {mapPath}");

            TextureNames = LevelReader.TextureNames(_level);
            _textureFlags = LevelReader.TextureFlags(_level);
            RequiredWads = LevelReader.RequiredWads(_level);
        }

        public List<string> GetRequiredWads()
        {
            EnsureLoaded();
            return new List<string>(RequiredWads);
        }

        public List<string> GetTextureNames()
        {
            EnsureLoaded();
            return new List<string>(TextureNames);
        }

        /// <summary>
        /// What libquake knows about a texture from its name (Sky, Alpha, Liquid), None for unknown names.
        /// </summary>
        public QLibTextureFlags GetTextureFlags(string textureName)
        {
            EnsureLoaded();
            return _textureFlags.TryGetValue(textureName, out var flags) ? flags : QLibTextureFlags.None;
        }

        /// <summary>
        /// Size of a texture, needed for the texture uvs. Names are case insensitive.
        /// </summary>
        public void RegisterTextureSize(string textureName, uint width, uint height)
        {
            EnsureBeforeGeometry(nameof(RegisterTextureSize));
            QLibNative.Check(QLibNative.QLibMap_SetTextureSize(_level, textureName, width, height), "SetTextureSize");
        }

        public void RegisterTextureSizesFromWad(NativeQFWad wad)
        {
            EnsureBeforeGeometry(nameof(RegisterTextureSizesFromWad));
            foreach (var texture in wad.Textures)
                RegisterTextureSize(texture.Name, texture.Width, texture.Height);
        }

        /// <summary>
        /// Marks all faces with this texture (case insensitive). clip, trigger, skip, hint, hintskip, nodraw, caulk and
        /// origin are already handled by libquake.
        /// </summary>
        public void SetFaceType(string textureName, SurfaceType type)
        {
            EnsureBeforeGeometry(nameof(SetFaceType));
            QLibNative.Check(QLibNative.QLibMap_SetFaceType(_level, textureName, (uint)type), "SetFaceType");
        }

        /// <summary>
        /// SetFaceType for a ';' separated list of texture names.
        /// </summary>
        public void SetFaceTypes(string textureNames, SurfaceType type)
        {
            if (string.IsNullOrEmpty(textureNames))
                return;
            foreach (var name in textureNames.Split(';'))
            {
                var trimmed = name.Trim();
                if (trimmed.Length > 0)
                    SetFaceType(trimmed, type);
            }
        }

        /// <summary>
        /// CSG, meshes and lightmap uvs.
        /// </summary>
        public void GenerateGeometry()
        {
            EnsureLoaded();
            QLibNative.Check(QLibNative.QLibMap_GenerateGeometry(_level), "GenerateGeometry");
            _geometryGenerated = true;
        }

        /// <summary>
        /// Lights baked in addition to the map's light entities (e.g. Unity lights), in map coordinates.
        /// </summary>
        public void SetExtraLights(QLibLight[] lights)
        {
            EnsureLoaded();
            QLibNative.Check(QLibNative.QLibMap_SetExtraLights(_level, lights, (uint)(lights?.Length ?? 0)),
                "SetExtraLights");
        }

        /// <summary>
        /// Copies all entities with their meshes, attributes and collision hulls.
        /// </summary>
        public void ExportData()
        {
            EnsureGeometry();
            TextureNames = LevelReader.TextureNames(_level);
            _textureFlags = LevelReader.TextureFlags(_level);
            RequiredWads = LevelReader.RequiredWads(_level);

            SolidEntities.Clear();
            uint solidCount = QLibNative.QLibLevel_GetSolidEntityCount(_level);
            for (uint i = 0; i < solidCount; i++)
                SolidEntities.Add(LevelReader.Solid(_level, i, TextureNames));

            PointEntities.Clear();
            uint pointCount = QLibNative.QLibLevel_GetPointEntityCount(_level);
            for (uint i = 0; i < pointCount; i++)
                PointEntities.Add(LevelReader.Point(_level, i));
        }

        /// <summary>
        /// Bakes the lightmap on the first call (map lights + extra lights) and returns the combined lightmap and its
        /// light style layers. Null if the map has no lit surfaces.
        /// </summary>
        public LevelLightmap? GetLightmap()
        {
            EnsureGeometry();
            return LevelReader.Lightmap(_level);
        }

        public void SetLightStyle(int style, string pattern)
        {
            EnsureLoaded();
            QLibNative.Check(QLibNative.QLibLevel_SetLightStyle(_level, style, pattern), "SetLightStyle");
        }

        /// <summary>
        /// Evaluates the light style patterns at a time in seconds. True if the combined lightmap changed.
        /// </summary>
        public bool UpdateLightStyles(double time)
        {
            EnsureGeometry();
            QLibNative.Check(QLibNative.QLibLevel_UpdateLightStyles(_level, time, out var changed), "UpdateLightStyles");
            return changed != 0;
        }

        private void EnsureLoaded()
        {
            if (_level == IntPtr.Zero)
                throw new InvalidOperationException("Map not loaded. Call Load() first.");
        }

        private void EnsureGeometry()
        {
            EnsureLoaded();
            if (!_geometryGenerated)
                throw new InvalidOperationException("Geometry not generated. Call GenerateGeometry() first.");
        }

        private void EnsureBeforeGeometry(string what)
        {
            EnsureLoaded();
            if (_geometryGenerated)
                throw new InvalidOperationException($"{what} has to be called before GenerateGeometry().");
        }

        private void Release()
        {
            if (_level != IntPtr.Zero)
            {
                QLibNative.QLibLevel_Destroy(_level);
                _level = IntPtr.Zero;
            }

            _geometryGenerated = false;
            SolidEntities.Clear();
            PointEntities.Clear();
        }

        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        ~NativeQFMap()
        {
            Release();
        }
    }
}
