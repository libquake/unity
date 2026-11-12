using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace QuakeKit
{
    /// <summary>
    /// Project wide QuakeKit settings, holds the map config used by maps without their own.
    /// </summary>
    class QKitProjectSettingsData : ScriptableObject
    {
        public const string SettingsPath = "Assets/QuakeKitProjectSettingsData.asset";
        private const string MapConfigDataPath = "Assets/DefaultQuakeKitMapConfigData.asset";

        [SerializeField]
        private QKitMapConfigData defaultQKitMapConfigData;

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
            var settings = CreateInstance<QKitProjectSettingsData>();
            settings.defaultQKitMapConfigData = CreateInstance<QKitMapConfigData>();
            settings.defaultQKitMapConfigData.ApplyDefaults();

            AssetDatabase.CreateAsset(settings.defaultQKitMapConfigData, MapConfigDataPath);
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();

            Debug.Log("[QuakeKit] Created default project settings and map configuration.");
        }

        /// <summary>
        /// The settings, or null if they do not exist yet. Safe to call during an asset import.
        /// </summary>
        public static QKitProjectSettingsData GetSettings()
        {
            return AssetDatabase.LoadAssetAtPath<QKitProjectSettingsData>(SettingsPath);
        }

        /// <summary>
        /// Gets or creates the settings. Not allowed during an asset import, use GetSettings() there.
        /// </summary>
        public static QKitProjectSettingsData GetOrCreateSettings()
        {
            EnsureSettingsExist();
            return GetSettings();
        }

        public QKitMapConfigData GetMapConfigData()
        {
            return defaultQKitMapConfigData;
        }
    }

    static class QKitSettingsProvider
    {
        [MenuItem("Tools/QuakeKit/Initialize Project Settings")]
        public static void InitializeSettings()
        {
            var settings = QKitProjectSettingsData.GetOrCreateSettings();
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
            return new SettingsProvider("Project/QuakeKitSettings", SettingsScope.Project)
            {
                label = "QuakeKit",
                activateHandler = (searchContext, rootElement) =>
                    settings = new SerializedObject(QKitProjectSettingsData.GetOrCreateSettings()),
                guiHandler = searchContext =>
                {
                    settings.Update();
                    EditorGUILayout.PropertyField(settings.FindProperty("defaultQKitMapConfigData"),
                        new GUIContent("Map Import Config"));
                    settings.ApplyModifiedPropertiesWithoutUndo();
                },
                keywords = new HashSet<string>(new[] { "Map Import Config", "Quake" })
            };
        }
    }
}
