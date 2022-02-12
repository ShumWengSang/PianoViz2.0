﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.SpatialAlignment;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    public class MarkerVisualLocalizationSettings : ISpatialLocalizationSettings
    {
        public const string DiscoveryHeader = "MVISUALDISC";
        public const string CoordinateAssignedHeader = "ASSIGNID";
        public const string CoordinateFoundHeader = "COORDFOUND";

        /// <inheritdoc />
        public MarkerVisualLocalizationSettings() { }

        /// <inheritdoc />
        public void Serialize(BinaryWriter writer) { }
    }

    /// <summary>
    /// SpatialLocalizer that shows a marker
    /// </summary>
    public abstract class MarkerVisualSpatialLocalizer : SpatialLocalizer<MarkerVisualLocalizationSettings>
    {
        [Tooltip("The reference to a prefab containing an IMarkerVisual.")]
        [SerializeField]
        protected GameObject MarkerVisualPrefab = null;
        private GameObject markerVisualGameObject = null;
        protected IMarkerVisual markerVisual = null;

        private Transform cameraTransform = null;

        private readonly Vector3 markerVisualPosition = Vector3.zero;
        private readonly Vector3 markerVisualRotation = new Vector3(0, 180, 0);

        public abstract Guid MarkerVisualDetectorSpatialLocalizerId { get; }

        /// <inheritdoc />
        public override bool TryCreateLocalizationSession(IPeerConnection peerConnection, MarkerVisualLocalizationSettings settings, out ISpatialLocalizationSession session)
        {
            session = null;
            if (markerVisual == null)
            {
                markerVisualGameObject = Instantiate(MarkerVisualPrefab);
                markerVisual = markerVisualGameObject.GetComponentInChildren<IMarkerVisual>();
                if (markerVisual == null)
                {
                    Debug.LogError("Marker Visual Prefab did not contain an IMarkerVisual component.");
                    return false;
                }
            }

            if (cameraTransform == null)
            {
                cameraTransform = Camera.main.transform;
                if (cameraTransform == null)
                {
                    Debug.LogError("Unable to determine camera's location in the scene.");
                    return false;
                }
            }

            session = new LocalizationSession(this, settings, peerConnection, debugLogging);
            return true;
        }

        /// <inheritdoc />
        public override bool TryDeserializeSettings(BinaryReader reader, out MarkerVisualLocalizationSettings settings)
        {
            settings = new MarkerVisualLocalizationSettings();
            return true;
        }

        private class LocalizationSession : SpatialLocalizationSession
        {
            /// <inheritdoc />
            public override IPeerConnection Peer => peerConnection;

            private readonly MarkerVisualSpatialLocalizer localizer;
            private readonly MarkerVisualLocalizationSettings settings;
            private readonly IPeerConnection peerConnection;
            private readonly ISpatialCoordinateService coordinateService;
            private readonly bool debugLogging = false;
            private readonly TaskCompletionSource<string> coordinateAssigned = null;
            private readonly TaskCompletionSource<string> coordinateFound = null;
            private readonly CancellationTokenSource discoveryCTS = null;

            private string coordinateId = string.Empty;

            public LocalizationSession(MarkerVisualSpatialLocalizer localizer, MarkerVisualLocalizationSettings settings, IPeerConnection peerConnection, bool debugLogging = false) : base()
            {
                DebugLog("Session created");
                this.localizer = localizer;
                this.settings = settings;
                this.peerConnection = peerConnection;
                this.debugLogging = debugLogging;

                coordinateAssigned = new TaskCompletionSource<string>();
                coordinateFound = new TaskCompletionSource<string>();
                discoveryCTS = new CancellationTokenSource();

                var cameraToMarker = Matrix4x4.TRS(this.localizer.markerVisualPosition, Quaternion.Euler(this.localizer.markerVisualRotation), Vector3.one);
                this.coordinateService = new MarkerVisualCoordinateService(this.localizer.markerVisual, cameraToMarker, this.localizer.cameraTransform, this.localizer.debugLogging);
            }

            /// <inheritdoc />
            protected override void OnManagedDispose()
            {
                base.OnManagedDispose();
                discoveryCTS.Dispose();
                coordinateService.Dispose();
            }

            /// <inheritdoc />
            public override async Task<ISpatialCoordinate> LocalizeAsync(CancellationToken cancellationToken)
            {
                DebugLog($"Localizing, CanBeCanceled:{cancellationToken.CanBeCanceled}, IsCancellationRequested:{cancellationToken.IsCancellationRequested}");

                ISpatialCoordinate coordinate = null;
                using (var cancellableCTS = CancellationTokenSource.CreateLinkedTokenSource(defaultCancellationToken, cancellationToken))
                {
                    if (!TrySendMarkerVisualDiscoveryMessage())
                    {
                        Debug.LogWarning("Failed to send marker visual discovery message, spatial localization failed.");
                        return null;
                    }

                    // Receive marker to show
                    DebugLog("Waiting to have a coordinate id assigned");
                    await Task.WhenAny(coordinateAssigned.Task, Task.Delay(-1, cancellableCTS.Token));
                    if (string.IsNullOrEmpty(coordinateId))
                    {
                        DebugLog("Failed to assign coordinate id");
                        return null;
                    }

                    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(discoveryCTS.Token, cancellableCTS.Token))
                    {
                        DebugLog($"Attempting to discover coordinate: {coordinateId}, CanBeCanceled:{cts.Token.CanBeCanceled}, IsCancellationRequested:{cts.Token.IsCancellationRequested}");
                        if (await coordinateService.TryDiscoverCoordinatesAsync(cts.Token, new string[] { coordinateId.ToString() }))
                        {
                            DebugLog($"Coordinate discovery completed: {coordinateId}");
                            if (!coordinateService.TryGetKnownCoordinate(coordinateId, out coordinate))
                            {
                                DebugLog("Failed to find spatial coordinate although discovery completed.");
                            }
                        }
                        else
                        {
                            DebugLog("TryDiscoverCoordinatesAsync failed.");
                        }
                    }

                    DebugLog($"Waiting for coordinate to be found: {coordinateId}");
                    await Task.WhenAny(coordinateFound.Task, Task.Delay(-1, cancellableCTS.Token));
                }

                return coordinate;
            }

            /// <inheritdoc />
            public override void OnDataReceived(BinaryReader reader)
            {
                string command = reader.ReadString();
                DebugLog($"Received command: {command}");
                switch (command)
                {
                    case MarkerVisualLocalizationSettings.CoordinateAssignedHeader:
                        coordinateId = reader.ReadString();
                        DebugLog($"Assigned coordinate id: {coordinateId}");
                        coordinateAssigned?.SetResult(coordinateId);
                        break;
                    case MarkerVisualLocalizationSettings.CoordinateFoundHeader:
                        string detectedId = reader.ReadString();
                        if (coordinateId == detectedId)
                        {
                            DebugLog($"Ending discovery: {coordinateId}");
                            discoveryCTS?.Cancel();

                            DebugLog($"Coordinate was found: {coordinateId}");
                            coordinateFound?.SetResult(detectedId);
                        }
                        else
                        {
                            DebugLog($"Unexpected coordinate found, expected: {coordinateId}, detected: {detectedId}");
                        }
                        break;
                    default:
                        DebugLog($"Sent unknown command: {command}");
                        break;
                }
            }

            private void DebugLog(string message)
            {
                if (debugLogging)
                {
                    Debug.Log($"MarkerVisualSpatialLocalizer.LocalizationSession: {message}");
                }
            }

            private bool TrySendMarkerVisualDiscoveryMessage()
            {
                if (localizer.markerVisual.TryGetMaxSupportedMarkerId(out var maxId))
                {
                    DebugLog($"Sending maximum id for discovery: {maxId}");
                    peerConnection?.SendData(writer =>
                    {
                        writer.Write(MarkerVisualLocalizationSettings.DiscoveryHeader);
                        writer.Write(maxId);
                    });

                    return true;
                }

                DebugLog("Unable to obtain max id from marker visual");
                return false;
            }
        }
    }
}
