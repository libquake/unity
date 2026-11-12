using UnityEditor;
using UnityEditor.AssetImporters;

namespace LibQuake
{
    [CustomEditor(typeof(QMapAssetImporter))]
    public class QMapAssetImporterEditor : ScriptedImporterEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Lightmap UVs (uv2) and the lightmap come from libquake on every import. The layout stays " +
                "stable when the map changes, so do not regenerate uv2 with Unity, that breaks the lightmap.",
                MessageType.Info);
        }
    }
}
