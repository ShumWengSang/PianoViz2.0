﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Microsoft.MixedReality.SpatialAlignment;

[assembly: InternalsVisibleToAttribute("Microsoft.MixedReality.SpectatorView.Editor")]

namespace Microsoft.MixedReality.SpectatorView
{
    public enum Role
    {
        User,
        Spectator
    }

    /// <summary>
    /// Class that facilitates the Spectator View experience
    /// </summary>
    public class SpectatorView : MonoBehaviour
    {
        public const string SettingsPrefabName = "SpectatorViewSettings";

        /// <summary>
        /// Role of the device in the spectator view experience.
        /// </summary>
        [Tooltip("Role of the device in the spectator view experience.")]
        [SerializeField]
        public Role Role;

        [Header("Networking")]
        /// <summary>
        /// User ip address, this value is used for the user if 'Use Mobile Network Configuration Visual' is set to false.
        /// </summary>
        [Tooltip("User ip address, this value is used for the user if 'Use Mobile Network Configuration Visual' is set to false.")]
        [SerializeField]
        private string userIpAddress = "127.0.0.1";

        /// <summary>
        /// Prefab for creating a mobile network configuration visual.
        /// </summary>
        [Tooltip("Default prefab for creating a mobile network configuration visual.")]
        [SerializeField]
#pragma warning disable 414 // The field is assigned but its value is never used
        private GameObject defaultMobileNetworkConfigurationVisualPrefab = null;
#pragma warning restore 414

        [Header("State Synchronization")]
        /// <summary>
        /// StateSynchronizationSceneManager MonoBehaviour
        /// </summary>
        [Tooltip("StateSynchronizationSceneManager")]
        [SerializeField]
        private StateSynchronizationSceneManager stateSynchronizationSceneManager = null;

        /// <summary>
        /// StateSynchronizationBroadcaster MonoBehaviour
        /// </summary>
        [Tooltip("StateSynchronizationBroadcaster MonoBehaviour")]
        [SerializeField]
        private StateSynchronizationBroadcaster stateSynchronizationBroadcaster = null;

        /// <summary>
        /// StateSynchronizationObserver MonoBehaviour
        /// </summary>
        [Tooltip("StateSynchronizationObserver MonoBehaviour")]
        [SerializeField]
        private StateSynchronizationObserver stateSynchronizationObserver = null;

        [Header("Spatial Alignment")]
        [Tooltip("A prioritized list of SpatialLocalizationInitializers that should be used when a spectator connects.")]
        [SerializeField]
        private SpatialLocalizationInitializer[] defaultSpatialLocalizationInitializers = null;

        [Header("Device Tracking")]
        [Tooltip("Prefab used to create a HoloLens device tracking observer.")]
        [SerializeField]
#pragma warning disable 414 // The field is assigned but its value is never used
        private GameObject holoLensDeviceTrackingObserverPrefab = null;
#pragma warning restore 414

        [Tooltip("Prefab used to create a Android device tracking observer.")]
        [SerializeField]
#pragma warning disable 414 // The field is assigned but its value is never used
        private GameObject androidDeviceTrackingObserverPrefab = null;
#pragma warning restore 414

        [Tooltip("Prefab used to create an iOS device tracking observer.")]
        [SerializeField]
#pragma warning disable 414 // The field is assigned but its value is never used
        private GameObject iOSDeviceTrackingObserverPrefab = null;
#pragma warning restore 414

        [Header("Recording")]
        /// <summary>
        /// Prefab for creating a mobile recording service visual.
        /// </summary>
        [Tooltip("Default prefab for creating a mobile recording service visual.")]
        [SerializeField]
#pragma warning disable 414 // The field is assigned but its value is never used
        private GameObject defaultMobileRecordingServiceVisualPrefab = null;
#pragma warning restore 414

        [Header("Miscellaneous")]
        [Tooltip("Check to call DontDestroyOnLoad for this game object.")]
        [SerializeField]
        private bool persistAcrossScenes = true;

        [Header("Debugging")]
        /// <summary>
        /// Debug visual prefab created by the user.
        /// </summary>
        [Tooltip("Debug visual prefab created by the user.")]
        [SerializeField]
        public GameObject userDebugVisualPrefab = null;

        /// <summary>
        /// Scaling applied to user debug visuals.
        /// </summary>
        [Tooltip("Scaling applied to spectator debug visuals.")]
        [SerializeField]
        public float userDebugVisualScale = 1.0f;

        /// <summary>
        /// Debug visual prefab created by the spectator.
        /// </summary>
        [Tooltip("Debug visual prefab created by the spectator.")]
        [SerializeField]
        public GameObject spectatorDebugVisualPrefab = null;

        /// <summary>
        /// Scaling applied to spectator debug visuals.
        /// </summary>
        [Tooltip("Scaling applied to spectator debug visuals.")]
        [SerializeField]
        public float spectatorDebugVisualScale = 1.0f;

        [Tooltip("Enable verbose debug logging messages")]
        [SerializeField]
        private bool debugLogging = false;

        [Tooltip("Check to hide the on-device developer console every update.")]
        [SerializeField]
        private bool hideDeveloperConsole = false;

        private GameObject settingsGameObject;
        private Dictionary<SpatialCoordinateSystemParticipant, ISet<Guid>> peerSupportedLocalizers = new Dictionary<SpatialCoordinateSystemParticipant, ISet<Guid>>();
        private SpatialCoordinateSystemParticipant currentParticipant = null;

#if UNITY_ANDROID || UNITY_IOS
        private GameObject mobileRecordingServiceVisual = null;
        private IRecordingService recordingService = null;
        private IRecordingServiceVisual recordingServiceVisual = null;
        private GameObject mobileNetworkConfigurationVisual = null;
        private INetworkConfigurationVisual networkConfigurationVisual = null;
#endif

        private void Awake()
        {
            if (persistAcrossScenes)
            {
                DebugLog("Setting up spectator view content to persist across scenes.");
                DontDestroyOnLoad(this);
            }

            DebugLog($"SpectatorView is running as: {Role.ToString()}. Expected User IPAddress: {userIpAddress}");

            GameObject settings = Resources.Load<GameObject>(SettingsPrefabName);
            if (settings != null)
            {
                settingsGameObject = Instantiate(settings, this.transform);
            }

            CreateDeviceTrackingObserver();

            if (stateSynchronizationSceneManager == null ||
                stateSynchronizationBroadcaster == null ||
                stateSynchronizationObserver == null)
            {
                Debug.LogError("StateSynchronization scene isn't configured correctly");
                return;
            }

            switch (Role)
            {
                case Role.User:
                    {
                        if (userDebugVisualPrefab != null)
                        {
                            SpatialCoordinateSystemManager.Instance.debugVisual = userDebugVisualPrefab;
                            SpatialCoordinateSystemManager.Instance.debugVisualScale = userDebugVisualScale;
                        }

                        RunStateSynchronizationAsBroadcaster();
                    }
                    break;
                case Role.Spectator:
                    {
                        if (spectatorDebugVisualPrefab != null)
                        {
                            SpatialCoordinateSystemManager.Instance.debugVisual = spectatorDebugVisualPrefab;
                            SpatialCoordinateSystemManager.Instance.debugVisualScale = spectatorDebugVisualScale;
                        }

                        // When running as a spectator, automatic localization should be initiated if it's configured.
                        SpatialCoordinateSystemManager.Instance.ParticipantConnected += OnParticipantConnected;

                        SetupSpectatorNetworkConnection();
                    }
                    break;
            }

            SetupRecordingService();
        }

        private void SetupSpectatorNetworkConnection()
        {
            if (!ShouldUseNetworkConfigurationVisual())
            {
                DebugLog("Not using a network configuration visual, beginning state synchronization as an observer.");
                RunStateSynchronizationAsObserver();
            }
            else
            {
                DebugLog("Using a network configuration visual. State synchronization will be delayed until a connection is started by the user.");
                SetupNetworkConfigurationVisual();
            }
        }

        private void OnSpectatorNetworkDisconnect(INetworkConnection connectionn)
        {
            if (stateSynchronizationObserver != null)
            {
                stateSynchronizationObserver.Disconnected -= OnSpectatorNetworkDisconnect;
            }

            DebugLog("Observed network disconnect for spectator, rerunning spectator view network connection setup logic.");
            SetupSpectatorNetworkConnection();
        }

        private void Update()
        {
            if (hideDeveloperConsole &&
                Debug.developerConsoleVisible)
            {
                Debug.developerConsoleVisible = false;
            }
        }

        private void OnDestroy()
        {
            Destroy(settingsGameObject);

#if UNITY_ANDROID || UNITY_IOS
            Destroy(mobileRecordingServiceVisual);

            if (mobileNetworkConfigurationVisual != null)
            {
                Destroy(mobileNetworkConfigurationVisual);
                mobileNetworkConfigurationVisual = null;
                networkConfigurationVisual = null;
            }
#endif

            if (this.Role == Role.Spectator)
            {
                SpatialCoordinateSystemManager.Instance.ParticipantConnected -= OnParticipantConnected;
            }
        }

        private void RunStateSynchronizationAsBroadcaster()
        {
            stateSynchronizationBroadcaster.gameObject.SetActive(true);
            stateSynchronizationObserver.gameObject.SetActive(false);

            // The StateSynchronizationSceneManager needs to be enabled after the broadcaster/observer
            stateSynchronizationSceneManager.gameObject.SetActive(true);
        }

        private void RunStateSynchronizationAsObserver()
        {
            stateSynchronizationBroadcaster.gameObject.SetActive(false);
            stateSynchronizationObserver.gameObject.SetActive(true);

            // The StateSynchronizationSceneManager needs to be enabled after the broadcaster/observer
            stateSynchronizationSceneManager.gameObject.SetActive(true);

            stateSynchronizationObserver.Disconnected += OnSpectatorNetworkDisconnect;

            // Make sure the StateSynchronizationSceneManager is enabled prior to connecting the observer
            stateSynchronizationObserver.ConnectTo(userIpAddress);
        }

        private void SetupRecordingService()
        {
#if UNITY_ANDROID || UNITY_IOS
            GameObject recordingVisualPrefab = defaultMobileRecordingServiceVisualPrefab;
            if (MobileRecordingSettings.IsInitialized && MobileRecordingSettings.Instance.OverrideMobileRecordingServicePrefab != null)
            {
                recordingVisualPrefab = MobileRecordingSettings.Instance.OverrideMobileRecordingServicePrefab;
            }

            if (MobileRecordingSettings.IsInitialized && 
                MobileRecordingSettings.Instance.EnableMobileRecordingService &&
                recordingVisualPrefab != null)
            {
                mobileRecordingServiceVisual = Instantiate(recordingVisualPrefab);

                if (!TryCreateRecordingService(out recordingService))
                {
                    Debug.LogError("Failed to create a recording service for the current platform.");
                    return;
                }

                recordingServiceVisual = mobileRecordingServiceVisual.GetComponentInChildren<IRecordingServiceVisual>(true);
                if (recordingServiceVisual == null)
                {
                    Debug.LogError("Failed to find an IRecordingServiceVisual in the created mobileRecordingServiceVisualPrefab. Note: It's assumed that the IRecordingServiceVisual is enabled by default in the mobileRecordingServiceVisualPrefab.");
                    return;
                }

                recordingServiceVisual.SetRecordingService(recordingService);
            }
#endif
        }

        private bool TryCreateRecordingService(out IRecordingService recordingService)
        {
#if UNITY_ANDROID
            recordingService = new AndroidRecordingService();
            return true;
#elif UNITY_IOS
            recordingService = new iOSRecordingService();
            return true;
#else
            recordingService = null;
            return false;
#endif
        }

        private void SetupNetworkConfigurationVisual()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (mobileNetworkConfigurationVisual == null)
            {
                if (networkConfigurationVisual != null)
                {
                    networkConfigurationVisual.NetworkConfigurationUpdated -= OnNetworkConfigurationUpdated;
                    networkConfigurationVisual = null;
                }

                GameObject mobileNetworkConfigurationVisualPrefab = defaultMobileNetworkConfigurationVisualPrefab;
                if (NetworkConfigurationSettings.IsInitialized && NetworkConfigurationSettings.Instance.OverrideMobileNetworkConfigurationVisualPrefab != null)
                {
                    mobileNetworkConfigurationVisualPrefab = NetworkConfigurationSettings.Instance.OverrideMobileNetworkConfigurationVisualPrefab;
                }

                mobileNetworkConfigurationVisual = Instantiate(mobileNetworkConfigurationVisualPrefab);
                networkConfigurationVisual = mobileNetworkConfigurationVisual.GetComponentInChildren<INetworkConfigurationVisual>(true);
                if (networkConfigurationVisual == null)
                {
                    Debug.LogError("Network configuration visual was not found. No connection will be established. Visual will be destroyed.");
                    Destroy(mobileNetworkConfigurationVisual);
                    mobileNetworkConfigurationVisual = null;
                }
                else
                {
                    networkConfigurationVisual.NetworkConfigurationUpdated += OnNetworkConfigurationUpdated;
                }
            }

            if (networkConfigurationVisual != null)
            {
                networkConfigurationVisual.Show();
            }
#endif
        }


        private void OnNetworkConfigurationUpdated(object sender, string ipAddress)
        {
#if UNITY_ANDROID || UNITY_IOS
            DebugLog($"OnNetworkConfigurationUpdated: ipAddress:{ipAddress}");

            this.userIpAddress = ipAddress;
            if (networkConfigurationVisual != null)
            {
                networkConfigurationVisual.Hide();
            }

            RunStateSynchronizationAsObserver();
#endif
        }


        private bool ShouldUseNetworkConfigurationVisual()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (NetworkConfigurationSettings.IsInitialized)
            {
                return NetworkConfigurationSettings.Instance.EnableMobileNetworkConfigurationVisual;
            }
            else
            {
                return (defaultMobileNetworkConfigurationVisualPrefab != null);
            }
#else
            return false;
#endif
        }

        private void DebugLog(string message)
        {
            if (debugLogging)
            {
                UnityEngine.Debug.Log($"SpectatorView: {message}");
            }
        }

        private void OnParticipantConnected(SpatialCoordinateSystemParticipant participant)
        {
            currentParticipant = participant;
            TryRunLocalizationForParticipantAsync(currentParticipant).FireAndForget();
        }

        /// <summary>
        /// Call to reset spatial alignment for the current participant
        /// </summary>
        /// <returns>Returns true if resetting localization succeeds, otherwise False.</returns>
        public async Task<bool> TryResetLocalizationAsync()
        {
            if (Role == Role.User)
            {
                Debug.LogError("Relocalization was called for a User but is only supported for Spectators. Resetting localization failed.");
                return await Task.FromResult(false);
            }

            if (currentParticipant != null)
            {
                return await TryRunLocalizationForParticipantAsync(currentParticipant);
            }

            DebugLog("No participants have connected. Resetting localization failed.");
            return await Task.FromResult(false);
        }

        private async Task<bool> TryRunLocalizationForParticipantAsync(SpatialCoordinateSystemParticipant participant)
        {
            DebugLog($"Waiting for the set of supported localizers from connected participant {participant.NetworkConnection.ToString()}");

            if (!peerSupportedLocalizers.ContainsKey(participant))
            {
                // When a remote participant connects, get the set of ISpatialLocalizers that peer
                // supports. This is asynchronous, as it comes across the network.
                peerSupportedLocalizers[participant] = await participant.GetPeerSupportedLocalizersAsync();
            }

            // If there are any supported localizers, find the first configured localizer in the
            // list that supports that type. If and when one is found, use it to perform localization.
            if (peerSupportedLocalizers.TryGetValue(participant, out var supportedLocalizers) &&
                supportedLocalizers != null)
            {
                DebugLog($"Received a set of {peerSupportedLocalizers.Count} supported localizers");

                var initializers = new List<SpatialLocalizationInitializer>();
                if (SpatialLocalizationInitializationSettings.IsInitialized &&
                    SpatialLocalizationInitializationSettings.Instance.PrioritizedInitializers != null)
                {
                    DebugLog($"Found prioritized spatial localization initializers list in spectator view settings");
                    foreach(var initializer in SpatialLocalizationInitializationSettings.Instance.PrioritizedInitializers)
                    {
                        initializers.Add(initializer);
                    }
                }

                if (defaultSpatialLocalizationInitializers != null)
                {
                    DebugLog($"Found default spatial localization initializers list for spectator view settings");
                    foreach (var initializer in defaultSpatialLocalizationInitializers)
                    {
                        initializers.Add(initializer);
                    }
                }

                return await TryRunLocalizationWithInitializerAsync(initializers, supportedLocalizers, participant);
            }
            else
            {
                Debug.LogWarning($"No supported localizers were received from the participant, localization will not be started");
            }

            return false;
        }

        private async Task<bool> TryRunLocalizationWithInitializerAsync(IList<SpatialLocalizationInitializer> initializers, ISet<Guid> supportedLocalizers, SpatialCoordinateSystemParticipant participant)
        {
            if (initializers == null || supportedLocalizers == null)
            {
                Debug.LogWarning("Did not find a supported localizer/initializer combination.");
                return false;
            }
            DebugLog($"TryRunLocalizationWithInitializerAsync, initializers:{initializers.Count}, supportedLocalizers:{supportedLocalizers.Count}");

            for (int i = 0; i < initializers.Count; i++)
            {
                if (supportedLocalizers.Contains(initializers[i].PeerSpatialLocalizerId))
                {
                    DebugLog($"Localization initializer {initializers[i].GetType().Name} supported localization with ID {initializers[i].PeerSpatialLocalizerId}, starting localization");
                    bool result = await initializers[i].TryRunLocalizationAsync(participant);
                    if (!result)
                    {
                        Debug.LogError($"Failed to localize experience with participant: {participant.NetworkConnection.ToString()}");
                    }
                    else
                    {
                        DebugLog($"Succeeded in localizing experience with participant: {participant.NetworkConnection.ToString()}");
                    }

                    return result;
                }
                else
                {
                    DebugLog($"Localization initializer {initializers[i].GetType().Name} not supported by peer.");
                }
            }

            return await Task.FromResult(false);
        }

        private void CreateDeviceTrackingObserver()
        {
#if UNITY_WSA
            Instantiate(holoLensDeviceTrackingObserverPrefab, transform);
#elif UNITY_ANDROID
            Instantiate(androidDeviceTrackingObserverPrefab, transform);
#elif UNITY_IOS
            Instantiate(iOSDeviceTrackingObserverPrefab, transform);
#endif
        }
    }
}
