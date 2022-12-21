using System;
using System.Collections.Generic;
using Aqua.Option;
using AquaAudio;
using BeauUtil;
using BeauUtil.Services;
using BeauUWT;
using EasyAssetStreaming;
using Leaf;
using UnityEngine;
using UnityEngine.Networking;

namespace Aqua
{
    [ServiceDependency(typeof(DataService), typeof(EventService), typeof(AudioMgr))]
    internal partial class NetworkWatcher : ServiceBehaviour
    {
        private readonly HashSet<UnityWebRequest> m_Requests = Collections.NewSet<UnityWebRequest>(24);
        private int m_RetryCount;

        protected override void Initialize()
        {
            base.Initialize();

            Streaming.OnLoadBegin += OnStreamingLoadBegin;
            Streaming.OnLoadResult += OnStreamingLoadEnd;

            OGD.Core.OnRequestSent += OnOGDRequestSent;
            OGD.Core.OnRequestResponse += OnOGDRequestResult;
        }

        protected override void Shutdown()
        {
            Services.Events?.DeregisterAll(this);

            Streaming.OnLoadBegin -= OnStreamingLoadBegin;
            Streaming.OnLoadResult -= OnStreamingLoadEnd;

            OGD.Core.OnRequestSent -= OnOGDRequestSent;
            OGD.Core.OnRequestResponse -= OnOGDRequestResult;

            base.Shutdown();
        }

        private void OnStreamingLoadBegin(StreamingAssetHandle id, long size, UnityWebRequest request, int retryStatus) {
            if (m_Requests.Add(request)) {

            }
        }

        private void OnStreamingLoadEnd(StreamingAssetHandle id, long size, UnityWebRequest request, Streaming.LoadResult resultType) {
            if (m_Requests.Remove(request)) {
                switch(resultType) {
                    case Streaming.LoadResult.Error_Network:
                    case Streaming.LoadResult.Error_Server:
                    case Streaming.LoadResult.Error_Unknown:
                        NetworkStats.OnError.Invoke(request.url);
                        break;
                }
            }
        }

        private void OnOGDRequestSent(UnityWebRequest request, int retryCount) {
            if (m_Requests.Add(request)) {

            }
        }

        private void OnOGDRequestResult(UnityWebRequest request, OGD.Core.Error error) {
            if (m_Requests.Remove(request)) {
                if (error.Status == OGD.Core.ReturnStatus.Success) {
                    return;
                }
                NetworkStats.OnError.Invoke(request.url);
            }
        }

        public int ActiveRequests {
            get { return m_Requests.Count; }
        }

        public int ActiveRetries {
            get { return m_RetryCount; }
        }
    }

    static public class NetworkStats {
        [ServiceReference] static private NetworkWatcher s_Instance;

        static public readonly CastableEvent<string> OnError = new CastableEvent<string>(2);

        static public int ActiveRequests {
            get { return s_Instance.ActiveRequests; }
        }
    }
}