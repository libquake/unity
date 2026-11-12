using UnityEngine;

namespace QuakeKit
{
    public class EntityEventEmitter : MonoBehaviour
    {
        public string target;
        public bool once;
        protected bool triggered = false;

        [SerializeField] protected QKitEventBus localEventBus;

        public void SetTarget(string targetName)
        {
            target = targetName;
        }

        public void SetLocalEventBus(QKitEventBus eb)
        {
            localEventBus = eb;
        }

        protected virtual void TriggerEntered(Collider col)
        {
            if (string.IsNullOrEmpty(target) || (once && triggered))
                return;
            if (localEventBus == null)
            {
                Debug.LogWarning($"{name}: no event bus, cannot fire '{target}'", this);
                return;
            }

            triggered = true;
            localEventBus.FireEvent(target);
        }

        private void OnTriggerEnter(Collider other)
        {
            TriggerEntered(other);
        }
    }
}
