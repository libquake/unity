# LibQuake for Unity

Unity package that imports Quake 1 `.map` and `.wad` files as assets. A map becomes a prefab with meshes,
colliders, a baked lightmap and one object per entity. It is built on [libquake](https://github.com/libquake/libquake),
which does the parsing, CSG, uvs and light baking.

Edit the map in [TrenchBroom](https://trenchbroom.github.io), save, and Unity reimports it.

| | |
|---|---|
| Unity | 2021.1 or newer |
| Render pipelines | built-in, URP, HDRP (no baked lightmap shader on HDRP) |
| Platforms | the importer runs in the editor on Windows, macOS and Linux, the result is plain Unity assets |

## Install

Add the package by git URL in the Package Manager (`+` > Add package from git URL):

```text
https://github.com/libquake/unity.git?path=/com.libquake.unity
```

Without git, download the repository and add `com.libquake.unity/package.json` as a local package.

## What gets imported

| File | Becomes |
|---|---|
| `.map` | prefab: worldspawn mesh and collision, lightmap, entities grouped by class |
| `.wad` | `WadTexture2DCollection` with every texture, its fullbright emission map and sky layers |
| `.lmp` | `LmpPalette` with the 256 colors of a Quake palette |

Start with [Getting started](guide/getting-started.md).
