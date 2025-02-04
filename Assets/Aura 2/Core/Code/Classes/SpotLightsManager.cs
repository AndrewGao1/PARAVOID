﻿
/***************************************************************************
*                                                                          *
*  Copyright (c) Raphaël Ernaelsten (@RaphErnaelsten)                      *
*  All Rights Reserved.                                                    *
*                                                                          *
*  NOTICE: Aura 2 is a commercial project.                                 * 
*  All information contained herein is, and remains the property of        *
*  Raphaël Ernaelsten.                                                     *
*  The intellectual and technical concepts contained herein are            *
*  proprietary to Raphaël Ernaelsten and are protected by copyright laws.  *
*  Dissemination of this information or reproduction of this material      *
*  is strictly forbidden.                                                  *
*                                                                          *
***************************************************************************/ 

using System.Collections.Generic;
using UnityEngine;

namespace Aura2API
{
    /// <summary>
    /// Manager that handles spot AuraLights
    /// </summary>
    public class SpotLightsManager
    {
        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="camera">The reference camera, for culling purposes</param>
        /// <param name="frustumSettings">The frustum settings</param>
        public SpotLightsManager(Camera camera, FrustumSettings frustumSettings)
        {
            _referenceCamera = camera;
            _frustumSettings = frustumSettings;

            _culler = new ObjectsCuller<AuraLight>(_referenceCamera, this._frustumSettings);
            for (int i = 0; i < AuraCamera.CommonDataManager.LightsCommonDataManager.RegisteredSpotLightsList.Count; ++i)
            {
                _culler.Register(AuraCamera.CommonDataManager.LightsCommonDataManager.RegisteredSpotLightsList[i]);
            }

            Camera.onPreRender += Camera_onPreRender;

            AuraCamera.CommonDataManager.LightsCommonDataManager.OnRegisterSpotLight += LightsCommonDataManager_OnRegisterSpotLight;
            AuraCamera.CommonDataManager.LightsCommonDataManager.OnUnregisterSpotLight += LightsCommonDataManager_OnUnregisterSpotLight;
        }
        #endregion

        #region Public Members
        /// <summary>
        /// The shadow map size
        /// </summary>
        public static readonly Vector2Int shadowMapSize = new Vector2Int(256, 256); // TODO : EXPOSE AS DYNAMIC PARAMETER
        /// <summary>
        /// The cookie map size
        /// </summary>
        public static readonly Vector2Int cookieMapSize = new Vector2Int(256, 256); // TODO : EXPOSE AS DYNAMIC PARAMETER
        #endregion

        #region Private Members
        /// <summary>
        /// The culler that will tell which candidate light is visible from the camera
        /// </summary>
        private readonly ObjectsCuller<AuraLight> _culler;
        /// <summary>
        /// The collected packed data of the visible lights
        /// </summary>
        private SpotLightParameters[] _visibleSpotLightParametersArray;
        /// <summary>
        /// Settings of the frustum
        /// </summary>
        public FrustumSettings _frustumSettings;
        /// <summary>
        /// The reference camera to get the clip space from
        /// </summary>
        private Camera _referenceCamera;
        /// <summary>
        /// One count buffer used to assign to the compute shader when the branch path is unused (for cross platform compatibility)
        /// </summary>
        private ComputeBuffer _emptyBuffer;
        #endregion

        #region Properties
        /// <summary>
        /// One count buffer used to assign to the compute shader when the branch path is unused (for cross platform compatibility)
        /// </summary>
        public ComputeBuffer EmptyBuffer
        {
            get
            {
                if (_emptyBuffer == null)
                {
                    _emptyBuffer = new ComputeBuffer(1, SpotLightParameters.Size);
                }

                return _emptyBuffer;
            }
        }

        /// <summary>
        /// Accessor to the compute buffer containing the packed data of the visible lights
        /// </summary>
        public ComputeBuffer Buffer
        {
            get;
            private set;
        }

        /// <summary>
        /// Tells if there are visible/unculled lights
        /// </summary>
        public bool HasVisibleLights
        {
            get
            {
                return _culler.HasVisibleObjects;
            }
        }
        #endregion  

        #region Functions
        /// <summary>
        ///     Called when any camera is a about to render
        /// </summary>
        private void Camera_onPreRender(Camera camera)
        {
            _culler.Update(camera, _frustumSettings);

            SetupComputeBuffer();

            CollectData();
        }

        /// <summary>
        /// Called when a new spot light registers onto the global list
        /// </summary>
        /// <param name="auraLight">The newly registered light</param>
        private void LightsCommonDataManager_OnRegisterSpotLight(AuraLight auraLight)
        {
            _culler.Register(auraLight);
        }

        /// <summary>
        /// Called when a spot light unregisters from the global list
        /// </summary>
        /// <param name="auraLight">The unregistering light</param>
        private void LightsCommonDataManager_OnUnregisterSpotLight(AuraLight auraLight)
        {
            _culler.Unregister(auraLight);
        }

        /// <summary>
        /// Allocate new compute buffer or null, according to visible objects count from culler
        /// </summary>
        private void SetupComputeBuffer()
        {
            if (Buffer == null || _culler.VisibleObjectsCount != Buffer.count)
            {
                DisposeComputeBuffer();

                if (_culler.HasVisibleObjects)
                {
                    Buffer = new ComputeBuffer(_culler.VisibleObjectsCount, SpotLightParameters.Size);
                    _visibleSpotLightParametersArray = new SpotLightParameters[_culler.VisibleObjectsCount];
                }
                else
                {
                    Buffer = null;
                }
            }
        }

        /// <summary>
        /// Resizes the buffers containing the packed data of the visible lights
        /// </summary>
        private void SetupBuffers()
        {
            if (Buffer == null || _culler.VisibleObjectsCount != Buffer.count)
            {
                DisposeComputeBuffer();

                if (_culler.HasVisibleObjects)
                {
                    Buffer = new ComputeBuffer(_culler.VisibleObjectsCount, SpotLightParameters.Size);
                    _visibleSpotLightParametersArray = new SpotLightParameters[_culler.VisibleObjectsCount];
                }
                else
                {
                    Buffer = null;
                }
            }
        }

        /// <summary>
        /// Collects the light's data and pack them in the computeBuffer
        /// </summary>
        private void CollectData()
        {
            if (_culler.HasVisibleObjects)
            {
                AuraLight[] visibleLights = _culler.GetVisibleObjects();
                for (int i = 0; i < _culler.VisibleObjectsCount; ++i)
                {
                    _visibleSpotLightParametersArray[i] = visibleLights[i].GetSpotParameters();
                }

                Buffer.SetData(_visibleSpotLightParametersArray);
            }
        }

        /// <summary>
        /// Releases the computeBuffer
        /// </summary>
        private void DisposeComputeBuffer()
        {
            if (Buffer != null)
            {
                Buffer.Release();
                Buffer = null;
            }
        }

        /// <summary>
        /// Disposes the members
        /// </summary>
        public void Dispose()
        {
            //_culler.Dispose();

            DisposeComputeBuffer();

            if (_emptyBuffer != null)
            {
                _emptyBuffer.Release();
                _emptyBuffer = null;
            }

            Camera.onPreRender -= Camera_onPreRender;
            AuraCamera.CommonDataManager.LightsCommonDataManager.OnRegisterSpotLight -= LightsCommonDataManager_OnRegisterSpotLight;
            AuraCamera.CommonDataManager.LightsCommonDataManager.OnUnregisterSpotLight -= LightsCommonDataManager_OnUnregisterSpotLight;
        }
        #endregion
    }
}
