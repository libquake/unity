# Entities

Entities become GameObjects through prefabs. The map config maps class names to prefabs, the importer
instantiates the prefab and hands it the entity's keys.

## Point entities

`pointEntities` in the map config lists class name and prefab. Point entities without an entry are skipped.
The default config maps `light` to the package's `PointLight` prefab.

The object is placed at the entity's `origin` and turned by its `angle` around the up axis.

## Solid entities

Every brush entity except the worldspawn gets an object, from the first match of:

1. an entry in `solidEntities` with its class name
2. `defaultTriggerObject` for class names containing `trigger`
3. `defaultSolidObject`

The prefab decides what the entity becomes. The importer fills what it finds on the prefab's root:

| Component | Filled with |
|---|---|
| MeshFilter | the entity's mesh, added when missing |
| MeshRenderer | the materials. Without one the entity is invisible |
| MeshCollider | the brush hulls, see [Collision](maps.md#collision). Without one there is no collision |

The package prefabs:

| Prefab | Components |
|---|---|
| `Solids/Default` | MeshRenderer, MeshCollider |
| `Solids/Trigger` | MeshCollider (convex, trigger), `EntityEventEmitter`. Not rendered |
| `Point/PointLight` | Light, `QuakePointLight` |

Entity origins are not applied to solid entities, their vertices are already in world space and the object
sits at zero.

## Keys

`EntityPropertyReceiver` on a prefab gets every key of the entity except `classname` through
`OnProperty(name, value)`. Subclass it for your own entities:

```csharp
using LibQuake;
using UnityEngine;

public class Rotator : EntityPropertyReceiver
{
    public float speed = 90;

    public override void OnProperty(string name, string value)
    {
        if (name == "speed" && QuakeConvert.TryParseFloat(value, out var s))
            speed = s;
    }

    void Update() => transform.Rotate(Vector3.up, speed * Time.deltaTime);
}
```

Values are strings as in the map file. `QuakeConvert.TryParseFloat` and `TryParseVector3` parse them
independent of the system culture, `EntityPropertyReceiver.ToVector3` also converts the axes to Unity's.
Positions need the scale on top, `inverseScale` holds the map's.

`OnProperty` runs during the import. Whatever it sets on serialized fields is stored in the prefab.

## Events

`target` and `targetname` are wired up at import time:

- `EntityEventEmitter` takes the entity's `target`. It fires when a collider enters its trigger, with `once`
  only the first time.
- `EntityEventReceiver` takes the entity's `targetname`. Override `OnTrigger()` to react.
- `EntityEventBus` on the prefab root holds one UnityEvent per target name. The importer registers every
  receiver there as a persistent listener and hands the bus to every emitter.

A `trigger_once` with `"target" "door1"` and a `func_door` with `"targetname" "door1"` work once the door's
prefab has a receiver:

```csharp
using LibQuake;
using UnityEngine;

public class Door : EntityEventReceiver
{
    public override void OnTrigger() => gameObject.SetActive(false);
}
```

Add it to a prefab, map the prefab to `func_door` in `solidEntities` and reimport. The listeners show up on
the `EntityEventBus` in the inspector, more can be added there by hand. `EntityEventBus.FireEvent(name)`
fires an event from code.
