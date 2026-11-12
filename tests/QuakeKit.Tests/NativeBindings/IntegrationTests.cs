using System.Linq;
using QuakeKit;

namespace QuakeKit.Tests.NativeBindings;

/// <summary>
/// MAP, WAD and lightmap working together.
/// </summary>
public class IntegrationTests
{
    [Fact]
    public void TextureSizesFromWad_GiveTextureUVs()
    {
        using var wad = new NativeQFWad();
        wad.Load(TestData.Path("prototype.wad"));
        wad.GetTextureNames();

        using var map = TestData.LoadMap("test.map");
        map.RegisterTextureSizesFromWad(wad);
        map.GenerateGeometry();
        map.ExportData();

        // the box is 128 units wide with a 128 texture, so its uvs span one texture repeat
        var us = map.SolidEntities[0].Vertices.Select(v => v.uv.x).ToArray();
        Assert.InRange(us.Max() - us.Min(), 0.99f, 1.01f);
    }

    [Fact]
    public void Lightmap_IsBakedFromTheMapLights()
    {
        using var map = TestData.LoadAndExport("bake.map");
        var lm = map.GetLightmap();
        Assert.NotNull(lm);
        Assert.True(lm!.Width > 0 && lm.Height > 0);
        Assert.Equal(16f, lm.LuxelSize);
        Assert.Equal(lm.Width * lm.Height * 4, lm.Combined.Length);
        // static, the animated style 5 light and the switchable light (first switchable style is 32)
        Assert.Equal(new[] { 0, 5, 32 }, lm.Layers.Select(l => l.Style).ToArray());
        Assert.Contains(lm.Layers[0].Pixels, b => b > 0);
    }

    [Fact]
    public void LightStyles_SwitchingALightOff_ChangesTheLightmap()
    {
        using var map = TestData.LoadAndExport("bake.map");
        var on = map.GetLightmap()!.Combined.Sum(b => (long)b);
        map.SetLightStyle(32, "a");
        Assert.True(map.UpdateLightStyles(0));
        var off = map.GetLightmap()!.Combined.Sum(b => (long)b);
        Assert.True(off < on);
        // nothing changes the second time
        Assert.False(map.UpdateLightStyles(0));
    }

    [Fact]
    public void ExtraLights_AreBaked()
    {
        long Brightness(NativeQFMap m) => m.GetLightmap()!.Layers[0].Pixels.Sum(b => (long)b);

        using var plain = TestData.LoadAndExport("bake.map");
        using var lit = TestData.LoadAndExport("bake.map");
        lit.SetExtraLights(new[]
        {
            new QLibLight
            {
                type = LightType.Point,
                position = new QLibVec3(600, 400, 100),
                color = new QLibVec3(1, 1, 1),
                intensity = 300,
                distanceScale = 1,
                castShadows = 1,
            },
        });
        Assert.True(Brightness(lit) > Brightness(plain));
    }
}
