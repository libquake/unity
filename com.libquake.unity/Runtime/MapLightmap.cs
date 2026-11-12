using UnityEngine;

namespace LibQuake
{
    /// <summary>
    /// Binds the lightmap of an imported map to all renderers below this object. Materials using
    /// LibQuake/Lightmapped read it from uv2.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class MapLightmap : MonoBehaviour
    {
        private static readonly int LightmapId = Shader.PropertyToID("_LQLightmap");

        public Texture2D lightmap;

        private MaterialPropertyBlock _block;

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        private void OnDisable()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.SetPropertyBlock(null);
        }

        /// <summary>
        /// Call again after adding renderers at runtime or after replacing the texture.
        /// </summary>
        public void Apply()
        {
            if (lightmap == null)
                return;
            _block ??= new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                r.GetPropertyBlock(_block);
                _block.SetTexture(LightmapId, lightmap);
                r.SetPropertyBlock(_block);
            }
        }
    }
}
