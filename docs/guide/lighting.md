# Lighting

With `bakeLightmap` on (the default), libquake bakes the lights of the map while importing and the map looks
like in Quake: light entities, their `light`, `_color` and `wait` keys, sunlight from the worldspawn.
The [libquake lighting guide](https://libquake.github.io/libquake/guide/lighting/) explains the baker and its
keys.

The lightmap is a sub asset of the map called `lightmap`. Values follow Quake, 128 is normal brightness, the
shader doubles them so lights can overbright.

## How it is drawn

`MapLightmap` on the root of the prefab binds the lightmap to every renderer below it through a material
property block. Materials based on the package materials use the `LibQuake/Lightmapped` shader:

- albedo times lightmap times 2, in gamma space like Quake
- fullbright pixels from the emission map keep their color
- alpha test for `{` textures
- unlit, works in the built-in pipeline and URP

Unity lights and Unity's own lightmapping have no effect on these materials. Renderers added below the map
at runtime need `MapLightmap.Apply()` to get the lightmap.

HDRP cannot run the shader. The lightmap is still baked, but the HDRP materials ignore it, light the map with
HDRP lights there.

## Luxel size

`luxelSize` is the size of a lightmap texel in Quake units. 16 matches Quake. Smaller values give sharper
shadows and a bigger lightmap, the bake takes longer.

## Without the baked lightmap

With `bakeLightmap` off the materials keep the pipeline's lit shader and the map is lit by Unity. The
`light` entities still become Unity point lights through their prefab (`QuakePointLight` turns the light
value into range and intensity), and the meshes have uv2 for Unity's own lightmapping, but Unity must not
regenerate it.
