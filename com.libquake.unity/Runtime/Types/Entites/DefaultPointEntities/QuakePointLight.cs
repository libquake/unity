using UnityEngine;

namespace LibQuake
{
    public class QuakePointLight : EntityPropertyReceiver
    {
        [SerializeField] private Light lightComponent;

        public override void OnProperty(string propertyName, string propertyValue)
        {
            if (propertyName != "light" || !QuakeConvert.TryParseFloat(propertyValue, out var quakeLightValue))
                return;

            lightComponent = GetComponent<Light>();
            float rangeQuakeUnits = (-1f + Mathf.Sqrt(1f + 1024f * quakeLightValue)) * 0.5f;
            lightComponent.range = rangeQuakeUnits / inverseScale;
            lightComponent.intensity = quakeLightValue * 6.283f / (inverseScale * inverseScale);
        }
    }
}
