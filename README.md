# MaterialConverter
## Description
Allows you to convert materials by remapping shader properties.
This is less automated than the built-in material conversions provided by LWRP and HDRP, but will work with custom shaders.
The tool will convert *all* materials in your project that use the specified source shaders (you can preview this list with the `Material List` button.

## Installation
This is set up as a UPM package, so you can add to your project either via Package Manager's `Add package from git URL` [https://github.com/TeckUnity/MaterialConverter.git], or grab the repo and `Add package from disk`.

## Instructions
Select one or more materials and/or shaders, and right-click to pull up the context menu (alternately, pull down the `Assets` menu). Select `Convert Material(s)`.

You will be presented with a list of shaders, and a dropdown for each where you can select a target shader to convert your materials to use. After selecting a shader, you can then specify how to map each property in the source shader to the target shader. If there are any properties in the target shader that match the source shader (name and type), those will automatically map.

Then simply hit `Convert Materials`. The `Material List` button lets you preview the materials that will be converted.

### Examples
Converting from Custom Shader to LWRP/Lit:

<img src="https://i.imgur.com/oGBFBq7.gif" width="49%" />

Converting from LWRP/Lit to Custom Shader:

<img src="https://i.imgur.com/wASx57X.gif" width="49%" />
