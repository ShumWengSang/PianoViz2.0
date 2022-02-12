﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// Helper class that scales mobile UI to show at same physical size across devices
    /// </summary>
    public class MobileUIScaler : MonoBehaviour
    {
        private void Awake()
        {
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
            float designDpi = 424;
            float dpi = Screen.dpi;

            float scaler = dpi / designDpi;
            gameObject.transform.localScale *= scaler;
#else
            gameObject.transform.localScale *= 1.0f;
#endif
        }
    }
}
