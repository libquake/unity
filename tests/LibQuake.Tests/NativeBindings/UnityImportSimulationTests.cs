using System;
using System.Collections.Generic;
using LibQuake;

namespace LibQuake.Tests.NativeBindings;

/// <summary>
/// The call sequence of QMapAssetImporter, without Unity.
/// </summary>
public class UnityImportSimulationTests
{
    private static void Import(string file)
    {
        using var map = new NativeQFMap();
        map.Load(TestData.Path(file), enableCSG: true);
        foreach (var name in map.GetTextureNames())
            map.RegisterTextureSize(name, 64, 64);
        map.SetFaceTypes("clip", SurfaceType.CLIP);
        map.SetFaceTypes("skip", SurfaceType.SKIP);
        map.SetFaceTypes("sky1;sky4", SurfaceType.NODRAW);
        map.GenerateGeometry();
        map.ExportData();
        var lightmap = map.GetLightmap();

        Assert.NotEmpty(map.SolidEntities);
        Assert.NotNull(lightmap);
        foreach (var e in map.SolidEntities)
        {
            Assert.NotNull(e.ClassName);
            Assert.NotNull(e.Attributes);
            Assert.NotNull(e.Hulls);
        }
    }

    [Theory]
    [InlineData("test.map")]
    [InlineData("bake.map")]
    [InlineData("tooltextures.map")]
    public void ImporterSequence_Works(string file)
    {
        Import(file);
    }

    [Fact]
    public void RepeatedImports_DoNotLeakOrCrash()
    {
        for (int i = 0; i < 25; i++)
            Import("bake.map");
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    [Fact]
    public void LoadingASecondMap_OnTheSameInstance_ReplacesTheFirst()
    {
        using var map = TestData.LoadAndExport("bake.map");
        map.Load(TestData.Path("test.map"));
        Assert.Empty(map.SolidEntities);
        map.GenerateGeometry();
        map.ExportData();
        Assert.Equal(24, map.SolidEntities[0].Vertices.Length);
    }
}
