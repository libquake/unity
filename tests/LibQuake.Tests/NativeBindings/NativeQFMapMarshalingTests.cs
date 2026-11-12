using System;
using System.Linq;
using LibQuake;

namespace LibQuake.Tests.NativeBindings;

/// <summary>
/// Checks that the copied mesh data is consistent: ranges, indices, normals and tangents.
/// </summary>
public class NativeQFMapMarshalingTests
{
    private static float Dot(QLibVec3 a, QLibVec3 b) => a.x * b.x + a.y * b.y + a.z * b.z;

    private static QLibVec3 Sub(QLibVec3 a, QLibVec3 b) => new QLibVec3(a.x - b.x, a.y - b.y, a.z - b.z);

    private static QLibVec3 Cross(QLibVec3 a, QLibVec3 b) =>
        new QLibVec3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);

    [Theory]
    [InlineData("test.map")]
    [InlineData("bake.map")]
    [InlineData("tooltextures.map")]
    public void SubmeshRangesAndIndices_AreValid(string file)
    {
        using var map = TestData.LoadAndExport(file);
        foreach (var e in map.SolidEntities)
        {
            Assert.All(e.Indices, i => Assert.InRange(i, 0u, (uint)e.Vertices.Length - 1));
            Assert.Equal(0, e.Indices.Length % 3);
            foreach (var s in e.Submeshes)
            {
                Assert.True(s.VertexOffset + s.VertexCount <= e.Vertices.Length);
                Assert.True(s.IndexOffset + s.IndexCount <= e.Indices.Length);
                // the indices of a submesh only use its own vertices
                for (uint i = s.IndexOffset; i < s.IndexOffset + s.IndexCount; i++)
                    Assert.InRange(e.Indices[i], s.VertexOffset, s.VertexOffset + s.VertexCount - 1);
                Assert.False(string.IsNullOrEmpty(s.TextureName));
            }
        }
    }

    [Fact]
    public void Normals_PointOutOfTheBox()
    {
        using var map = TestData.LoadAndExport("test.map");
        var world = map.SolidEntities[0];
        var center = new QLibVec3(world.Center.x, world.Center.y, world.Center.z);
        Assert.All(world.Vertices, v => Assert.True(Dot(v.normal, Sub(v.pos, center)) > 0));
    }

    [Fact]
    public void Triangles_AreClockwiseSeenFromTheFront()
    {
        // Quake winding: the triangle normal from the winding points against the vertex normal
        using var map = TestData.LoadAndExport("test.map");
        var world = map.SolidEntities[0];
        for (int t = 0; t < world.Indices.Length; t += 3)
        {
            var a = world.Vertices[world.Indices[t]];
            var b = world.Vertices[world.Indices[t + 1]];
            var c = world.Vertices[world.Indices[t + 2]];
            var n = Cross(Sub(b.pos, a.pos), Sub(c.pos, a.pos));
            Assert.True(Dot(n, a.normal) < 0);
        }
    }

    [Fact]
    public void Tangents_AreUnitLength_AndInThePlane()
    {
        using var map = TestData.LoadMap("test.map");
        map.RegisterTextureSize("128_blue_3", 128, 128);
        map.GenerateGeometry();
        map.ExportData();
        Assert.All(map.SolidEntities[0].Vertices, v =>
        {
            var t = new QLibVec3(v.tangent.x, v.tangent.y, v.tangent.z);
            Assert.InRange(Dot(t, t), 0.99f, 1.01f);
            Assert.InRange(Dot(t, v.normal), -1e-3f, 1e-3f);
            Assert.True(v.tangent.w == 1 || v.tangent.w == -1);
        });
    }

    [Fact]
    public void LightmapUVs_AreInsideTheAtlas()
    {
        using var map = TestData.LoadAndExport("bake.map");
        Assert.All(map.SolidEntities.SelectMany(e => e.Vertices), v =>
        {
            Assert.InRange(v.lightmapUV.x, 0f, 1f);
            Assert.InRange(v.lightmapUV.y, 0f, 1f);
        });
    }

    [Fact]
    public void ExportData_Twice_GivesTheSameData()
    {
        using var map = TestData.LoadAndExport("bake.map");
        var first = map.SolidEntities[0].Vertices.ToArray();
        map.ExportData();
        Assert.Equal(first, map.SolidEntities[0].Vertices);
    }
}
