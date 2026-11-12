# LibQuake for Unity

[![Tests](https://github.com/libquake/unity/actions/workflows/test.yml/badge.svg)](https://github.com/libquake/unity/actions/workflows/test.yml)

## quake map importer for Unity based on [Qodot / FuncGodot](https://github.com/func-godot/func_godot_plugin)
<p align="center">
<img src="media/demo.gif" alt="tb new" width="60%" height=60%/>
</p>

Supported Unity Versions:
* 2021.x
* 2022.x
* 2023.x

Supported Render Pipelines:
* Base
* URP
* HDRP
* your own by replacing the base materials

### What it is
LibQuake for Unity is a package for importing Quake maps made with Trenchbroom etc. It is built on [libquake](https://github.com/libquake/libquake).
The package also comes with a Quake WAD importer to remove the need for extracting textures.
It is heavily inspired on [Qodot / FuncGodot](https://github.com/func-godot/func_godot_plugin), a Godot plugin for working with quake maps. 

### Why is it experimental
The package is in a pretty early stage and is in active development. But the base functionality should work as expected.
Please file an issue if you run into problems. :-)


## Instructions

#### How to install:

you can install it via Unity's [package manager via gitURL](https://docs.unity3d.com/Manual/upm-ui-giturl.html):

```
https://github.com/libquake/unity.git?path=/com.libquake.unity
```

if you don't have git installed in your `PATH` environment, you can also download the package via github and install it as a local package.

The documentation and a guide are at [libquake.github.io/unity](https://libquake.github.io/unity/), the source is in [docs/](docs/).

<br>
<br>

<p float="left">
<img src="media/screens/screen_1.png" width="30%" height="30%"/>
<img src="media/screens/screen_2.png" width="30%" height="30%"/>
<img src="media/screens/screen_3.png" width="30%" height="30%"/>
<img src="media/screens/screen_4.png" width="30%" height="30%"/>
<img src="media/screens/screen_5.png" width="30%" height="30%"/>
</P>

<br>
<br>


## Credits

LibQuake for Unity is inspired by Qodot / FuncGodot.

FuncGodot was created by [Hannah "EMBYR" Crawford](https://embyr.sh/), [Emberlynn Bland](https://github.com/deertears/), and [Tim "RhapsodyInGeek" Maccabe](https://github.com/RhapsodyInGeek), reworked from the [Godot 4 port of Qodot](https://github.com/QodotPlugin/Qodot/tree/main) by Embyr, with contributions from members of the FuncGodot, Qodot, Godot, and Quake Mapping Communities.

Both plugins are based on the original [Qodot for Godot 3.5](https://github.com/QodotPlugin/qodot-plugin/) created by [Josh "Shifty" Palmer](https://twitter.com/ShiftyAxel).

<p align="center"><img src="https://github.com/func-godot/.github/assets/44485952/9ff9cd96-024b-4202-b4a2-611741b81609" alt="Godambler" /></p>
