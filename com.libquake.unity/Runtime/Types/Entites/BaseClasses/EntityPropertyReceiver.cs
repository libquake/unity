using UnityEngine;

namespace LibQuake
{
    public class EntityPropertyReceiver : MonoBehaviour
    {
        [HideInInspector]
        public float inverseScale = 1;

        /// <summary>
        /// Parses a Quake "x y z" vector and converts it to Unity axes (no scaling).
        /// </summary>
        public static bool ToVector3(string val, out Vector3 vec3)
        {
            if (!QuakeConvert.TryParseVector3(val, out var quake))
            {
                vec3 = Vector3.zero;
                return false;
            }

            vec3 = QuakeConvert.ToUnityDirection(quake);
            return true;
        }

        public static Vector3 ToVector3(string val)
        {
            ToVector3(val, out var vec);
            return vec;
        }

        public virtual void OnProperty(string name, string value)
        { }
    }
}
