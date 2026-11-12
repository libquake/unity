using System.Globalization;
using UnityEngine;

namespace QuakeKit
{
    /// <summary>
    /// Conversions between Quake and Unity. Quake is z up and right handed, Unity is y up and left handed:
    /// (x, y, z) -> (-y, z, x), positions are divided by the map's inverse scale.
    /// Map values always use '.' as decimal separator, so parsing ignores the current culture.
    /// </summary>
    public static class QKitConvert
    {
        public static Vector3 ToUnityDirection(float x, float y, float z) => new Vector3(-y, z, x);

        public static Vector3 ToUnityDirection(Vector3 quake) => ToUnityDirection(quake.x, quake.y, quake.z);

        public static Vector3 ToUnityPosition(float x, float y, float z, float inverseScale) =>
            new Vector3(-y / inverseScale, z / inverseScale, x / inverseScale);

        public static Vector3 ToUnityPosition(Vector3 quake, float inverseScale) =>
            ToUnityPosition(quake.x, quake.y, quake.z, inverseScale);

        public static bool TryParseFloat(string value, out float result) =>
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

        /// <summary>
        /// Parses "x y z" without converting the axes, for colors and similar values.
        /// </summary>
        public static bool TryParseVector3(string value, out Vector3 result)
        {
            result = Vector3.zero;
            if (value == null)
                return false;
            var parts = value.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3 || !TryParseFloat(parts[0], out var x) || !TryParseFloat(parts[1], out var y) ||
                !TryParseFloat(parts[2], out var z))
                return false;
            result = new Vector3(x, y, z);
            return true;
        }
    }
}
