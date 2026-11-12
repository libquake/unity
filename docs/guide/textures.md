# Textures

## Lookup

For every texture name in the map, the importer looks in this order:

1. **Material folder** (`materialFolder`): a material named like the texture. It is used as is, with its
   own shader.
2. **Texture folder** (`textureFolder`): a texture asset named like the texture.
3. **Wads**: the imported wad files. The ones listed in the map's `wad` key come first, then any other wad
   that has the texture. Names are compared case insensitive.

A texture name with a path, like `base/wall1`, is looked up as `wall1` in the sub folder `base` of the
material and texture folder. Material and texture can be combined: a material in the material folder
without a texture of its own gets the one from the texture folder or the wad.

## Wads

`.wad` files (WAD2, as used by Quake and TrenchBroom) are imported as a `WadTexture2DCollection`. Each entry
has:

| | |
|---|---|
| `texture` | RGBA, point filtered, with mipmaps. For skies the back layer |
| `emission` | the fullbright pixels, black elsewhere. Only for textures that have some |
| `skyFront` | the cloud layer of a sky, alpha 0 where the back layer shows |
| `hasAlpha` | `{` texture, palette index 255 is transparent |

Import the wads before the maps that use them. A map imported first finds nothing and needs a reimport.

The wad's import settings have an Export button that writes every texture and layer as png into `folder`
(`Assets/textures` by default). The files are named after the texture with `*` and `+` removed, emission
maps end in `_emission`, sky cloud layers in `_front`.

## Special textures

| Name | |
|---|---|
| `sky...` | sky material with both layers, scrolling like in Quake. Not lit |
| `{...` | alpha tested, transparent where the palette index is 255 |
| `*...` | liquid, drawn with the base material, without the warp. The brush becomes a trigger volume, see [Maps](maps.md) |
| `clip`, `skip` and other tool textures | not drawn, see [Maps](maps.md) |

Alpha faces are culled on the back like in Quake. A fence brush has `{` on both sides and would show its far
side through the holes otherwise. For `{` on one face and `skip` on the others, turn on `doubleSidedAlpha` in
the map config.

Fullbright pixels (palette 224 to 254) go into the material's emission map and keep their color regardless
of the light.

## Render pipelines

The package has base materials for each pipeline in `Assets/Materials/`. The importer picks them by the
render pipeline asset set in the graphics settings:

| Pipeline | Base | Alpha | Sky |
|---|---|---|---|
| built-in | `Base/Base.mat` | `Base/Alpha.mat` | `Base/Sky.mat` |
| URP | `URP/Base.mat` | `URP/Alpha.mat` | `URP/Sky.mat` |
| HDRP | `HDRP/Base.mat` | `HDRP/Alpha.mat` | none, skies use the base material |

With a baked lightmap, materials based on these switch to the `LibQuake/Lightmapped` shader, except on HDRP.
See [Lighting](lighting.md).

## Custom materials

A material in the material folder replaces the base material for its texture. It keeps its shader and
settings, the importer only fills in the base texture if the material has none and the emission map if the
texture has fullbright pixels. Culling and the lightmap shader are left alone.

To use the baked lightmap in a custom shader, read the texture `_LQLightmap` with the second uv set.
