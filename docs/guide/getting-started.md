# Getting started

## Project settings

After the package is installed, it creates two assets on the next editor load:

- `Assets/LibQuakeProjectSettingsData.asset`, the project settings
- `Assets/DefaultLibQuakeMapConfigData.asset`, the map config used by every map without its own

They show up under Project Settings > LibQuake. If they are missing, Tools > LibQuake > Initialize Project
Settings creates them again.

The map config holds everything an import needs: scale, lighting, texture folders and the prefabs for
entities. The [map config reference](../reference/map-config.md) lists every field. More configs can be
created with Create > LibQuake > Configs > MapConfig and assigned per map in its import settings.

## TrenchBroom

Set the game path of TrenchBroom's Generic game to the project's `Assets` folder (Preferences > Generic),
then create maps with the Generic game in Valve or Standard format. Add the wad files from `Assets/` in the
map's texture settings.

Save the map somewhere below `Assets/`. TrenchBroom's `autosave/` folders are ignored by the importer.

## Importing a map

1. Put the wad files the map uses into `Assets/` and let Unity import them first. See
   [Textures](textures.md).
2. Save the `.map` below `Assets/`. Unity imports it as a prefab.
3. Drag the prefab into a scene.

Every save in TrenchBroom reimports the map. Changes to its map config reimport it as well. Textures and
wads are looked up while importing, so a map imported before its wads were there needs a manual reimport
(right click > Reimport).

Textures that cannot be found are reported in the console and get a 64x64 size, the faces stay untextured.

## Scale and axes

Quake is z up, Unity y up. Positions are converted as `(x, y, z)` to `(-y, z, x)` and divided by the map
config's `inverseScale`, 24 Quake units per Unity unit by default. The player in Quake is 56 units tall,
32 units are roughly a meter, so an `inverseScale` of 32 gives meters.

`QuakeConvert` does the same conversion at runtime, for values read from entity keys.
