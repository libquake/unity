using System.Collections.Generic;
using UnityEngine;

namespace QuakeKit
{
    /// <summary>
    /// Hands the key/value pairs of a map entity to the QuakeKit components of its prefab.
    /// </summary>
    public static class EntityAttributes
    {
        public static void Apply(GameObject go, IReadOnlyDictionary<string, string> attributes, float inverseScale)
        {
            var parser = go.GetComponent<EntityPropertyReceiver>();
            var receiver = go.GetComponent<EntityEventReceiver>();
            var emitter = go.GetComponent<EntityEventEmitter>();

            if (parser != null)
                parser.inverseScale = inverseScale;

            foreach (var prop in attributes)
            {
                switch (prop.Key)
                {
                    case "target" when emitter != null:
                        emitter.SetTarget(prop.Value);
                        break;
                    case "targetname" when receiver != null:
                        receiver.targetName = prop.Value;
                        break;
                }

                if (prop.Key != "classname" && parser != null)
                    parser.OnProperty(prop.Key, prop.Value);
            }
        }
    }
}
