using System.Globalization;
using QuakeKit;
using UnityEngine;

namespace QuakeKit.Tests.Runtime;

public class QKitConvertTests
{
    [Fact]
    public void ToUnityPosition_SwapsAxesAndScales()
    {
        var p = QKitConvert.ToUnityPosition(new Vector3(48, 24, 96), 24);
        Assert.Equal(-1f, p.x);
        Assert.Equal(4f, p.y);
        Assert.Equal(2f, p.z);
    }

    [Fact]
    public void ToUnityDirection_QuakeUpIsUnityUp()
    {
        var d = QKitConvert.ToUnityDirection(0, 0, 1);
        Assert.Equal(0f, d.x);
        Assert.Equal(1f, d.y);
        Assert.Equal(0f, d.z);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public void Parsing_IgnoresTheCurrentCulture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        try
        {
            Assert.True(QKitConvert.TryParseFloat("0.5", out var f));
            Assert.Equal(0.5f, f);

            Assert.True(QKitConvert.TryParseVector3("1.5  -2 3", out var v));
            Assert.Equal(1.5f, v.x);
            Assert.Equal(-2f, v.y);
            Assert.Equal(3f, v.z);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1 2")]
    [InlineData("1 x 3")]
    public void TryParseVector3_RejectsInvalidValues(string? value)
    {
        Assert.False(QKitConvert.TryParseVector3(value!, out _));
    }
}
