using System.IO;
using UnityEditor;
using UnityEngine.Rendering;

namespace LibQuake
{
    public static class QPathTools
    {
        /// <summary>
        /// Path of the asset of the given type named like the texture, below folder. Texture names may contain
        /// sub folders ("base/wall1"). Empty if there is none.
        /// </summary>
        public static string GetTextureAssetPath(string texName, string folder, string type)
        {
            var fullPath = folder;
            var segments = texName.Split('/');
            texName = segments[segments.Length - 1];
            for (int i = 0; i < segments.Length - 1; i++)
            {
                fullPath += "/" + segments[i];
            }

            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                return "";
            }

            // the search matches substrings, pick the exact name
            var guids = AssetDatabase.FindAssets($"\"{texName}\" t:{type}", new[] { fullPath });
            if (guids.Length == 1)
                return AssetDatabase.GUIDToAssetPath(guids[0]);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == texName)
                    return path;
            }

            return "";
        }

        public static void GetPipelineFolder(out string pipelinePath, out string baseColorShaderParam)
        {
            pipelinePath = "Base";
            baseColorShaderParam = "_MainTex";
            if (GraphicsSettings.defaultRenderPipeline == null) return;
            switch (GraphicsSettings.defaultRenderPipeline.GetType().Name)
            {
                case "UniversalRenderPipelineAsset":
                    pipelinePath = "URP";
                    baseColorShaderParam = "_BaseMap";
                    break;
                case "HDRenderPipelineAsset":
                    pipelinePath = "HDRP";
                    baseColorShaderParam = "_BaseColorMap";
                    break;
            }
        }
    }
}
