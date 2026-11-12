using System;
using System.Collections.Generic;
using UnityEngine;

namespace LibQuake
{
    [Serializable]
    public class PointEntity
    {
        public string className;
        public GameObject prefab;

        public void SetupPrefab(GameObject go, Dictionary<string, string> attributes, Vector3 origin, float angle,
            float inverseScale)
        {
            go.transform.position = QuakeConvert.ToUnityPosition(origin, inverseScale);
            go.transform.Rotate(Vector3.up, angle + 90, Space.Self);
            EntityAttributes.Apply(go, attributes, inverseScale);
        }
    }
}
