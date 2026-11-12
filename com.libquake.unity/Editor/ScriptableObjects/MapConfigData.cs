using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LibQuake
{
    [CreateAssetMenu(fileName = "QuakeMapConfigData", menuName = "LibQuake/Configs/MapConfig", order = 1)]
    public class MapConfigData : ScriptableObject
    {
        private const string PackagePath = "Packages/com.libquake.unity/Assets/";

        [Header("General")]
        public float inverseScale = 24;
        public bool enableCSG = true;
        [Header("Lighting")]
        [Tooltip("Bake the map lights with libquake and use the LibQuake/Lightmapped shader.")]
        public bool bakeLightmap = true;
        [Tooltip("World units per lightmap texel, 16 matches Quake.")]
        [Min(1)] public float luxelSize = 16;
        [Header("Materials and Folders")]
        public Material defaultBaseMaterial;
        public string textureFolder = "Assets/Textures/";
        public string materialFolder = "Assets/Materials/";
        [Header("Special Textures")]
        public string clipTexture = "clip";
        public string skipTexture = "skip";
        [Tooltip("Draw both sides of '{' (alpha) faces. Off like Quake: fence brushes have '{' on both sides. On for " +
                 "'{' on one face and skip on the other.")]
        public bool doubleSidedAlpha;
        [Header("Entities")]
        public GameObject defaultSolidObject;
        public GameObject defaultClipObject;
        public GameObject defaultTriggerObject;
        public List<PointEntity> pointEntities;
        public List<SolidEntity> solidEntities;

        private void Reset()
        {
            ApplyDefaults();
        }

        /// <summary>
        /// Package prefabs and materials, plus the default light entity.
        /// </summary>
        internal void ApplyDefaults()
        {
            defaultSolidObject = AssetDatabase.LoadAssetAtPath<GameObject>(PackagePath + "Prefabs/Solids/Default.prefab");
            defaultClipObject = AssetDatabase.LoadAssetAtPath<GameObject>(PackagePath + "Prefabs/Solids/Clip.prefab");
            defaultTriggerObject = AssetDatabase.LoadAssetAtPath<GameObject>(PackagePath + "Prefabs/Solids/Trigger.prefab");
            defaultBaseMaterial = MaterialManager.Instance.GetBaseMaterial();

            pointEntities = new List<PointEntity>
            {
                new PointEntity
                {
                    className = "light",
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PackagePath + "Prefabs/Point/PointLight.prefab")
                }
            };
            solidEntities = new List<SolidEntity>();
        }
    }
}
