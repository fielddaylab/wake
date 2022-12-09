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

        protected override void Initialize()
        {
            base.Initialize();

            // Streaming.OnLoadBegin
        }

        protected override void Shutdown()
        {
            Services.Events?.DeregisterAll(this);

            base.Shutdown();
        }
    }
}