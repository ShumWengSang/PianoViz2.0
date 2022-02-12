## Custom IRecordingServiceVisuals

Spectator View supports specifying a custom prefab for starting and stopping screen recording on mobile devices. To configure your own prefab, follow the steps below:

1. Create a prefab containing an Unity component that implements `IRecordingServiceVisual`.
2. Create a SpectatorViewSettings prefab by calling 'Spectator View' -> 'Edit Settings' in the Unity Editor toolbar.

![Marker](../../../../../../doc/images/SpectatorViewSettingsMenu.png)

3. Specify your created prefab as the `Override Mobile Recording Service Visual` in the `MobileRecordingSettings` in your SpectatorViewSettings prefab.

![Marker](../../../../../../doc/images/SettingsInspector.png)

 When a scene containing the SpectatorView prefab starts, the `SpectatorView` MonoBehaviour will instantiate this custom prefab. It will then search for an `IRecordingServiceVisual` in the created game object, which is provided a reference to the `IRecordingService`. Your UI will be responsible for managing its own show and hide behavior. 

## Custom INetworkConfigurationVisuals

Spectator View supports specifying a custom prefab for choosing an IP Address on mobile devices. To configure your own prefab, follow the steps below:

1. Create a prefab containing a Unity component that implements `INetworkConfigurationVisual`.
2. Create a SpectatorViewSettings prefab by calling 'Spectator View' -> 'Edit Settings' in the Unity Editor toolbar.

![Marker](../../../../../../doc/images/SpectatorViewSettingsMenu.png)

3. Specify your created prefab as the `Override Mobile Network Configuration Visual` in the `NetworkConfigurationSettings` in your SpectatorViewSettings prefab.

![Marker](../../../../../../doc/images/SettingsInspector.png)

When a scene containing the SpectatorView prefab starts, the `SpectatorView` MonoBehaviour will instantiate this custom prefab. Once your prefab fires a `NetworkConfigurationUpdated` event, the SpectatorView MonoBehaviour script will attempt to connect the `StateSynchronizationObserver` to the provided IP Address.