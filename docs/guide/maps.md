# Maps

## The imported prefab

```text
worldSpawn              mesh, MeshCollider, MapLightmap, EntityEventBus
├── liquids             one convex trigger collider per liquid brush
├── light               a folder per entity class
│   ├── light_0
│   └── light_1
└── func_door
    └── func_door_0     mesh, colliders, components of its prefab
```

The root is the `worldspawn` entity. Its mesh has one submesh per texture, its collider is built from the
brush hulls. Every other entity goes into a folder named after its class and is called `classname_N`,
numbered in file order.

Meshes, collision meshes, materials and the lightmap are sub assets of the map. Materials are created per
import, one per texture, and shared by all entities of the map.

## Geometry

libquake builds the meshes, Unity only converts the axes. With `enableCSG` on, faces hidden inside other brushes
of the same entity are removed. `func_group` and `func_detail` are separate entities and are not merged
with the world.

Faces with tool textures are not rendered:

| Texture | Surface |
|---|---|
| `clip`, `trigger` | collision only |
| `skip`, `hint`, `hintskip`, `nodraw`, `caulk`, `origin` | neither drawn nor solid |

The names for clip and skip can be changed in the map config (`clipTexture`, `skipTexture`). Sky, alpha and
liquid textures are covered in [Textures](textures.md).

Lightmap uvs (uv2) come from libquake on every import. Do not let Unity generate lightmap uvs for the
meshes, that breaks the lightmap.

## Collision

| Entity | Collider |
|---|---|
| worldspawn | one MeshCollider with all brushes, clip brushes included, liquids excluded |
| liquids of the worldspawn | one convex trigger per brush, below `liquids` |
| solid entity, plain MeshCollider | one MeshCollider with all its brushes |
| solid entity, collider set to trigger or convex | one convex MeshCollider per brush |

Unity cannot use a non convex mesh as a trigger, so triggers get a collider per brush. Whether an entity
gets a collider at all and how it is set up comes from its prefab, see [Entities](entities.md).

## Reimports

A map is reimported when the file or its map config changes. Wads, texture folders and the material folder
are only read during the import, after changing them reimport the map by hand.

Prefab overrides in a scene survive a reimport as long as the object names stay the same. Entity names
depend on their order in the map, so adding an entity of the same class in the middle renumbers the ones
after it.
