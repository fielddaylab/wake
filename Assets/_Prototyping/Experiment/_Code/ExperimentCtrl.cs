using System;
using UnityEngine;
using BeauData;
using BeauUtil;
using ProtoAudio;
using BeauRoutine;
using System.Collections;
using System.Reflection;
using BeauUtil.Variants;

namespace ProtoAqua.Experiment
{
    public class ExperimentCtrl : MonoBehaviour, ISceneLoadHandler
    {
        public void OnSceneLoad(SceneBinding inScene, object inContext)
        {
        }

        private void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Services.Data.AddVariable("kevin:help.requests", 1);
                Services.Script.TriggerResponse("partner", GameTriggers.RequestPartnerHelp);
            }
        }

        static private void LogSizeOf(Type inType)
        {
            try
            {
                Debug.LogFormat("sizeof({0})={1}", inType.ToString(), System.Runtime.InteropServices.Marshal.SizeOf(inType));
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}