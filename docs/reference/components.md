# Components

Runtime types in the `LibQuake` namespace, assembly `com.libquake.unity.runtime`. They work in builds, the
importers and everything that touches libquake are editor only.

## MapLightmap

On the root of a lightmapped map. Binds `lightmap` to every renderer below it as `_LQLightmap`, through a
material property block. Runs in edit mode.

| Member | |
|---|---|
| `Texture2D lightmap` | the baked lightmap of the map |
| `void Apply()` | binds the lightmap again, after adding renderers or replacing the texture |

## EntityEventBus

On the root of every map. One UnityEvent per target name.

| Member | |
|---|---|
| `List<EntityEventEntry> eventList` | `name` and `unityEvent` per target name |
| `void FireEvent(string targetName)` | invokes the event, nothing if there is none |
| `UnityEvent FindEvent(string targetName)` | the event, null if there is none |
| `UnityEvent GetOrAddEvent(string targetName)` | the event, created if needed |

## EntityEventEmitter

Fires `target` on the map's event bus when a collider enters the trigger on the same object.

| Member | |
|---|---|
| `string target` | from the entity's `target` key |
| `bool once` | fire only the first time |
| `void SetTarget(string targetName)` | |
| `void SetLocalEventBus(EntityEventBus bus)` | set by the importer |
| `protected virtual void TriggerEntered(Collider col)` | override to filter, call the base to fire |

## EntityEventReceiver

| Member | |
|---|---|
| `string targetName` | from the entity's `targetname` key |
| `virtual void OnTrigger()` | called when an emitter fires `targetName`, logs by default |

## EntityPropertyReceiver

Base for components that read entity keys.

| Member | |
|---|---|
| `float inverseScale` | the map's scale, set before the keys |
| `virtual void OnProperty(string name, string value)` | once per key except `classname`, at import time |
| `static bool ToVector3(string value, out Vector3 v)` | parses `"x y z"` and converts the axes, no scaling |
| `static Vector3 ToVector3(string value)` | same, zero if the value does not parse |

## QuakePointLight

`EntityPropertyReceiver` for `light` entities. Sets range and intensity of the `Light` on the same object
from the `light` key.

## QuakeConvert

| Member | |
|---|---|
| `Vector3 ToUnityDirection(float x, float y, float z)` | `(x, y, z)` to `(-y, z, x)`, also with a `Vector3` |
| `Vector3 ToUnityPosition(float x, float y, float z, float inverseScale)` | the same, divided by the scale, also with a `Vector3` |
| `bool TryParseFloat(string value, out float result)` | invariant culture |
| `bool TryParseVector3(string value, out Vector3 result)` | `"x y z"`, no axis conversion, for colors and the like |

## WadTexture2DCollection

The imported wad, see [Textures](../guide/textures.md#wads).

| Member | |
|---|---|
| `List<Entry> textures` | `name`, `texture`, `emission`, `skyFront`, `hasAlpha` |
| `string wadName` | asset path of the wad |
| `Entry Find(string name)` | case insensitive, null if missing |
| `IEnumerable<Texture2D> AllTextures()` | every texture and layer |

## LmpPalette

The imported `.lmp`: `colors` (256), `brightColors` (224 to 254) and `transparentColor` (255).
