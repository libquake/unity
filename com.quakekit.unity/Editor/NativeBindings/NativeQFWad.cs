using System;
using System.Collections.Generic;

#nullable enable

namespace QuakeKit
{
    /// <summary>
    /// A WAD2 texture archive loaded through libquake.
    /// </summary>
    public class NativeQFWad : IDisposable
    {
        private IntPtr _wad = IntPtr.Zero;

        /// <summary>Texture names and sizes, filled by GetTextureNames(). Data is empty, use GetTexture().</summary>
        public List<WadTexture> Textures { get; } = new List<WadTexture>();

        /// <summary>
        /// Opens the file. Throws a QLibException if it is not a valid WAD2 file.
        /// </summary>
        public void Load(string wadPath, bool flipHorizontal = false)
        {
            QLibNative.EnsureCompatible();
            Release();
            QLibNative.Check(QLibNative.QLibWad_Load(wadPath, flipHorizontal ? 1u : 0u, out _wad), $"Loading {wadPath}");
        }

        /// <summary>
        /// Fills Textures with the name and size of every texture, without pixel data.
        /// </summary>
        public void GetTextureNames()
        {
            EnsureLoaded();
            Textures.Clear();
            uint count = QLibNative.QLibWad_GetTextureCount(_wad);
            for (uint i = 0; i < count; i++)
            {
                QLibNative.Check(QLibNative.QLibWad_GetTextureInfo(_wad, i, out var info), "GetTextureInfo");
                Textures.Add(new WadTexture(info, Array.Empty<byte>(), Array.Empty<byte>(), Array.Empty<byte>()));
            }
        }

        /// <summary>
        /// A texture with RGBA pixels, null if the WAD has no texture with this name (case insensitive).
        /// </summary>
        public WadTexture? GetTexture(string textureName)
        {
            EnsureLoaded();
            int index = QLibNative.QLibWad_FindTexture(_wad, textureName);
            return index < 0 ? null : GetTexture(index);
        }

        /// <summary>Number of textures, valid indices for GetTexture(int).</summary>
        public int TextureCount
        {
            get
            {
                EnsureLoaded();
                return (int)QLibNative.QLibWad_GetTextureCount(_wad);
            }
        }

        /// <summary>
        /// A texture with RGBA pixels by index, 0 to TextureCount - 1. Sky textures come as two layers (Data is the
        /// back layer, SkyFront the clouds), each half the width of the WAD texture.
        /// </summary>
        public WadTexture GetTexture(int index)
        {
            EnsureLoaded();
            if (index < 0 || index >= TextureCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            QLibNative.Check(QLibNative.QLibWad_GetTextureInfo(_wad, (uint)index, out var info), "GetTextureInfo");
            QLibResult Copy(QLibTextureLayer layer, byte[] px) =>
                QLibNative.QLibWad_CopyTextureLayer(_wad, (uint)index, layer, px, (uint)px.Length);
            return new WadTexture(info,
                TextureReader.Layer(info, QLibTextureFlags.HasData, QLibTextureLayer.Albedo, Copy),
                TextureReader.Layer(info, QLibTextureFlags.HasEmission, QLibTextureLayer.Emission, Copy),
                TextureReader.Layer(info, QLibTextureFlags.HasSkyFront, QLibTextureLayer.SkyFront, Copy));
        }

        private void EnsureLoaded()
        {
            if (_wad == IntPtr.Zero)
                throw new InvalidOperationException("WAD not loaded. Call Load() first.");
        }

        private void Release()
        {
            if (_wad != IntPtr.Zero)
            {
                QLibNative.QLibWad_Destroy(_wad);
                _wad = IntPtr.Zero;
            }

            Textures.Clear();
        }

        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        ~NativeQFWad()
        {
            Release();
        }
    }

    public class WadTexture
    {
        public string Name { get; }
        /// <summary>Sky: the size of one layer.</summary>
        public uint Width { get; }
        public uint Height { get; }
        public QLibTextureFlags Flags { get; }
        public bool IsSky => (Flags & QLibTextureFlags.Sky) != 0;
        /// <summary>'{' texture: transparent pixels, needs an alpha tested material.</summary>
        public bool HasAlpha => (Flags & QLibTextureFlags.Alpha) != 0;
        public bool IsLiquid => (Flags & QLibTextureFlags.Liquid) != 0;

        /// <summary>RGBA pixels, first row is the top of the texture unless the WAD was loaded flipped. Sky: the
        /// back layer.</summary>
        public byte[] Data { get; }

        /// <summary>True if the texture has fullbright pixels (palette 224..254).</summary>
        public bool HasEmission => (Flags & QLibTextureFlags.HasEmission) != 0;

        /// <summary>RGBA like Data, black except the fullbright pixels. Empty without emission.</summary>
        public byte[] Emission { get; }

        /// <summary>Sky only: the cloud layer, RGBA like Data, alpha 0 where the back layer shows.</summary>
        public byte[] SkyFront { get; }

        internal WadTexture(QLibTextureInfo info, byte[] data, byte[] emission, byte[] skyFront)
        {
            Name = QLibNative.PtrToString(info.name);
            Width = info.width;
            Height = info.height;
            Flags = info.flags;
            Data = data;
            Emission = emission;
            SkyFront = skyFront;
        }
    }
}
