﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    internal class MaskBroadcaster : ComponentBroadcaster<MaskService, MaskBroadcaster.ChangeType>
    {
        [Flags]
        public enum ChangeType : byte
        {
            None = 0x0,
            Properties = 0x1,
        }

        private Mask maskBroadcaster;
        private MaskProperties previousValues;

        protected override void Awake()
        {
            base.Awake();

            this.maskBroadcaster = GetComponent<Mask>();
        }

        public static bool HasFlag(ChangeType changeType, ChangeType flag)
        {
            return (changeType & flag) == flag;
        }

        protected override bool HasChanges(ChangeType changeFlags)
        {
            return changeFlags != ChangeType.None;
        }

        protected override ChangeType CalculateDeltaChanges()
        {
            ChangeType changeType = ChangeType.None;

            MaskProperties newValues = new MaskProperties(maskBroadcaster);
            if (previousValues != newValues)
            {
                changeType |= ChangeType.Properties;
                previousValues = newValues;
            }

            return changeType;
        }

        protected override void SendCompleteChanges(IEnumerable<INetworkConnection> connections)
        {
            SendDeltaChanges(connections, ChangeType.Properties);
        }

        protected override void SendDeltaChanges(IEnumerable<INetworkConnection> connections, ChangeType changeFlags)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter message = new BinaryWriter(memoryStream))
            {
                ComponentBroadcasterService.WriteHeader(message, this);

                message.Write((byte)changeFlags);

                if (HasFlag(changeFlags, ChangeType.Properties))
                {
                    message.Write(previousValues.enabled);
                    message.Write(previousValues.showMaskGraphic);
                }

                message.Flush();
                memoryStream.TryGetBuffer(out var buffer);
                StateSynchronizationSceneManager.Instance.Send(connections, buffer.Array, buffer.Offset, buffer.Count);
            }
        }

        private struct MaskProperties
        {
            public MaskProperties(Mask mask)
            {
                enabled = mask.enabled;
                showMaskGraphic = mask.showMaskGraphic;
            }

            public bool enabled;
            public bool showMaskGraphic;

            public static bool operator ==(MaskProperties first, MaskProperties second)
            {
                return first.Equals(second);
            }

            public static bool operator !=(MaskProperties first, MaskProperties second)
            {
                return !first.Equals(second);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is MaskProperties))
                {
                    return false;
                }

                MaskProperties other = (MaskProperties)obj;
                return
                    other.enabled == enabled &&
                    other.showMaskGraphic == showMaskGraphic;
            }

            public override int GetHashCode()
            {
                return enabled.GetHashCode();
            }
        }
    }
}
