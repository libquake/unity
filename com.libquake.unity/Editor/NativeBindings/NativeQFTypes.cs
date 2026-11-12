using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

#nullable enable

namespace LibQuake
{
    // ============================================================================
    // Plain data structs, same layout as libquake's src/wrapper.h (API version 2).
    // All of them are blittable, arrays of them are copied by the marshaller in one go.
    // ============================================================================

    [StructLayout(LayoutKind.Sequential)]
    public struct QLibVec2
    {
        public float x;
        public float y;

        public Vector2 ToVector2() => new Vector2(x, y);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct QLibVec3
    {
        public float x;
        public float y;
        public float z;

        public QLibVec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct QLibVec4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Vector4 ToVector4() => new Vector4(x, y, z, w);
    }

    /// <summary>
    /// Vertex in Quake map coordinates (z up). tangent.w is the bitangent sign.
    /// lightmapUV v = 0 is the first row of the lightmap data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct QLibVertex
    {
        public QLibVec3 pos;
        public QLibVec3 normal;
        public QLibVec4 tangent;
        public QLibVec2 uv;
        public QLibVec2 lightmapUV;
    }

    public enum SurfaceType : uint
    {
        SOLID = 0,
        CLIP = 1,
        SKIP = 2,
        NODRAW = 3,
    }

    public enum HullType : uint
    {
        Solid = 0,
        Clip = 1,
        Liquid = 2,
    }

    public enum LightType : uint
    {
        Point = 0,
        Spot = 1,
        Sun = 2,
    }

    public enum LightFalloff : uint
    {
        Linear = 0,
        Inverse = 1,
        InverseSquare = 2,
        None = 3,
        LocalMinlight = 4,
        InverseSquareB = 5,
    }

    public enum QLibResult
    {
        Ok = 0,
        InvalidArgument = 1,
        BufferTooSmall = 2,
        NotReady = 3,
        LoadFailed = 4,
        Internal = 5,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct QLibSubmesh
    {
        public uint vertexOffset;
        public uint vertexCount;
        public uint indexOffset;
        public uint indexCount;
        public int textureIndex;
        public uint surfaceType;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct QLibSolidEntityInfo
    {
        public IntPtr className;
        public QLibVec3 boundsMin;
        public QLibVec3 boundsMax;
        public uint vertexCount;
        public uint indexCount;
        public uint submeshCount;
        public uint attributeCount;
        public uint hullCount;
        public uint hullPointCount;
        public uint hullIndexCount;
        public uint reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct QLibPointEntityInfo
    {
        public IntPtr className;
        public QLibVec3 origin;
        public float angle;
        public uint attributeCount;
        public uint reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct QLibHull
    {
        public uint type;
        public uint pointOffset;
        public uint pointCount;
        public uint indexOffset;
        public uint indexCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct QLibLightmapInfo
    {
        public uint width;
        public uint height;
        public uint layerCount;
        public float luxelSize;
    }

    /// <summary>
    /// Light for the lightmap baker, in Quake map coordinates and units.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct QLibLight
    {
        public LightType type;
        public LightFalloff falloff;
        public QLibVec3 position;
        public QLibVec3 direction;
        public QLibVec3 color;
        public float intensity;
        public float distanceScale;
        public float coneAngle;
        public float softAngle;
        public int style;
        public uint castShadows;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct QLibMapOptions
    {
        public uint enableCSG;
        public uint convertToOpenGL;
        public float luxelSize;
        public uint lightmapPadding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct QLibBspOptions
    {
        public uint loadTextures;
        public uint loadTextureData;
        public uint convertToOpenGL;
        public uint reserved;
    }

    /// <summary>What a texture is (from its name) and which layers it has.</summary>
    [Flags]
    public enum QLibTextureFlags : uint
    {
        None = 0,
        /// <summary>Name starts with "sky": back layer (Data) and cloud layer (SkyFront).</summary>
        Sky = 1 << 0,
        /// <summary>Name starts with '{': palette index 255 is transparent, draw with alpha test.</summary>
        Alpha = 1 << 1,
        /// <summary>Name starts with '*': water, slime, lava, teleporter.</summary>
        Liquid = 1 << 2,
        HasData = 1 << 3,
        HasEmission = 1 << 4,
        HasSkyFront = 1 << 5,
    }

    public enum QLibTextureLayer : uint
    {
        Albedo = 0,
        Emission = 1,
        SkyFront = 2,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct QLibTextureInfo
    {
        public IntPtr name;
        /// <summary>Sky: the size of one layer, half the width stored in the file.</summary>
        public uint width;
        public uint height;
        public QLibTextureFlags flags;
        public uint reserved0;
        public uint reserved1;
        public uint reserved2;
    }

    // ============================================================================
    // P/Invoke declarations
    // ============================================================================

    public class QLibException : Exception
    {
        public QLibResult Result { get; }

        public QLibException(QLibResult result, string message) : base(message)
        {
            Result = result;
        }
    }

    internal static class QLibNative
    {
        private const string Lib = "quakelib";

        /// <summary>The C API version these bindings are written for.</summary>
        public const uint ApiVersion = 3;

        private static bool _versionChecked;

        /// <summary>
        /// Makes sure the loaded native library speaks the same API, an outdated binary would otherwise fail
        /// in strange ways.
        /// </summary>
        public static void EnsureCompatible()
        {
            if (_versionChecked)
                return;
            uint version;
            try
            {
                version = QLib_GetApiVersion();
            }
            catch (EntryPointNotFoundException)
            {
                version = 1;
            }

            if (version != ApiVersion)
            {
                throw new QLibException(QLibResult.Internal,
                    $"libquake binary has API version {version}, the bindings need {ApiVersion}. Update the native library.");
            }

            _versionChecked = true;
        }

        public static void Check(QLibResult result, string what)
        {
            if (result == QLibResult.Ok)
                return;
            var detail = PtrToString(QLib_GetLastError());
            throw new QLibException(result,
                string.IsNullOrEmpty(detail) ? $"{what} failed: {result}" : $"{what} failed: {detail}");
        }

        public static string PtrToString(IntPtr ptr)
        {
            return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint QLib_GetApiVersion();

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr QLib_GetLastError();

        // MAP
        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void QLibMap_GetDefaultOptions(out QLibMapOptions options);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibMap_Load([MarshalAs(UnmanagedType.LPStr)] string path,
            ref QLibMapOptions options, out IntPtr level);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibMap_SetTextureSize(IntPtr level,
            [MarshalAs(UnmanagedType.LPStr)] string textureName, uint width, uint height);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibMap_SetFaceType(IntPtr level,
            [MarshalAs(UnmanagedType.LPStr)] string textureName, uint surfaceType);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibMap_GenerateGeometry(IntPtr level);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibMap_SetExtraLights(IntPtr level, [In] QLibLight[]? lights, uint count);

        // BSP
        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void QLibBsp_GetDefaultOptions(out QLibBspOptions options);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibBsp_Load([MarshalAs(UnmanagedType.LPStr)] string path,
            ref QLibBspOptions options, out IntPtr level);

        // Level (MAP and BSP)
        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void QLibLevel_Destroy(IntPtr level);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint QLibLevel_GetTextureCount(IntPtr level);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibLevel_GetTextureInfo(IntPtr level, uint index, out QLibTextureInfo info);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibLevel_CopyTextureLayer(IntPtr level, uint index, QLibTextureLayer layer,
            [Out] byte[] rgba, uint byteCount);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint QLibLevel_GetRequiredWadCount(IntPtr level);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr QLibLevel_GetRequiredWad(IntPtr level, uint index);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint QLibLevel_GetSolidEntityCount(IntPtr level);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibLevel_GetSolidEntityInfo(IntPtr level, uint entity,
            out QLibSolidEntityInfo info);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibLevel_CopySolidEntityMesh(IntPtr level, uint entity,
            [Out] QLibVertex[] vertices, uint vertexCapacity, [Out] uint[] indices, uint indexCapacity,
            [Out] QLibSubmesh[] submeshes, uint submeshCapacity);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibLevel_CopyCollisionHulls(IntPtr level, uint entity,
            [Out] QLibHull[] hulls, uint hullCapacity, [Out] QLibVec3[] points, uint pointCapacity,
            [Out] uint[] indices, uint indexCapacity);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint QLibLevel_GetPointEntityCount(IntPtr level);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibLevel_GetPointEntityInfo(IntPtr level, uint entity,
            out QLibPointEntityInfo info);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibLevel_GetEntityAttribute(IntPtr level, uint solid, uint entity,
            uint attribute, out IntPtr key, out IntPtr value);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibLevel_GetLightmapInfo(IntPtr level, out QLibLightmapInfo info);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibLevel_CopyLightmap(IntPtr level, [Out] byte[] rgba, uint byteCount);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibLevel_CopyLightmapLayer(IntPtr level, uint layer, [Out] byte[] rgba,
            uint byteCount, out int style);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibLevel_SetLightStyle(IntPtr level, int style,
            [MarshalAs(UnmanagedType.LPStr)] string pattern);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibLevel_UpdateLightStyles(IntPtr level, double time, out uint changed);

        // WAD
        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibWad_Load([MarshalAs(UnmanagedType.LPStr)] string path,
            uint flipHorizontal, out IntPtr wad);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void QLibWad_Destroy(IntPtr wad);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint QLibWad_GetTextureCount(IntPtr wad);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibWad_GetTextureInfo(IntPtr wad, uint index, out QLibTextureInfo info);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QLibWad_FindTexture(IntPtr wad, [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern QLibResult QLibWad_CopyTextureLayer(IntPtr wad, uint index, QLibTextureLayer layer,
            [Out] byte[] rgba, uint byteCount);
    }

    // ============================================================================
    // Managed copies of the native data, shared by MAP and BSP files
    // ============================================================================

    /// <summary>
    /// Range of one texture in the entity's vertex and index arrays. Indices are entity wide.
    /// </summary>
    public class MapSubmesh
    {
        public uint VertexOffset { get; }
        public uint VertexCount { get; }
        public uint IndexOffset { get; }
        public uint IndexCount { get; }
        public int TextureID { get; }
        public string TextureName { get; }
        public SurfaceType SurfaceType { get; }

        internal MapSubmesh(QLibSubmesh s, string textureName)
        {
            VertexOffset = s.vertexOffset;
            VertexCount = s.vertexCount;
            IndexOffset = s.indexOffset;
            IndexCount = s.indexCount;
            TextureID = s.textureIndex;
            TextureName = textureName;
            SurfaceType = (SurfaceType)s.surfaceType;
        }
    }

    /// <summary>
    /// Convex hull of one brush, in Quake map coordinates. Indices point into Points.
    /// </summary>
    public class CollisionHull
    {
        public HullType Type { get; }
        public Vector3[] Points { get; }
        public int[] Indices { get; }

        internal CollisionHull(HullType type, Vector3[] points, int[] indices)
        {
            Type = type;
            Points = points;
            Indices = indices;
        }
    }

    /// <summary>
    /// A brush entity (worldspawn, doors, triggers, ...), positions in Quake map coordinates.
    /// </summary>
    public class MapSolidEntity
    {
        public string ClassName { get; }
        public Vector3 Center { get; }
        public Vector3 BoundsMin { get; }
        public Vector3 BoundsMax { get; }
        public Dictionary<string, string> Attributes { get; }
        public QLibVertex[] Vertices { get; }
        public uint[] Indices { get; }
        public List<MapSubmesh> Submeshes { get; }
        public List<CollisionHull> Hulls { get; }

        internal MapSolidEntity(string className, QLibSolidEntityInfo info, Dictionary<string, string> attributes,
            QLibVertex[] vertices, uint[] indices, List<MapSubmesh> submeshes, List<CollisionHull> hulls)
        {
            ClassName = className;
            BoundsMin = info.boundsMin.ToVector3();
            BoundsMax = info.boundsMax.ToVector3();
            Center = new Vector3((info.boundsMin.x + info.boundsMax.x) * 0.5f, (info.boundsMin.y + info.boundsMax.y) * 0.5f,
                (info.boundsMin.z + info.boundsMax.z) * 0.5f);
            Attributes = attributes;
            Vertices = vertices;
            Indices = indices;
            Submeshes = submeshes;
            Hulls = hulls;
        }
    }

    /// <summary>
    /// A point entity (lights, monsters, spawn points, ...), origin in Quake map coordinates.
    /// </summary>
    public class MapPointEntity
    {
        public string ClassName { get; }
        public Vector3 Origin { get; }
        public float Angle { get; }
        public Dictionary<string, string> Attributes { get; }

        internal MapPointEntity(string className, Vector3 origin, float angle, Dictionary<string, string> attributes)
        {
            ClassName = className;
            Origin = origin;
            Angle = angle;
            Attributes = attributes;
        }
    }

    public class LightmapLayer
    {
        /// <summary>Quake light style of this layer, 0 is static, 32 and up are switchable lights.</summary>
        public int Style { get; }

        public byte[] Pixels { get; }

        internal LightmapLayer(int style, byte[] pixels)
        {
            Style = style;
            Pixels = pixels;
        }
    }

    /// <summary>
    /// RGBA lightmap, the first row of Pixels is v = 0 of the lightmap uvs. Values follow the Quake tools:
    /// 128 is normal brightness, a shader should use albedo * lightmap * 2.
    /// </summary>
    public class LevelLightmap
    {
        public int Width { get; }
        public int Height { get; }
        public float LuxelSize { get; }
        public byte[] Combined { get; }
        public List<LightmapLayer> Layers { get; }

        internal LevelLightmap(int width, int height, float luxelSize, byte[] combined, List<LightmapLayer> layers)
        {
            Width = width;
            Height = height;
            LuxelSize = luxelSize;
            Combined = combined;
            Layers = layers;
        }
    }

    internal static class TextureReader
    {
        /// <summary>A layer of a texture as RGBA, empty if the flags say the texture does not have it.</summary>
        public static byte[] Layer(QLibTextureInfo info, QLibTextureFlags has, QLibTextureLayer layer,
            Func<QLibTextureLayer, byte[], QLibResult> copy)
        {
            if ((info.flags & has) == 0)
                return Array.Empty<byte>();
            var px = new byte[info.width * info.height * 4];
            QLibNative.Check(copy(layer, px), $"CopyTextureLayer {layer}");
            return px;
        }
    }

    internal static class LevelReader
    {
        public static List<string> TextureNames(IntPtr level)
        {
            var names = new List<string>();
            uint count = QLibNative.QLibLevel_GetTextureCount(level);
            for (uint i = 0; i < count; i++)
            {
                QLibNative.Check(QLibNative.QLibLevel_GetTextureInfo(level, i, out var info), "GetTextureInfo");
                names.Add(QLibNative.PtrToString(info.name));
            }

            return names;
        }

        /// <summary>Flags of every texture by name (case insensitive): sky, alpha and liquid follow the name.</summary>
        public static Dictionary<string, QLibTextureFlags> TextureFlags(IntPtr level)
        {
            var flags = new Dictionary<string, QLibTextureFlags>(StringComparer.OrdinalIgnoreCase);
            uint count = QLibNative.QLibLevel_GetTextureCount(level);
            for (uint i = 0; i < count; i++)
            {
                QLibNative.Check(QLibNative.QLibLevel_GetTextureInfo(level, i, out var info), "GetTextureInfo");
                flags[QLibNative.PtrToString(info.name)] = info.flags;
            }

            return flags;
        }

        public static List<string> RequiredWads(IntPtr level)
        {
            var wads = new List<string>();
            uint count = QLibNative.QLibLevel_GetRequiredWadCount(level);
            for (uint i = 0; i < count; i++)
                wads.Add(QLibNative.PtrToString(QLibNative.QLibLevel_GetRequiredWad(level, i)));
            return wads;
        }

        private static Dictionary<string, string> Attributes(IntPtr level, bool solid, uint entity, uint count)
        {
            var attrs = new Dictionary<string, string>();
            for (uint a = 0; a < count; a++)
            {
                QLibNative.Check(QLibNative.QLibLevel_GetEntityAttribute(level, solid ? 1u : 0u, entity, a, out var key,
                    out var value), "GetEntityAttribute");
                attrs[QLibNative.PtrToString(key)] = QLibNative.PtrToString(value);
            }

            return attrs;
        }

        public static MapSolidEntity Solid(IntPtr level, uint entity, List<string> textureNames)
        {
            QLibNative.Check(QLibNative.QLibLevel_GetSolidEntityInfo(level, entity, out var info), "GetSolidEntityInfo");

            var vertices = new QLibVertex[info.vertexCount];
            var indices = new uint[info.indexCount];
            var subs = new QLibSubmesh[info.submeshCount];
            QLibNative.Check(QLibNative.QLibLevel_CopySolidEntityMesh(level, entity, vertices, info.vertexCount, indices,
                info.indexCount, subs, info.submeshCount), "CopySolidEntityMesh");

            var submeshes = new List<MapSubmesh>(subs.Length);
            foreach (var s in subs)
            {
                var name = s.textureIndex >= 0 && s.textureIndex < textureNames.Count ? textureNames[s.textureIndex] : string.Empty;
                submeshes.Add(new MapSubmesh(s, name));
            }

            var hulls = new List<CollisionHull>((int)info.hullCount);
            if (info.hullCount > 0)
            {
                var nativeHulls = new QLibHull[info.hullCount];
                var points = new QLibVec3[info.hullPointCount];
                var hullIndices = new uint[info.hullIndexCount];
                QLibNative.Check(QLibNative.QLibLevel_CopyCollisionHulls(level, entity, nativeHulls, info.hullCount, points,
                    info.hullPointCount, hullIndices, info.hullIndexCount), "CopyCollisionHulls");
                foreach (var h in nativeHulls)
                {
                    var p = new Vector3[h.pointCount];
                    for (int i = 0; i < p.Length; i++)
                        p[i] = points[h.pointOffset + i].ToVector3();
                    var idx = new int[h.indexCount];
                    for (int i = 0; i < idx.Length; i++)
                        idx[i] = (int)hullIndices[h.indexOffset + i];
                    hulls.Add(new CollisionHull((HullType)h.type, p, idx));
                }
            }

            return new MapSolidEntity(QLibNative.PtrToString(info.className), info,
                Attributes(level, true, entity, info.attributeCount), vertices, indices, submeshes, hulls);
        }

        public static MapPointEntity Point(IntPtr level, uint entity)
        {
            QLibNative.Check(QLibNative.QLibLevel_GetPointEntityInfo(level, entity, out var info), "GetPointEntityInfo");
            return new MapPointEntity(QLibNative.PtrToString(info.className), info.origin.ToVector3(), info.angle,
                Attributes(level, false, entity, info.attributeCount));
        }

        public static LevelLightmap? Lightmap(IntPtr level)
        {
            QLibNative.Check(QLibNative.QLibLevel_GetLightmapInfo(level, out var info), "GetLightmapInfo");
            if (info.width == 0 || info.height == 0)
                return null;
            uint bytes = info.width * info.height * 4;
            var combined = new byte[bytes];
            QLibNative.Check(QLibNative.QLibLevel_CopyLightmap(level, combined, bytes), "CopyLightmap");
            var layers = new List<LightmapLayer>();
            for (uint l = 0; l < info.layerCount; l++)
            {
                var px = new byte[bytes];
                QLibNative.Check(QLibNative.QLibLevel_CopyLightmapLayer(level, l, px, bytes, out var style), "CopyLightmapLayer");
                layers.Add(new LightmapLayer(style, px));
            }

            return new LevelLightmap((int)info.width, (int)info.height, info.luxelSize, combined, layers);
        }
    }
}
