using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LibQuake
{
    [System.Serializable]
    public class EntityEventEntry
    {
        public string name;
        public UnityEvent unityEvent = new UnityEvent();

        public EntityEventEntry(string targetName)
        {
            name = targetName;
        }
    }

    public class EntityEventBus : MonoBehaviour
    {
        public List<EntityEventEntry> eventList = new List<EntityEventEntry>();

        public void FireEvent(string targetName)
        {
            FindEvent(targetName)?.Invoke();
        }

        public UnityEvent FindEvent(string targetName)
        {
            foreach (var ev in eventList)
            {
                if (ev.name == targetName) return ev.unityEvent;
            }
            return null;
        }

        /// <summary>
        /// Registers a persistent listener, creating the event for targetName if needed.
        /// </summary>
        public UnityEvent GetOrAddEvent(string targetName)
        {
            var ev = FindEvent(targetName);
            if (ev != null)
                return ev;
            var entry = new EntityEventEntry(targetName);
            eventList.Add(entry);
            return entry.unityEvent;
        }
    }
}
