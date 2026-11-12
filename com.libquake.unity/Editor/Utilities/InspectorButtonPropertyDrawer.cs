using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LibQuake
{
    /// <summary>
    /// Draws a field as a button that calls MethodName on the inspected object. Only works on serialized fields.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class InspectorButtonAttribute : PropertyAttribute
    {
        public const float DefaultButtonWidth = 100;

        public readonly string MethodName;

        public float ButtonWidth { get; set; } = DefaultButtonWidth;

        public InspectorButtonAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }

    [CustomPropertyDrawer(typeof(InspectorButtonAttribute))]
    public class InspectorButtonPropertyDrawer : PropertyDrawer
    {
        private MethodInfo _eventMethodInfo;

        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            var buttonAttribute = (InspectorButtonAttribute)attribute;
            var buttonRect = new Rect(position.x + (position.width - buttonAttribute.ButtonWidth) * 0.5f, position.y,
                buttonAttribute.ButtonWidth, position.height);
            if (!GUI.Button(buttonRect, label.text))
                return;

            var ownerType = prop.serializedObject.targetObject.GetType();
            _eventMethodInfo ??= ownerType.GetMethod(buttonAttribute.MethodName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (_eventMethodInfo != null)
                _eventMethodInfo.Invoke(prop.serializedObject.targetObject, null);
            else
                Debug.LogWarning($"InspectorButton: Unable to find method {buttonAttribute.MethodName} in {ownerType}");
        }
    }
}
