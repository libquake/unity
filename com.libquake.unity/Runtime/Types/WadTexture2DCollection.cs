using System;
using System.Collections.Generic;
using UnityEngine;

namespace LibQuake
{
    public class WadTexture2DCollection : ScriptableObject
    {
        /// <summary>Suffix of the emission map of a texture, e.g. "lava1_emission".</summary>
        public const string EmissionSuffix = "_emission";

        /// <summary>Suffix of the cloud layer of a sky, e.g. "sky1_front".</summary>
        public const string SkyFrontSuffix = "_front";

        /// <summary>A WAD texture with all its layers.</summary>
        [Serializable]
        public class Entry
        {
            public string name;
            /// <summary>The texture, for skies the back layer.</summary>
            public Texture2D texture;
            /// <summary>Fullbright pixels, null if the texture has none.</summary>
            public Texture2D emission;
            /// <summary>Sky cloud layer, alpha 0 where the back layer shows. Null for other textures.</summary>
            public Texture2D skyFront;
            /// <summary>'{' texture, transparent pixels need an alpha tested material.</summary>
            public bool hasAlpha;
        }

        public List<Entry> textures = new List<Entry>();

        public string wadName;

        /// <summary>The texture with this name (case insensitive), null if the WAD does not have it.</summary>
        public Entry Find(string name)
        {
            foreach (var entry in textures)
            {
                if (entry != null && entry.texture != null &&
                    string.Equals(entry.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>Every texture of the collection, layers included.</summary>
        public IEnumerable<Texture2D> AllTextures()
        {
            foreach (var entry in textures)
            {
                if (entry == null) continue;
                if (entry.texture != null) yield return entry.texture;
                if (entry.emission != null) yield return entry.emission;
                if (entry.skyFront != null) yield return entry.skyFront;
            }
        }
    }
}
