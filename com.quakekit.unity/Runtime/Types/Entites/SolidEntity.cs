using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuakeKit
{
    [Serializable]
    public class SolidEntity
    {
        public string className;
        public GameObject prefab;

        public static void SetupPrefab(GameObject go, Dictionary<string, string> attributes, float inverseScale = 1)
        {
            EntityAttributes.Apply(go, attributes, inverseScale);
        }
    }
}
