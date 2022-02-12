﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.SpectatorView
{
    public class RecordingServiceVisual : MonoBehaviour,
        IRecordingServiceVisual,
        IMobileOverlayVisualChild
    {
        /// <summary>
        /// Screen recording states
        /// </summary>
        protected enum RecordingState
        {
            Ready,
            Initializing,
            Recording
        }

        /// <summary>
        /// Game object that contains the record and preview buttons
        /// </summary>
        [Tooltip("Game object that contains the record and preview buttons")]
        [SerializeField]
        protected GameObject _buttonParent;

        /// <summary>
        /// Button that toggles starting/stopping recording
        /// </summary>
        [Tooltip("Button that toggles starting/stopping recording")]
        [SerializeField]
        protected Button _recordButton;

        /// <summary>
        /// Image enabled on recording button when not recording
        /// </summary>
        [Tooltip("Image enabled on recording button when not recording")]
        [SerializeField]
        protected Image _startRecordingImage;

        /// <summary>
        /// Background image enabled on recording button when not recording
        /// </summary>
        [Tooltip("Background image enabled on recording button when not recording")]
        [SerializeField]
        protected Image _startRecordingBackgroundImage;

        /// <summary>
        /// Image enabled on recording button when recording
        /// </summary>
        [Tooltip("Image enabled on recording button when recording")]
        [SerializeField]
        protected Image _stopRecordingImage;

        /// <summary>
        /// Background image enabled on recording button when recording
        /// </summary>
        [Tooltip("Background image enabled on recording button when recording")]
        [SerializeField]
        protected Image _stopRecordingBackgroundImage;

        /// <summary>
        /// Button used to view last recorded video
        /// </summary>
        [Tooltip("Button used to view last recorded video")]
        [SerializeField]
        protected Button _previewButton;

        /// <summary>
        /// Game object shown when counting down to start recording
        /// </summary>
        [Tooltip("Game object shown when counting down to start recording")]
        [SerializeField]
        protected GameObject _countdownParent;

        /// <summary>
        /// Text updated to contain current countdown value when starting recording
        /// </summary>
        [Tooltip("Text updated to contain current countdown value when starting recording")]
        [SerializeField]
        protected Text _countdownText;

        /// <summary>
        /// Length of time (in seconds) for countdown to start recording
        /// </summary>
        [Tooltip("Length of time (in seconds) for countdown to start recording")]
        [SerializeField]
        protected float _countdownLength = 3;

        private IRecordingService _recordingService;
        private float _recordingStartTime = 0;
        private RecordingState state = RecordingState.Ready;
        private bool _updateRecordingUI = false;
        private bool _readyToRecord = false;

        /// <inheritdoc/>
        public event OverlayVisibilityRequest OverlayVisibilityRequest;

        /// <inheritdoc/>
        public void SetRecordingService(IRecordingService recordingService)
        {
            _recordingService = recordingService;
        }

        protected void Start()
        {
            if (_countdownParent != null)
            {
                _countdownParent.SetActive(false);
            }

            Show();
        }

        protected void Update()
        {
            if (_recordingService != null &&
                _previewButton != null)
            {
                bool showPreviewButton = _recordingService.IsRecordingAvailable();
                _previewButton.gameObject.SetActive(showPreviewButton);
            }

            if (state == RecordingState.Initializing)
            {
                // When initializing, we need to always update the ui based on the countdown timer
                _updateRecordingUI = true;

                if (!_readyToRecord)
                {
                    var countdownComplete = (Time.time - _recordingStartTime) > _countdownLength;
                    if (countdownComplete &&
                        _recordingService.IsInitialized())
                    {
                        Debug.Log("Preparing to record");
                        // Because recording is currently based on screen capture logic, we need to delay recording until we've
                        // hidden our overlay visuals
                        _readyToRecord = true;
                        OverlayVisibilityRequest?.Invoke(false);
                    }
                }
            }

            if (_updateRecordingUI)
            {
                UpdateRecordingUI();
            }
        }

        public void OnRecordClick()
        {
            Debug.Log("Record button clicked");

            if (_recordingService == null)
            {
                Debug.LogError("Error: Recording service not set for RecordingServiceVisual");
                return;
            }

            if (state == RecordingState.Ready)
            {
                StartRecording();
            }
            else if (state == RecordingState.Recording)
            {
                StopRecording();
            }
            else if (state == RecordingState.Initializing)
            {
                StopInitializing();
            }
        }

        private void StartRecording()
        {
            Debug.Log("Initializing recording");
            _recordingService.Initialize();
            state = RecordingState.Initializing;
            _updateRecordingUI = true;
            _recordingStartTime = Time.time;
            _readyToRecord = false;
        }

        private void StopRecording()
        {
            Debug.Log("Stopping recording");
            _recordingService.StopRecording();
            _recordingService.Dispose();
            state = RecordingState.Ready;
            _updateRecordingUI = true;
            _readyToRecord = false;
        }

        private void StopInitializing()
        {
            Debug.Log("Stopping initializing");
            _recordingService.Dispose();
            state = RecordingState.Ready;
            _updateRecordingUI = true;
            _readyToRecord = false;
        }

        public void OnPreviewClick()
        {
            Debug.Log("Preview button clicked");

            if (_recordingService == null)
            {
                Debug.LogError("Error: Recording service not set for RecordingServiceVisual");
                return;
            }

            if (_recordingService.IsRecordingAvailable())
            {
                Debug.Log("Showing recording");
                _recordingService.ShowRecording();
            }
            else
            {
                Debug.LogError("Recording wasn't available to show");
            }
        }

        private void UpdateRecordingUI()
        {
            _updateRecordingUI = false;

            var startImageActive = (state == RecordingState.Ready);
            if (_startRecordingImage != null)
            {
                _startRecordingImage.gameObject.SetActive(startImageActive);
            }

            if (_startRecordingBackgroundImage != null)
            {
                _startRecordingBackgroundImage.gameObject.SetActive(startImageActive);
            }

            var stopImageActive = (state == RecordingState.Initializing) || (state == RecordingState.Recording);
            if (_stopRecordingImage != null)
            {
                _stopRecordingImage.gameObject.SetActive(stopImageActive);
            }

            if (_stopRecordingBackgroundImage != null)
            {
                _stopRecordingBackgroundImage.gameObject.SetActive(stopImageActive);
            }

            if (_countdownParent != null)
            {
                var countdownActive = (state == RecordingState.Initializing) && (!_readyToRecord);
                _countdownParent.gameObject.SetActive(countdownActive);

                if (countdownActive)
                {
                    var dt = Time.time - _recordingStartTime;
                    var countdownVal = (int)(_countdownLength - dt);
                    if (countdownVal < 0)
                    {
                        countdownVal = 0;
                    }
                    _countdownText.text = countdownVal.ToString();
                }
            }
        }

        /// <inheritdoc/>
        public void Show()
        {
            if (state == RecordingState.Recording)
            {
                StopRecording();
            }

            _buttonParent.SetActive(true);
        }

        /// <inheritdoc/>
        public void Hide()
        {
            _buttonParent.SetActive(false);

            if (_readyToRecord)
            {
                Debug.Log("Starting recording");
                _recordingService.StartRecording();
                state = RecordingState.Recording;
            }
        }
    }
}
