using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using ProtoAqua;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProtoAqua
{
    public class ScriptObject : MonoBehaviour, ISceneLoadHandler, ISceneUnloadHandler
    {
        #region Inspector

        [SerializeField] private string m_ClassName = "";
        [SerializeField] private string m_Id = "default-object";
    
        #endregion // Inspector

        public StringHash Id() { return m_Id; }
        public StringHash ClassName() { return m_ClassName; }

        void ISceneLoadHandler.OnSceneLoad(SceneBinding inScene, object inContext)
        {
            // throw new NotImplementedException();
        }

        void ISceneUnloadHandler.OnSceneUnload(SceneBinding inScene, object inContext)
        {
            // throw new NotImplementedException();
        }
    }
}