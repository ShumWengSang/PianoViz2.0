// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// #define TRYTHIS
using Microsoft.MixedReality.PhotoCapture;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Wrapper class for SpectatorView.OpenCV.dll built from the SpectatorView.Native.sln
    /// </summary>
    public class SpectatorViewOpenCVInterface
    {
        private const string SpectatorViewOpenCVDll = "SpectatorView.OpenCV.dll";

        [DllImport(SpectatorViewOpenCVDll, EntryPoint = "Initialize")]
        internal static extern bool InitializeNative();

        [DllImport(SpectatorViewOpenCVDll, EntryPoint = "DetectMarkers")]
        internal static extern bool DetectMarkersNative(
            byte[] imageData,
            int imageWidth,
            int imageHeight,
            float[] focalLength,
            float[] principalPoint,
            float[] radialDistortion,
            int radialDistortionCount,
            float[] tangentialDistortion,
            int tangentialDistortionCount,
            float markerSize,
            int arUcoMarkerDictionaryId);

        [DllImport(SpectatorViewOpenCVDll, EntryPoint = "GetDetectedMarkersCount")]
        internal static extern int GetDetectedMarkersCountNative();

        [DllImport(SpectatorViewOpenCVDll, EntryPoint = "GetDetectedMarkerIds")]
        internal static extern bool GetDetectedMarkerIdsNative(int[] detectedIds, int size);

        [DllImport(SpectatorViewOpenCVDll, EntryPoint = "GetDetectedMarkerPose")]
        internal static extern bool GetDetectedMarkerPoseNative(int detectedId, float[] position, float[] rotation);

        /// <summary>
        /// True if the SpectatorViewPlugin.dll has been successfully initialized
        /// </summary>
        public bool IsInitialized { get; private set; } = false;

        private float _markerSize = 0.03f; // meters
        private const int _arucoDictionaryId = 10; // equivalent to cv::aruco::DICT_6X6_250

        /// <summary>
        /// Called to initialize SpectatorViewPlugin.dll
        /// </summary>
        /// <param name="markerSize">Physical size of the markers being detected in meters</param>
        public bool Initialize(float markerSize)
        {
            _markerSize = markerSize;

            try
            {
                if (InitializeNative())
                {
                    IsInitialized = true;
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to call InitializeNative: " + ex.ToString());
            }

            Debug.LogWarning("SpectatorView.OpenCV.dll did not initialize correctly");
            return false;
        }

        public void SetMarkerSize(float markerSize)
        {
            _markerSize = markerSize;
        }
        
        /// <summary>
        /// Assesses the provided image for ArUco markers
        /// </summary>
        /// <param name="imageData"></param>
        /// <param name="imageWidth"></param>
        /// <param name="imageHeight"></param>
        /// <param name="pixelFormat"></param>
        /// <param name="intrinsics"></param>
        /// <param name="extrinsics"></param>
        /// <returns></returns>
        public Dictionary<int, Marker> ProcessImage(
            byte[] imageData,
            uint imageWidth,
            uint imageHeight,
            PixelFormat pixelFormat,
            CameraIntrinsics intrinsics,
            CameraExtrinsics extrinsics)
        {
            var dictionary = new Dictionary<int, Marker>();

            if (!IsInitialized)
            {
                Debug.LogError("Process image called but SpectatorViewPlugin.dll did not initialize correctly");
                return dictionary;
            }

            if (!IsValidPixelFormat(pixelFormat))
            {
                Debug.LogError("Error: SpectatorViewPlugin.dll expects BGRA pixel format, actual pixel format: " + pixelFormat.ToString());
                return dictionary;
            }

            var focalLength = new float[2];
            focalLength[0] = intrinsics.FocalLength.x;
            focalLength[1] = intrinsics.FocalLength.y;
            var principalPoint = new float[2];
            principalPoint[0] = intrinsics.PrincipalPoint.x;
            principalPoint[1] = intrinsics.PrincipalPoint.y;
            var radialDistortion = new float[3];
            radialDistortion[0] = intrinsics.RadialDistortion.x;
            radialDistortion[1] = intrinsics.RadialDistortion.y;
            radialDistortion[2] = intrinsics.RadialDistortion.z;
            var tangentialDistortion = new float[2];
            tangentialDistortion[0] = intrinsics.TangentialDistortion.x;
            tangentialDistortion[1] = intrinsics.TangentialDistortion.y;

            if (DetectMarkersNative(
                imageData,
                (int)imageWidth,
                (int)imageHeight,
                focalLength,
                principalPoint,
                radialDistortion,
                radialDistortion.Length,
                tangentialDistortion,
                tangentialDistortion.Length,
                _markerSize,
                _arucoDictionaryId))
            {
                int count = GetDetectedMarkersCountNative();
                if (count > 0)
                {
                    var ids = new int[count];
                    if (GetDetectedMarkerIdsNative(ids, ids.Length))
                    {
                        var cameraToWorldMatrix = extrinsics.ViewFromWorld;

                        for (int i = 0; i < ids.Length; i++)
                        {
                            var id = ids[i];
                            var position = new float[3];
                            var rotation = new float[3];
                            if (GetDetectedMarkerPoseNative(id, position, rotation))
                            {
                                Vector3 positionInOpenCVCameraSpace = new Vector3(position[0], -position[1], position[2]);

                                // The below logic ensures the following marker orientation:
                                // Positive x axis is in the left direction of the observed marker
                                // Positive y axis is in the upward direction of the observed marker
                                // Positive z axis is facing outward from the observed marker
                                Vector3 rodriguesVector = new Vector3(rotation[0], rotation[1], rotation[2]);
                                Marker marker = CreateMarkerFromPositionAndRotation(id, positionInOpenCVCameraSpace, rodriguesVector, cameraToWorldMatrix);
                                Debug.Log("Marker detected: " + marker.ToString());

                                dictionary[id] = marker;
                            }
                            else
                            {
                                Debug.LogError("Unable to obtain pose for marker: " + id);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Unable to obtain marker ids");
                    }
                }
            }
            else
            {
                Debug.LogError("Unable to detect markers");
            }

            return dictionary;
        }

        public static Marker CreateMarkerFromPositionAndRotation(int id, Vector3 openCVPosition, Vector3 openCVRodriguesRotation, Matrix4x4 cameraToWorldMatrix)
        {

#if TRYTHIS
            openCVPosition.y *= -1f;

            var angle = Mathf.Rad2Deg * openCVRodriguesRotation.magnitude;
            var axis = openCVRodriguesRotation.normalized;
            Quaternion q = Quaternion.AngleAxis(angle, axis);
            Quaternion rotation = Quaternion.Euler(
                -1.0f * q.eulerAngles.x,
                q.eulerAngles.y,
                -1.0f * q.eulerAngles.z) * Quaternion.Euler(0, 0, 180);


            var tOpenCV = Matrix4x4.TRS(openCVPosition, rotation, Vector3.one);
            var t = tOpenCV;
            t.m20 *= -1.0f;
            t.m21 *= -1.0f;
            t.m22 *= -1.0f;
            t.m23 *= -1.0f;

            Matrix4x4 transformUnityCamera = t;

            // Use camera to world transform to get world pose of marker
            Matrix4x4 transformUnityWorld = cameraToWorldMatrix * transformUnityCamera;

            // Apply updated transform to gameobject in world
            var marker = new Marker(id, transformUnityWorld.GetColumn(3), Quaternion.LookRotation(transformUnityWorld.GetColumn(2), transformUnityWorld.GetColumn(1)));
            return marker;
#else
            openCVPosition = new Vector3(1, 1, 1);
            openCVRodriguesRotation = new Vector3(3, 3, 3);
            cameraToWorldMatrix = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, -1, 0), new Vector4(0, 0, 0, 1));

            var angle = Mathf.Rad2Deg * openCVRodriguesRotation.magnitude;
            var axis = openCVRodriguesRotation.normalized;
            Quaternion rotationInOpenCVCameraSpace = Quaternion.AngleAxis(angle, axis);
            rotationInOpenCVCameraSpace = Quaternion.Euler(
                -1.0f * rotationInOpenCVCameraSpace.eulerAngles.x,
                rotationInOpenCVCameraSpace.eulerAngles.y,
                -1.0f * rotationInOpenCVCameraSpace.eulerAngles.z) * Quaternion.Euler(0, 0, 180);

            var transformInOpenCVCameraSpace = Matrix4x4.TRS(openCVPosition, rotationInOpenCVCameraSpace, Vector3.one);

            var transformInUnityWorld = cameraToWorldMatrix * transformInOpenCVCameraSpace;

            var positionInUnityWorld = transformInUnityWorld.GetColumn(3);
            var rotationInUnityWorld = Quaternion.LookRotation(transformInUnityWorld.GetColumn(2), transformInUnityWorld.GetColumn(1));

            var marker = new Marker(id, positionInUnityWorld, rotationInUnityWorld);


            return marker;
#endif
        }

        private bool IsValidPixelFormat(PixelFormat pixelFormat)
        {
            return (pixelFormat == PixelFormat.BGRA8);
        }
    }
}
