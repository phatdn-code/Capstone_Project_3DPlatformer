Support: Discord (https://discord.gg/K88zmyuZFD)
Online Documentation: https://inabstudios.gitbook.io/better-fog/

Offline Docs:

### IMPORTING

After downloading the asset, import the appropriate .unitypackage file based on your Unity version and SRP (either Built-in.unitypackage, URP 6.unitypackage, or URP 2022.unitypackage).

### Demo Scenes

Demo scenes can be found: \Common (URP or Built-In)\Demo Scenes

HOW TO USE

BUILT-IN

Ensure you have the Post Processing Stack V2 installed in your project. 
You can then easily add the Better Fog effect to your volumes.

You need one of the following in your scene:
A directional light with shadows enabled.
The CameraDepth.cs script added to your camera to enable the camera depth texture.


URP

Add BetterFogFeature to your URP data asset.
Set the Event property to "Before Rendering Post Processing".
Now you can add the Better Fog effect to your post-processing volume.

If you encounter issues setting up, you can use the provided URP settings asset. Adjust it via the Graphics section in your project settings. 
Ensure that the render asset is not overridden in the Quality tab.
Make sure in Quality tab render asset is not overriden

### ISSUES
If you encounter any difficulties using, implementing, or understanding the asset, please feel free to contact me.
