﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// This class observes changes and updates content on a user device.
    /// </summary>
    public class StateSynchronizationBroadcaster : NetworkManager<StateSynchronizationBroadcaster>
    {
        /// <summary>
        /// Check to enable debug logging.
        /// </summary>
        [Tooltip("Check to enable debug logging.")]
        [SerializeField]
        protected bool debugLogging;

        /// <summary>
        /// Port used for sending data.
        /// </summary>
        [Tooltip("Port used for sending data.")]
        public int Port = 7410;

        private const float PerfUpdateTimeSeconds = 1.0f;
        private float timeUntilNextPerfUpdate = PerfUpdateTimeSeconds;
        private int numFrames = 0;

        private GameObject dontDestroyOnLoadGameObject;
        
        protected override int RemotePort => Port;

        protected override void Awake()
        {
            DebugLog($"Awoken!");
            base.Awake();

            RegisterCommandHandler(StateSynchronizationObserver.SyncCommand, HandleSyncCommand);
            RegisterCommandHandler(StateSynchronizationObserver.PerfDiagnosticModeEnabledCommand, HandlePerfMonitoringModeEnableRequest);

            // Ensure that runInBackground is set to true so that the app continues to send network
            // messages even if it loses focus
            Application.runInBackground = true;
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            UnregisterCommandHandler(StateSynchronizationObserver.SyncCommand, HandleSyncCommand);
            UnregisterCommandHandler(StateSynchronizationObserver.PerfDiagnosticModeEnabledCommand, HandlePerfMonitoringModeEnableRequest);
        }

        protected override void Start()
        {
            base.Start();
            StartListening(Port);
        }

        private void DebugLog(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"StateSynchronizationBroadcaster: {message}");
            }
        }

        protected override void OnConnected(INetworkConnection connection)
        {
            DebugLog($"Broadcaster received connection from {connection.ToString()}.");
            base.OnConnected(connection);
        }

        protected override void OnDisconnected(INetworkConnection connection)
        {
            DebugLog($"Broadcaster received disconnect from {connection.ToString()}"); ;
            base.OnDisconnected(connection);
        }

        /// <summary>
        /// True if network connections exist, otherwise false
        /// </summary>
        public bool HasConnections
        {
            get
            {
                return connectionManager != null && connectionManager.Connections.Count > 0;
            }
        }

        /// <summary>
        /// Returns how many bytes have been queued to send to other devices
        /// </summary>
        public int OutputBytesQueued
        {
            get
            {
                return connectionManager.OutputBytesQueued;
            }
        }

        private void Update()
        {
            if (connectionManager == null)
            {
                return;
            }

            UpdateExtension();

            if (HasConnections && BroadcasterSettings.IsInitialized && BroadcasterSettings.Instance && BroadcasterSettings.Instance.AutomaticallyBroadcastAllGameObjects)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        ComponentExtensions.EnsureComponent<TransformBroadcaster>(root);
                    }
                }

                // GameObjects that are marked DontDestroyOnLoad exist in a special scene, and that scene
                // cannot be enumerated via the SceneManager. The only way to access that scene is from a
                // GameObject inside that scene, so we need to create a GameObject we have access to inside
                // that scene in order to enumerate all of its root GameObjects.
                if (dontDestroyOnLoadGameObject == null)
                {
                    dontDestroyOnLoadGameObject = new GameObject("StateSynchronizationBroadcaster_DontDestroyOnLoad");
                    DontDestroyOnLoad(dontDestroyOnLoadGameObject);
                }

                foreach (GameObject root in dontDestroyOnLoadGameObject.scene.GetRootGameObjects())
                {
                    ComponentExtensions.EnsureComponent<TransformBroadcaster>(root);
                }
            }
        }

        /// <summary>
        /// Extension method called on update
        /// </summary>
        protected virtual void UpdateExtension() { }

        /// <summary>
        /// Called after a frame is completed to send state data to socket end points.
        /// </summary>
        public void OnFrameCompleted()
        {
            //Camera update
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter message = new BinaryWriter(memoryStream))
            {
                Transform camTrans = null;
                if (Camera.main != null &&
                    Camera.main.transform != null)
                {
                    camTrans = Camera.main.transform;
                }

                message.Write(StateSynchronizationObserver.CameraCommand);
                message.Write(Time.time);
                message.Write(camTrans != null ? camTrans.position : Vector3.zero);
                message.Write(camTrans != null ? camTrans.rotation : Quaternion.identity);
                message.Flush();

                memoryStream.TryGetBuffer(out var buffer);
                connectionManager.Broadcast(buffer.Array, buffer.Offset, buffer.Count);
            }

            //Perf
            timeUntilNextPerfUpdate -= Time.deltaTime;
            numFrames++;
            if (timeUntilNextPerfUpdate < 0)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                using (BinaryWriter message = new BinaryWriter(memoryStream))
                {
                    message.Write(StateSynchronizationObserver.PerfCommand);
                    StateSynchronizationPerformanceMonitor.Instance.WriteMessage(message, numFrames);
                    message.Flush();
                    memoryStream.TryGetBuffer(out var buffer);
                    connectionManager.Broadcast(buffer.Array, buffer.Offset, buffer.Count);
                }

                timeUntilNextPerfUpdate = PerfUpdateTimeSeconds;
                numFrames = 0;
            }
        }

        public void HandleSyncCommand(INetworkConnection connection, string command, BinaryReader reader, int remainingDataSize)
        {
            reader.ReadSingle(); // float time
            StateSynchronizationSceneManager.Instance.ReceiveMessage(connection, reader);
        }

        private void HandlePerfMonitoringModeEnableRequest(INetworkConnection connection, string command, BinaryReader reader, int remainingDataSize)
        {
            bool enabled = reader.ReadBoolean();
            if (StateSynchronizationPerformanceMonitor.Instance != null)
            {
                StateSynchronizationPerformanceMonitor.Instance.SetDiagnosticMode(enabled);
            }
        }

    }
}
