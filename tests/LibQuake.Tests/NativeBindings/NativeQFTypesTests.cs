using System.IO;
using System.Runtime.InteropServices;
using LibQuake;

namespace LibQuake.Tests.NativeBindings;

internal static class TestData
{
    public static string Path(string file) => System.IO.Path.Combine(Directory.GetCurrentDirectory(), file);

    public static NativeQFMap LoadMap(string file, bool csg = true)
    {
        var map = new NativeQFMap();
        map.Load(Path(file), enableCSG: csg);
        return map;
    }

    public static NativeQFMap LoadAndExport(string file, bool csg = true)
    {
        var map = LoadMap(file, csg);
        map.GenerateGeometry();
        map.ExportData();
        return map;
    }
}

/// <summary>
/// The C# structs must have the exact size of the C structs in libquake's wrapper.h, otherwise arrays of them
/// are copied with the wrong stride.
/// </summary>
public class NativeQFTypesTests
{
    [Fact]
    public void StructSizes_MatchTheCApi()
    {
        Assert.Equal(56, Marshal.SizeOf<QLibVertex>());
        Assert.Equal(24, Marshal.SizeOf<QLibSubmesh>());
        Assert.Equal(64, Marshal.SizeOf<QLibSolidEntityInfo>());
        Assert.Equal(32, Marshal.SizeOf<QLibPointEntityInfo>());
        Assert.Equal(20, Marshal.SizeOf<QLibHull>());
        Assert.Equal(16, Marshal.SizeOf<QLibLightmapInfo>());
        Assert.Equal(68, Marshal.SizeOf<QLibLight>());
        Assert.Equal(16, Marshal.SizeOf<QLibMapOptions>());
        Assert.Equal(16, Marshal.SizeOf<QLibBspOptions>());
        Assert.Equal(32, Marshal.SizeOf<QLibTextureInfo>());
    }

    [Fact]
    public void NativeLibrary_HasTheExpectedApiVersion()
    {
        // Load runs the version check, an outdated binary throws a QLibException here
        using var map = TestData.LoadMap("test.map");
    }
}
