using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuakeKit
{
    [Serializable]
    public class PointEntity
    {
        public string className;
        public GameObject prefab;

        public void SetupPrefab(GameObject go, Dictionary<string, string> attributes, Vector3 origin, float angle,
            float inverseScale)
        {
            go.transform.position = QKitConvert.ToUnityPosition(origin, inverseScale);
            go.transform.Rotate(Vector3.up, angle + 90, Space.Self);
            EntityAttributes.Apply(go, attributes, inverseScale);
        }
    }
}
