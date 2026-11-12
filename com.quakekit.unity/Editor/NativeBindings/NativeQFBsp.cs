using System;
using System.Collections.Generic;

#nullable enable

namespace QuakeKit
{
    /// <summary>
    /// A compiled .bsp file loaded through libquake. Positions are Quake map coordinates.
    /// </summary>
    public class NativeQFBsp : IDisposable
    {
        private IntPtr _level = IntPtr.Zero;

        public List<BspTexture> Textures { get; } = new List<BspTexture>();
        public List<MapSolidEntity> SolidEntities { get; } = new List<MapSolidEntity>();
        public List<MapPointEntity> PointEntities { get; } = new List<MapPointEntity>();

        /// <summary>
        /// Loads the file, geometry is available right away. Throws a QLibException on failure.
        /// </summary>
        public void Load(string bspPath, bool convertToOpenGL = false, bool loadTextureData = true)
        {
            QLibNative.EnsureCompatible();
            Release();
            QLibNative.QLibBsp_GetDefaultOptions(out var options);
            options.convertToOpenGL = convertToOpenGL ? 1u : 0u;
            options.loadTextureData = loadTextureData ? 1u : 0u;
            QLibNative.Check(QLibNative.QLibBsp_Load(bspPath, ref options, out _level), $"Loading {bspPath}");
        }

        /// <summary>
        /// Copies textures (with RGBA pixels when available) and all entities.
        /// </summary>
        public void ExportData()
        {
            EnsureLoaded();
            var names = LevelReader.TextureNames(_level);

            Textures.Clear();
            for (uint i = 0; i < names.Count; i++)
            {
                QLibNative.Check(QLibNative.QLibLevel_GetTextureInfo(_level, i, out var info), "GetTextureInfo");
                uint index = i;
                QLibResult Copy(QLibTextureLayer layer, byte[] px) =>
                    QLibNative.QLibLevel_CopyTextureLayer(_level, index, layer, px, (uint)px.Length);
                Textures.Add(new BspTexture(names[(int)i], info,
                    TextureReader.Layer(info, QLibTextureFlags.HasData, QLibTextureLayer.Albedo, Copy),
                    TextureReader.Layer(info, QLibTextureFlags.HasEmission, QLibTextureLayer.Emission, Copy),
                    TextureReader.Layer(info, QLibTextureFlags.HasSkyFront, QLibTextureLayer.SkyFront, Copy)));
            }

            SolidEntities.Clear();
            uint solidCount = QLibNative.QLibLevel_GetSolidEntityCount(_level);
            for (uint i = 0; i < solidCount; i++)
                SolidEntities.Add(LevelReader.Solid(_level, i, names));

            PointEntities.Clear();
            uint pointCount = QLibNative.QLibLevel_GetPointEntityCount(_level);
            for (uint i = 0; i < pointCount; i++)
                PointEntities.Add(LevelReader.Point(_level, i));
        }

        /// <summary>
        /// The lightmap stored in the bsp (and .lit file): Combined with the current light style values, Layers one
        /// per light style.
        /// </summary>
        public LevelLightmap? GetLightmap()
        {
            EnsureLoaded();
            return LevelReader.Lightmap(_level);
        }

        public void SetLightStyle(int style, string pattern)
        {
            EnsureLoaded();
            QLibNative.Check(QLibNative.QLibLevel_SetLightStyle(_level, style, pattern), "SetLightStyle");
        }

        public bool UpdateLightStyles(double time)
        {
            EnsureLoaded();
            QLibNative.Check(QLibNative.QLibLevel_UpdateLightStyles(_level, time, out var changed), "UpdateLightStyles");
            return changed != 0;
        }

        private void EnsureLoaded()
        {
            if (_level == IntPtr.Zero)
                throw new InvalidOperationException("BSP not loaded. Call Load() first.");
        }

        private void Release()
        {
            if (_level != IntPtr.Zero)
            {
                QLibNative.QLibLevel_Destroy(_level);
                _level = IntPtr.Zero;
            }

            Textures.Clear();
            SolidEntities.Clear();
            PointEntities.Clear();
        }

        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        ~NativeQFBsp()
        {
            Release();
        }
    }

    public class BspTexture
    {
        public string Name { get; }
        /// <summary>Sky: the size of one layer, half the width stored in the bsp.</summary>
        public uint Width { get; }
        public uint Height { get; }
        public QLibTextureFlags Flags { get; }
        public bool IsSky => (Flags & QLibTextureFlags.Sky) != 0;
        /// <summary>'{' texture: transparent pixels, needs an alpha tested material.</summary>
        public bool HasAlpha => (Flags & QLibTextureFlags.Alpha) != 0;
        public bool IsLiquid => (Flags & QLibTextureFlags.Liquid) != 0;

        /// <summary>RGBA, empty if the bsp has no pixels for this texture. Sky: the back layer.</summary>
        public byte[] Data { get; }

        /// <summary>True if the texture has fullbright pixels (palette 224..254).</summary>
        public bool HasEmission => Emission.Length > 0;

        /// <summary>RGBA like Data, black except the fullbright pixels. Empty without emission.</summary>
        public byte[] Emission { get; }

        /// <summary>Sky only: the cloud layer, RGBA like Data, alpha 0 where the back layer shows.</summary>
        public byte[] SkyFront { get; }

        internal BspTexture(string name, QLibTextureInfo info, byte[] data, byte[] emission, byte[] skyFront)
        {
            Name = name;
            Width = info.width;
            Height = info.height;
            Flags = info.flags;
            Data = data;
            Emission = emission;
            SkyFront = skyFront;
        }
    }
}
