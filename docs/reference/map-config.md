# Map config

`MapConfigData`, a ScriptableObject. Create one with Create > LibQuake > Configs > MapConfig and assign it in
the import settings of a map, or set the project default under Project Settings > LibQuake. A new config
starts with the package prefabs and the `light` entity.

Maps using a config are reimported when it changes.

## General

| Field | Default | |
|---|---|---|
| `float inverseScale` | 24 | Quake units per Unity unit |
| `bool enableCSG` | on | remove faces hidden inside other brushes of the same entity |

## Lighting

| Field | Default | |
|---|---|---|
| `bool bakeLightmap` | on | bake the map lights and use the `LibQuake/Lightmapped` shader, see [Lighting](../guide/lighting.md) |
| `float luxelSize` | 16 | Quake units per lightmap texel, at least 1 |

## Materials and folders

| Field | Default | |
|---|---|---|
| `string textureFolder` | `Assets/Textures/` | textures named like the map textures |
| `string materialFolder` | `Assets/Materials/` | materials named like the map textures, used instead of the base material |

Either can be empty to skip that step of the [lookup](../guide/textures.md#lookup).

## Special textures

| Field | Default | |
|---|---|---|
| `string clipTexture` | `clip` | faces with this texture only collide |
| `string skipTexture` | `skip` | faces with this texture are dropped |
| `bool doubleSidedAlpha` | off | draw both sides of `{` faces |

## Entities

| Field | |
|---|---|
| `GameObject defaultSolidObject` | prefab for solid entities without an entry |
| `GameObject defaultTriggerObject` | prefab for solid entities with `trigger` in the class name and without an entry |
| `List<PointEntity> pointEntities` | class name and prefab per point entity class |
| `List<SolidEntity> solidEntities` | class name and prefab per solid entity class |

See [Entities](../guide/entities.md).
