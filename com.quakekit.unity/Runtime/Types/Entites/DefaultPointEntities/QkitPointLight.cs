using UnityEngine;

namespace QuakeKit
{
    public class QkitPointLight : EntityPropertyReceiver
    {
        [SerializeField] private Light lightComponent;

        public override void OnProperty(string propertyName, string propertyValue)
        {
            if (propertyName != "light" || !QKitConvert.TryParseFloat(propertyValue, out var quakeLightValue))
                return;

            lightComponent = GetComponent<Light>();
            float rangeQuakeUnits = (-1f + Mathf.Sqrt(1f + 1024f * quakeLightValue)) * 0.5f;
            lightComponent.range = rangeQuakeUnits / inverseScale;
            lightComponent.intensity = quakeLightValue * 6.283f / (inverseScale * inverseScale);
        }
    }
}
