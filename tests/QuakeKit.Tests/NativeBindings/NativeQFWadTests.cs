using System.Linq;
using QuakeKit;

namespace QuakeKit.Tests.NativeBindings;

public class NativeQFWadTests
{
    private static NativeQFWad LoadWad()
    {
        var wad = new NativeQFWad();
        wad.Load(TestData.Path("prototype.wad"));
        return wad;
    }

    [Fact]
    public void Load_MissingFile_Throws()
    {
        using var wad = new NativeQFWad();
        Assert.Throws<QLibException>(() => wad.Load(TestData.Path("does_not_exist.wad")));
    }

    [Fact]
    public void Load_NotAWad_Throws()
    {
        using var wad = new NativeQFWad();
        Assert.Throws<QLibException>(() => wad.Load(TestData.Path("test.map")));
    }

    [Fact]
    public void GetTextureNames_ListsAllTexturesWithSizes()
    {
        using var wad = LoadWad();
        wad.GetTextureNames();
        Assert.NotEmpty(wad.Textures);
        Assert.All(wad.Textures, t =>
        {
            Assert.False(string.IsNullOrEmpty(t.Name));
            Assert.True(t.Width > 0 && t.Height > 0);
            Assert.Empty(t.Data);
        });
        Assert.Equal(wad.Textures.Count, wad.Textures.Select(t => t.Name).Distinct().Count());
    }

    [Fact]
    public void GetTexture_ReturnsRgbaPixels_CaseInsensitive()
    {
        using var wad = LoadWad();
        var texture = wad.GetTexture("128_BLUE_3");
        Assert.NotNull(texture);
        Assert.Equal("128_blue_3", texture!.Name);
        Assert.Equal(128u, texture.Width);
        Assert.Equal(128u, texture.Height);
        Assert.Equal(128 * 128 * 4, texture.Data.Length);
        Assert.Contains(texture.Data, b => b != 0);
    }

    [Fact]
    public void GetTexture_FullbrightPixels_GiveEmission()
    {
        using var wad = LoadWad();
        var button = wad.GetTexture("+0button_0");
        Assert.NotNull(button);
        Assert.True(button!.HasEmission);
        Assert.Equal(button.Data.Length, button.Emission.Length);
        int lit = 0;
        for (int i = 0; i < button.Emission.Length; i += 4)
        {
            if (button.Emission[i] == 0 && button.Emission[i + 1] == 0 && button.Emission[i + 2] == 0)
                continue;
            lit++;
            Assert.Equal(button.Data[i], button.Emission[i]);
        }

        Assert.Equal(884, lit);

        var blue = wad.GetTexture("128_blue_3");
        Assert.False(blue!.HasEmission);
        Assert.Empty(blue.Emission);

        wad.GetTextureNames();
        Assert.True(wad.Textures.Single(t => t.Name == "+0button_0").HasEmission);
    }

    [Fact]
    public void GetTexture_NameWithCapitals_IsFound()
    {
        using var wad = LoadWad();
        Assert.Equal("text_A", wad.GetTexture("TEXT_a")?.Name);
    }

    [Fact]
    public void GetTextureByIndex_MatchesGetTextureByName()
    {
        using var wad = LoadWad();
        wad.GetTextureNames();
        Assert.Equal(wad.Textures.Count, wad.TextureCount);
        for (int i = 0; i < wad.TextureCount; i++)
        {
            var byIndex = wad.GetTexture(i);
            var byName = wad.GetTexture(byIndex.Name);
            Assert.Equal(wad.Textures[i].Name, byIndex.Name);
            Assert.Equal(byName!.Data, byIndex.Data);
            Assert.Equal(byName.Emission, byIndex.Emission);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => wad.GetTexture(wad.TextureCount));
        Assert.Throws<ArgumentOutOfRangeException>(() => wad.GetTexture(-1));
    }

    [Fact]
    public void Load_Flipped_ReversesTheRows()
    {
        using var top = LoadWad();
        using var bottom = new NativeQFWad();
        bottom.Load(TestData.Path("prototype.wad"), flipHorizontal: true);

        var a = top.GetTexture("+0button_0")!;
        var b = bottom.GetTexture("+0button_0")!;
        int row = (int)a.Width * 4;
        for (int y = 0; y < a.Height; y++)
        {
            var flippedY = (int)a.Height - 1 - y;
            Assert.Equal(a.Data.AsSpan(y * row, row).ToArray(), b.Data.AsSpan(flippedY * row, row).ToArray());
            Assert.Equal(a.Emission.AsSpan(y * row, row).ToArray(), b.Emission.AsSpan(flippedY * row, row).ToArray());
        }
    }

    [Fact]
    public void GetTexture_Sky_HasTwoLayers()
    {
        using var top = LoadWad();
        using var bottom = new NativeQFWad();
        bottom.Load(TestData.Path("prototype.wad"), flipHorizontal: true);

        var a = top.GetTexture("sky1")!;
        var b = bottom.GetTexture("sky1")!;
        Assert.True(a.IsSky);
        Assert.Equal(QLibTextureFlags.Sky | QLibTextureFlags.HasData | QLibTextureFlags.HasSkyFront, a.Flags);
        Assert.Equal(128u, a.Width);
        Assert.Equal(128u, a.Height);
        Assert.Equal(a.Data.Length, a.SkyFront.Length);
        Assert.Empty(a.Emission);

        // clouds with holes where the back layer shows
        int clear = 0;
        for (int i = 3; i < a.SkyFront.Length; i += 4)
            clear += a.SkyFront[i] == 0 ? 1 : 0;
        Assert.InRange(clear, 1, 128 * 128 - 1);

        int row = (int)a.Width * 4;
        for (int y = 0; y < a.Height; y++)
        {
            var flippedY = (int)a.Height - 1 - y;
            Assert.Equal(a.SkyFront.AsSpan(y * row, row).ToArray(), b.SkyFront.AsSpan(flippedY * row, row).ToArray());
        }
    }

    [Fact]
    public void GetTexture_FlagsFromTheName()
    {
        using var wad = LoadWad();
        var water = wad.GetTexture("*64_water1_1")!;
        Assert.True(water.IsLiquid);
        Assert.False(water.HasAlpha);
        Assert.Empty(water.SkyFront);
        Assert.Equal(QLibTextureFlags.HasData, wad.GetTexture("128_blue_3")!.Flags);

        using var alpha = new NativeQFWad();
        alpha.Load(TestData.Path("sky_alpha.wad"));
        var fence = alpha.GetTexture("{fence")!;
        Assert.True(fence.HasAlpha);
        int holes = 0;
        for (int i = 3; i < fence.Data.Length; i += 4)
            holes += fence.Data[i] == 0 ? 1 : 0;
        Assert.Equal(48 * 48, holes);
    }

    [Fact]
    public void GetTexture_Unknown_ReturnsNull()
    {
        using var wad = LoadWad();
        Assert.Null(wad.GetTexture("nonexistent"));
    }
}
