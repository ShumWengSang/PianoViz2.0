# PianoViz2.0
Repository for PianoViz, a HoloLens project that aims to replicate the Synthesia experience in AR/MR.
The repository is called PianoViz 2.0 to distinct it from 1.0, which is purely a test base and experimental. 

# Build
## Requirements
- Windows PC
- Visual Studio 2019 installed on the PC
  - Universal Windows Platform development tools (installed through visual studio installer)
  - Desktop development with C++ tools (installed through visual studio installer)
- [Windows 10 SDK (10.0.18362.0)](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/)
- Unity 2019.4.34f1
- HoloLens 1.0
- Bluetooth Low Energy (BLE) MIDI keyboard that is compatible with HoloLens 1.0
  - We use a [KORG Microkey2-25AIR](https://www.amazon.com/Korg-microKEY-air-Bluetooth-Controller/dp/B018ATKGAG?th=1)

The project is already configured to build on Android and HoloLens out of the box via Unity. 

# External Libraries
PianoViz uses a lot of external libraries to run. It is not neccessary to build any of these unless you're trying to modify the libraries directly. Of note are:

- [DotTween](http://dotween.demigiant.com/)
- [Microsoft's Mixed Reality Toolkit 2.7.0](https://docs.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/?view=mrtkunity-2021-05)
- [Microsoft's Spectator View (Personal Branch)](https://github.com/microsoft/MixedReality-SpectatorView) 
  - ARCore 3.1.3
  - ARFoundation 3.1.3
  - ARKit 3.1.3
  - OpenCV (For arUco marker detection)
  - Note: The personal branch contains modification that allows the project to run in Unity 2019.4. Modifications include
    - Update of [Mixed Reality QR](https://www.nuget.org/Packages/Microsoft.MixedReality.QR) to 0.5.3013
    - Update of above dependencies to 3.1.3
    - Modification of [tiff library](http://www.libtiff.org/) in the vcpkg build used by Spectator View
- [BleWinrtDLL (Personal Branch)](https://github.com/ShumWengSang/BleWinrtDll)
- [Maestro Player Tool kit (MPTK)](https://assetstore.unity.com/packages/tools/audio/maestro-midi-player-tool-kit-free-107994)


