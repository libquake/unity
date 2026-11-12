using System;
using System.Linq;
using LibQuake;

namespace LibQuake.Tests.NativeBindings;

public class NativeQFMapTests
{
    [Fact]
    public void Load_MissingFile_Throws()
    {
        using var map = new NativeQFMap();
        var ex = Assert.Throws<QLibException>(() => map.Load(TestData.Path("does_not_exist.map")));
        Assert.Equal(QLibResult.LoadFailed, ex.Result);
        Assert.Contains("does_not_exist.map", ex.Message);
    }

    [Fact]
    public void CallsBeforeLoad_Throw()
    {
        using var map = new NativeQFMap();
        Assert.Throws<InvalidOperationException>(() => map.GenerateGeometry());
        Assert.Throws<InvalidOperationException>(() => map.ExportData());
        Assert.Throws<InvalidOperationException>(() => map.GetLightmap());
    }

    [Fact]
    public void FaceTypesAndTextureSizes_AfterGenerateGeometry_Throw()
    {
        // they only have an effect before the geometry is built
        using var map = TestData.LoadMap("test.map");
        map.GenerateGeometry();
        Assert.Throws<InvalidOperationException>(() => map.SetFaceType("clip", SurfaceType.CLIP));
        Assert.Throws<InvalidOperationException>(() => map.RegisterTextureSize("128_blue_3", 128, 128));
    }

    [Fact]
    public void ExportData_BeforeGenerateGeometry_Throws()
    {
        using var map = TestData.LoadMap("test.map");
        Assert.Throws<InvalidOperationException>(() => map.ExportData());
    }

    [Fact]
    public void Load_ReadsWadsAndTextures()
    {
        using var map = TestData.LoadMap("test.map");
        Assert.Equal(new[] { "prototype.wad" }, map.GetRequiredWads());
        Assert.Contains("128_blue_3", map.GetTextureNames());
    }

    [Fact]
    public void Box_HasWeldedQuads()
    {
        using var map = TestData.LoadAndExport("test.map");
        var world = map.SolidEntities[0];
        Assert.Equal("worldspawn", world.ClassName);
        // 6 faces with 4 shared corners and 2 triangles each
        Assert.Equal(24, world.Vertices.Length);
        Assert.Equal(36, world.Indices.Length);
        Assert.Single(world.Submeshes);
        Assert.Equal("128_blue_3", world.Submeshes[0].TextureName);
        Assert.Equal(SurfaceType.SOLID, world.Submeshes[0].SurfaceType);
    }

    [Fact]
    public void Box_BoundsAndCenter()
    {
        using var map = TestData.LoadAndExport("test.map");
        var world = map.SolidEntities[0];
        Assert.Equal(-64f, world.BoundsMin.x);
        Assert.Equal(-16f, world.BoundsMin.z);
        Assert.Equal(64f, world.BoundsMax.y);
        Assert.Equal(16f, world.BoundsMax.z);
        Assert.Equal(0f, world.Center.x);
    }

    [Fact]
    public void Attributes_IncludeClassnameAndKeys()
    {
        using var map = TestData.LoadAndExport("test.map");
        var world = map.SolidEntities[0];
        Assert.Equal("worldspawn", world.Attributes["classname"]);
        Assert.Equal("prototype.wad", world.Attributes["wad"]);
        Assert.Equal("220", world.Attributes["mapversion"]);
    }

    [Fact]
    public void Csg_ChangesTheGeometry()
    {
        using var withCsg = TestData.LoadAndExport("bake.map", csg: true);
        using var withoutCsg = TestData.LoadAndExport("bake.map", csg: false);
        Assert.NotEqual(withCsg.SolidEntities[0].Indices.Length, withoutCsg.SolidEntities[0].Indices.Length);
    }

    [Fact]
    public void TextureSizes_GiveTextureUVs_CaseInsensitive()
    {
        using var map = TestData.LoadMap("test.map");
        map.RegisterTextureSize("128_BLUE_3", 128, 128);
        map.GenerateGeometry();
        map.ExportData();
        Assert.Contains(map.SolidEntities[0].Vertices, v => v.uv.x != 0 || v.uv.y != 0);
    }

    [Fact]
    public void WithoutTextureSizes_TextureUVsAreZero()
    {
        using var map = TestData.LoadAndExport("test.map");
        Assert.All(map.SolidEntities[0].Vertices, v => Assert.True(v.uv.x == 0 && v.uv.y == 0));
    }

    [Fact]
    public void ToolTextures_AreNotSolid_AndGiveHulls()
    {
        using var map = TestData.LoadAndExport("tooltextures.map");
        var world = map.SolidEntities[0];
        Assert.DoesNotContain(world.Submeshes,
            s => s.SurfaceType == SurfaceType.SOLID && (s.TextureName == "hint" || s.TextureName == "nodraw"));

        // ground and nodraw are solid, the clip brush blocks only players, water is a volume, hint is left out
        Assert.Equal(4, world.Hulls.Count);
        Assert.Equal(2, world.Hulls.Count(h => h.Type == HullType.Solid));
        Assert.Single(world.Hulls, h => h.Type == HullType.Clip);
        Assert.Single(world.Hulls, h => h.Type == HullType.Liquid);
        Assert.All(world.Hulls, h =>
        {
            Assert.Equal(8, h.Points.Length);
            Assert.Equal(36, h.Indices.Length);
            Assert.All(h.Indices, i => Assert.InRange(i, 0, h.Points.Length - 1));
        });
    }

    [Fact]
    public void SetFaceTypes_BeforeGeometry_MarksFaces()
    {
        using var map = TestData.LoadMap("test.map");
        map.SetFaceTypes("128_BLUE_3; something_else", SurfaceType.NODRAW);
        map.GenerateGeometry();
        map.ExportData();
        Assert.All(map.SolidEntities[0].Submeshes, s => Assert.Equal(SurfaceType.NODRAW, s.SurfaceType));
    }

    [Fact]
    public void GetTextureFlags_FollowTheNames_AndSkyFacesAreDrawn()
    {
        using var map = TestData.LoadAndExport("sky_alpha.map");
        Assert.Equal(QLibTextureFlags.Sky, map.GetTextureFlags("SKY1") & QLibTextureFlags.Sky);
        Assert.Equal(QLibTextureFlags.Alpha, map.GetTextureFlags("{fence") & QLibTextureFlags.Alpha);
        Assert.Equal(QLibTextureFlags.None,
            map.GetTextureFlags("{fence") & (QLibTextureFlags.Sky | QLibTextureFlags.Liquid));
        Assert.Equal(QLibTextureFlags.None, map.GetTextureFlags("not_in_the_map"));

        Assert.Contains(map.SolidEntities[0].Submeshes,
            s => s.TextureName == "sky1" && s.SurfaceType == SurfaceType.SOLID);
    }

    [Fact]
    public void Dispose_Twice_IsFine()
    {
        var map = TestData.LoadAndExport("test.map");
        map.Dispose();
        map.Dispose();
    }
}
