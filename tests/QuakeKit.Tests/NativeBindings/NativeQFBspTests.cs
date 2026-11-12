using System.Linq;
using QuakeKit;

namespace QuakeKit.Tests.NativeBindings;

public class NativeQFBspTests
{
    [Fact]
    public void Textures_WithFullbrights_HaveEmission()
    {
        using var bsp = new NativeQFBsp();
        bsp.Load(TestData.Path("fullbright.bsp"));
        bsp.ExportData();

        var button = bsp.Textures.Single(t => t.Name == "+0button_0");
        Assert.True(button.HasEmission);
        Assert.Equal(button.Data.Length, button.Emission.Length);
        Assert.False(bsp.Textures.Single(t => t.Name == "128_blue_3").HasEmission);
    }

    [Fact]
    public void Load_ExportsEntitiesAndLightmap()
    {
        using var bsp = new NativeQFBsp();
        bsp.Load(TestData.Path("lightstyles.bsp"));
        bsp.ExportData();

        Assert.NotEmpty(bsp.SolidEntities);
        var world = bsp.SolidEntities[0];
        Assert.Equal("worldspawn", world.ClassName);
        Assert.NotEmpty(world.Vertices);
        Assert.Empty(world.Hulls);
        Assert.All(world.Indices, i => Assert.InRange(i, 0u, (uint)world.Vertices.Length - 1));
        Assert.Contains(bsp.PointEntities, p => p.ClassName == "light");
        Assert.NotEmpty(bsp.Textures);

        var lm = bsp.GetLightmap();
        Assert.NotNull(lm);
        Assert.Equal(lm!.Width * lm.Height * 4, lm.Combined.Length);
        Assert.Contains(lm.Combined, b => b > 0);
    }

    [Fact]
    public void Textures_SkyAndAlpha_MatchTheWad()
    {
        using var bsp = new NativeQFBsp();
        bsp.Load(TestData.Path("sky_alpha.bsp"));
        bsp.ExportData();
        using var wad = new NativeQFWad();
        wad.Load(TestData.Path("sky_alpha.wad"));

        // the same layers as the wad, the bsp stores the sky whole
        var sky = bsp.Textures.Single(t => t.Name == "sky1");
        var wadSky = wad.GetTexture("sky1")!;
        Assert.True(sky.IsSky);
        Assert.Equal(128u, sky.Width);
        Assert.Equal(wadSky.Data, sky.Data);
        Assert.Equal(wadSky.SkyFront, sky.SkyFront);

        var fence = bsp.Textures.Single(t => t.Name == "{fence");
        Assert.True(fence.HasAlpha);
        Assert.Equal(wad.GetTexture("{fence")!.Data, fence.Data);
        Assert.Empty(fence.SkyFront);
        Assert.False(bsp.Textures.Single(t => t.Name == "128_blue_3").HasAlpha);
    }

    [Fact]
    public void Lightmap_HasOneLayerPerStyle()
    {
        using var bsp = new NativeQFBsp();
        bsp.Load(TestData.Path("lightstyles.bsp"));
        var lm = bsp.GetLightmap()!;
        Assert.True(lm.Layers.Count > 1);
        Assert.Equal(0, lm.Layers[0].Style);
        Assert.All(lm.Layers, l => Assert.Equal(lm.Combined.Length, l.Pixels.Length));
        Assert.Equal(lm.Layers.Count, lm.Layers.Select(l => l.Style).Distinct().Count());
    }

    [Fact]
    public void Load_MissingFile_Throws()
    {
        using var bsp = new NativeQFBsp();
        Assert.Throws<QLibException>(() => bsp.Load(TestData.Path("does_not_exist.bsp")));
    }
}
