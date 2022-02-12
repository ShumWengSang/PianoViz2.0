﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.SpatialAlignment;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// A variant of marker based <see cref="Microsoft.MixedReality.SpatialAlignment.ISpatialCoordinateService"/> implementation. This one tracks coordinates displayed on the screen of current mobile device.
    /// The logic is that every time you start tracking a new coordinate is created and shown on the screen, after you stop tracking that coordinates location is no longer updated with the device.
    /// </summary>
    public class MarkerVisualCoordinateService : SpatialCoordinateServiceBase<int>
    {
        private class SpatialCoordinate : SpatialCoordinateUnityBase<int>
        {
            private readonly IMarkerVisual markerVisual;

            private LocatedState locatedState = LocatedState.Resolved;

            public Matrix4x4 WorldToCoordinate
            {
                get
                {
                    return worldMatrix;
                }

                set
                {
                    worldMatrix = value;
                }
            }

            /// <inheritdoc/>
            public override LocatedState State => locatedState;

            public SpatialCoordinate(int id, IMarkerVisual markerVisual)
                : base(id) { this.markerVisual = markerVisual; }

            public void ShowMarker()
            {
                markerVisual.ShowMarker(Id);
                locatedState = LocatedState.Tracking;
            }

            public void HideMarker()
            {
                locatedState = LocatedState.Resolved;
                markerVisual.HideMarker();
            }
        }

        private readonly IMarkerVisual markerVisual;
        private readonly UnityEngine.Matrix4x4 cameraToMarker;
        private readonly UnityEngine.Transform cameraTransform;

        private bool debugLogging = false;

        public MarkerVisualCoordinateService(IMarkerVisual markerVisual, UnityEngine.Matrix4x4 cameraToMarker, UnityEngine.Transform cameraTransform, bool debugLogging = false)
        {
            this.markerVisual = markerVisual ?? throw new ArgumentNullException("MarkerVisual was null.");
            this.cameraToMarker = cameraToMarker;
            this.cameraTransform = cameraTransform;
            this.debugLogging = debugLogging;
            DebugLog("Service Created");
        }

        protected override bool TryParse(string id, out int result)
        {
            DebugLog($"Parsing coordinate id: {id}");
            result = -1;
            return int.TryParse(id, out result);
        }

        protected override void OnManagedDispose()
        {
            base.OnManagedDispose();
            DebugLog("Service Disposed");
        }

        protected override async Task OnDiscoverCoordinatesAsync(CancellationToken cancellationToken, int[] idsToLocate)
        {
            DebugLog($"OnDiscoverCoordinateAsync, CanBeCanceled:{cancellationToken.CanBeCanceled}, IsCancellationRequested:{cancellationToken.IsCancellationRequested}");
            if (idsToLocate == null || idsToLocate.Length < 1)
            {
                UnityEngine.Debug.LogError($"{nameof(MarkerVisualCoordinateService)} depends on ids so that it could visualize them, at least one should be provided.");
                return;
            }

            DebugLog($"Creating spatial coordinate {idsToLocate[0]}");
            SpatialCoordinate markerCoordinate = new SpatialCoordinate(idsToLocate[0], markerVisual);
            OnNewCoordinate(markerCoordinate.Id, markerCoordinate);

            DebugLog($"Showing marker");
            markerCoordinate.ShowMarker();

            DebugLog($"Waiting for cancellation token: CanBeCanceled:{cancellationToken.CanBeCanceled}, IsCancellationRequested:{cancellationToken.IsCancellationRequested}");
            await Task.WhenAny(Task.Delay(-1, cancellationToken));

            // Use the local position and local rotation to avoid using camera parent transforms.
            markerCoordinate.WorldToCoordinate = Matrix4x4.TRS(cameraTransform.localPosition, cameraTransform.localRotation, Vector3.one) * cameraToMarker;

            DebugLog($"Hiding marker");
            markerCoordinate.HideMarker();
        }

        private void DebugLog(string message)
        {
            if (debugLogging)
            {
                UnityEngine.Debug.Log($"MarkerVisualCoordinateService: {message}");
            }
        }
    }
}
