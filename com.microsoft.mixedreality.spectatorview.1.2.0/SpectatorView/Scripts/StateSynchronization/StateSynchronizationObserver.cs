﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// This class observes changes and updates content on a spectator device.
    /// </summary>
    public class StateSynchronizationObserver : NetworkManager<StateSynchronizationObserver>
    {
        public const string SyncCommand = "SYNC";
        public const string CameraCommand = "Camera";
        public const string PerfCommand = "Perf";
        public const string PerfDiagnosticModeEnabledCommand = "PERFDIAG";

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
        [SerializeField]
        protected int port = 7410;

        private const float heartbeatTimeInterval = 0.1f;
        private float timeSinceLastHeartbeat = 0.0f;
        private HologramSynchronizer hologramSynchronizer = new HologramSynchronizer();
        private StateSynchronizationPerformanceMonitor.ParsedMessage lastPerfMessage = StateSynchronizationPerformanceMonitor.ParsedMessage.Empty;

        private static readonly byte[] heartbeatMessage = GenerateHeartbeatMessage();

        protected override int RemotePort => port;

        protected override void Awake()
        {
            DebugLog($"Awoken!");
            base.Awake();

            // Ensure that runInBackground is set to true so that the app continues to send network
            // messages even if it loses focus
            Application.runInBackground = true;

            StartListening(port);
            RegisterCommandHandler(SyncCommand, HandleSyncCommand);
            RegisterCommandHandler(CameraCommand, HandleCameraCommand);
            RegisterCommandHandler(PerfCommand, HandlePerfCommand);
        }

        protected void Update()
        {
            CheckAndSendHeartbeat();
            hologramSynchronizer.UpdateHolograms();
        }

        private void DebugLog(string message)
        {
            if (debugLogging)
            {
                string connectedState = IsConnected ? $"Connected - {ConnectedIPAddress}" : "Not Connected";
                Debug.Log($"StateSynchronizationObserver - {connectedState}: {message}");
            }
        }

        protected override void OnConnected(INetworkConnection connection)
        {
            base.OnConnected(connection);

            DebugLog($"Observer Connected to connection: {connection.ToString()}");

            if (StateSynchronizationSceneManager.IsInitialized)
            {
                StateSynchronizationSceneManager.Instance.MarkSceneDirty();
            }

            hologramSynchronizer.Reset(connection);
        }

        public void HandleCameraCommand(INetworkConnection connection, string command, BinaryReader reader, int remainingDataSize)
        {
            float timeStamp = reader.ReadSingle();
            hologramSynchronizer.RegisterCameraUpdate(timeStamp);
            transform.position = reader.ReadVector3();
            transform.rotation = reader.ReadQuaternion();
        }

        public void HandleSyncCommand(INetworkConnection connection, string command, BinaryReader reader, int remainingDataSize)
        {
            float timeStamp = reader.ReadSingle();
            hologramSynchronizer.RegisterFrameData(reader.ReadBytes(remainingDataSize), timeStamp);
        }

        public void HandlePerfCommand(INetworkConnection connection, string command, BinaryReader reader, int remainingDataSize)
        {
            StateSynchronizationPerformanceMonitor.ReadMessage(reader, out lastPerfMessage);
        }

        public void SetPerformanceMonitoringMode(bool enabled)
        {
            if (IsConnected)
            {
                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(PerfDiagnosticModeEnabledCommand);
                    writer.Write(enabled);
                    writer.Flush();
                    stream.TryGetBuffer(out var buffer);
                    connectionManager.Broadcast(buffer.Array, buffer.Offset, buffer.Count);
                }
            }
        }

        internal bool PerformanceMonitoringModeEnabled => lastPerfMessage.PerformanceMonitoringEnabled;
        internal IReadOnlyList<Tuple<string, double>> PerformanceEventDurations => lastPerfMessage.EventDurations;
        internal IReadOnlyList<Tuple<string, double>> PerformanceSummedEventDurations => lastPerfMessage.SummedEventDurations;
        internal IReadOnlyList<Tuple<string, int>> PerformanceEventCounts => lastPerfMessage.EventCounts;
        internal IReadOnlyList<Tuple<string, StateSynchronizationPerformanceMonitor.MemoryUsage>> PerformanceMemoryUsageEvents => lastPerfMessage.MemoryUsages;

        private void CheckAndSendHeartbeat()
        {
            if (IsConnected)
            {
                timeSinceLastHeartbeat += Time.deltaTime;
                if (timeSinceLastHeartbeat > heartbeatTimeInterval)
                {
                    timeSinceLastHeartbeat = 0.0f;
                    connectionManager.Broadcast(heartbeatMessage, 0, heartbeatMessage.Length);
                }
            }
        }

        private static byte[] GenerateHeartbeatMessage()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // It doesn't matter what the content of this message is, it just can't conflict with other commands
                // sent in this channel and read by the Broadcaster.
                writer.Write("♥");
                writer.Flush();

                return stream.ToArray();
            }
        }
    }
}
