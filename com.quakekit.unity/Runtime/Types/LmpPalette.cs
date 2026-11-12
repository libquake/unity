using System.Collections.Generic;
using UnityEngine;

namespace QuakeKit
{
    public class LmpPalette : ScriptableObject
    {
        /// <summary>Quake draws palette entries 224..254 at full brightness.</summary>
        public const int FullbrightFirst = 224;

        /// <summary>Transparent index on '{' textures.</summary>
        public const int TransparentIndex = 255;

        public List<Color> colors = new List<Color>();
        public List<Color> brightColors = new List<Color>();
        public Color transparentColor;

        public bool isTransparent(Color32 c) { return c == transparentColor; }
        public bool isBrightColor(Color32 c)
        {
            foreach (var bc in brightColors)
            {
                if (c == bc) return true;
            }
            return false;
        }
    }
}
