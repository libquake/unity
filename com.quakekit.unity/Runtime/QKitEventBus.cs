using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace QuakeKit
{
    [System.Serializable]
    public class QKitEventEntry
    {
        public string name;
        public UnityEvent unityEvent = new UnityEvent();

        public QKitEventEntry(string targetName)
        {
            name = targetName;
        }
    }

    public class QKitEventBus : MonoBehaviour
    {
        public List<QKitEventEntry> eventList = new List<QKitEventEntry>();

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
            var entry = new QKitEventEntry(targetName);
            eventList.Add(entry);
            return entry.unityEvent;
        }
    }
}
