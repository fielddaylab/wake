/*
 * Copyright (C) 2022. Autumn Beauchesne, Field Day Lab
 * Author:  Autumn Beauchesne
 * Date:    1 June 2022
 * 
 * File:    StreamingLoadIcon.cs
 * Purpose: Displays a loading icon when an IStreamingComponent is loading.
 */

#if USING_BEAUUTIL
using BeauUtil;
#endif // USING_BEAUUTIL

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace EasyAssetStreaming {
    [AddComponentMenu("Streaming Assets/Streaming Load Icon")]
    [RequireComponent(typeof(IStreamingComponent))]
    public sealed class StreamingLoadIcon : MonoBehaviour {

        public GameObject LoadingIcon;

        private readonly StreamingComponentEvent OnUpdated;
        [NonSerialized] private bool m_LastKnownLoading;
        [NonSerialized] private IStreamingComponent m_Component;
        [NonSerialized] private IStreamingTextureComponent m_ComponentAsTexture;

        private StreamingLoadIcon() {
            OnUpdated = (c, s) => {
                bool loading = s == Streaming.AssetStatus.PendingLoad;
                if (loading != m_LastKnownLoading) {
                    m_LastKnownLoading = loading;
                    LoadingIcon.SetActive(loading);
                    if (m_ComponentAsTexture != null) {
                        m_ComponentAsTexture.Visible = !loading;
                    }
                }
            };
        }

        private void Awake() {
            m_Component = GetComponent<IStreamingComponent>();
            m_ComponentAsTexture = m_Component as IStreamingTextureComponent;
            m_Component.OnUpdated += OnUpdated;
            
            m_LastKnownLoading = m_Component.IsLoading();
            LoadingIcon.SetActive(m_LastKnownLoading);
            if (m_ComponentAsTexture != null) {
                m_ComponentAsTexture.Visible = !m_LastKnownLoading;
            }
        }

        private void OnDestroy() {
            if (m_Component != null) {
                m_Component.OnUpdated -= OnUpdated;
            }
        }
    }
}