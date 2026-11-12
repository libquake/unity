using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LibQuake
{
    /// <summary>
    /// Project wide LibQuake settings, holds the map config used by maps without their own.
    /// </summary>
    class ProjectSettingsData : ScriptableObject
    {
        public const string SettingsPath = "Assets/LibQuakeProjectSettingsData.asset";
        private const string MapConfigDataPath = "Assets/DefaultLibQuakeMapConfigData.asset";

        [SerializeField]
        private MapConfigData defaultMapConfigData;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += EnsureSettingsExist;
        }

        /// <summary>
        /// Creates the settings if they are missing. Not allowed during an asset import.
        /// </summary>
        public static void EnsureSettingsExist()
        {
            if (GetSettings() == null)
                CreateSettings();
        }

        private static void CreateSettings()
        {
            var settings = CreateInstance<ProjectSettingsData>();
            settings.defaultMapConfigData = CreateInstance<MapConfigData>();
            settings.defaultMapConfigData.ApplyDefaults();

            AssetDatabase.CreateAsset(settings.defaultMapConfigData, MapConfigDataPath);
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();

            Debug.Log("[LibQuake] Created default project settings and map configuration.");
        }

        /// <summary>
        /// The settings, or null if they do not exist yet. Safe to call during an asset import.
        /// </summary>
        public static ProjectSettingsData GetSettings()
        {
            return AssetDatabase.LoadAssetAtPath<ProjectSettingsData>(SettingsPath);
        }

        /// <summary>
        /// Gets or creates the settings. Not allowed during an asset import, use GetSettings() there.
        /// </summary>
        public static ProjectSettingsData GetOrCreateSettings()
        {
            EnsureSettingsExist();
            return GetSettings();
        }

        public MapConfigData GetMapConfigData()
        {
            return defaultMapConfigData;
        }
    }

    static class ProjectSettingsProvider
    {
        [MenuItem("Tools/LibQuake/Initialize Project Settings")]
        public static void InitializeSettings()
        {
            var settings = ProjectSettingsData.GetOrCreateSettings();
            if (settings != null)
            {
                EditorGUIUtility.PingObject(settings);
                Selection.activeObject = settings;
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            SerializedObject settings = null;
            return new SettingsProvider("Project/LibQuake", SettingsScope.Project)
            {
                label = "LibQuake",
                activateHandler = (searchContext, rootElement) =>
                    settings = new SerializedObject(ProjectSettingsData.GetOrCreateSettings()),
                guiHandler = searchContext =>
                {
                    settings.Update();
                    EditorGUILayout.PropertyField(settings.FindProperty("defaultMapConfigData"),
                        new GUIContent("Map Import Config"));
                    settings.ApplyModifiedPropertiesWithoutUndo();
                },
                keywords = new HashSet<string>(new[] { "Map Import Config", "Quake" })
            };
        }
    }
}
