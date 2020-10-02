using System;
using System.Collections.Generic;
using BeauData;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using ProtoAqua;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeauUtil.Variants;

namespace ProtoAqua.Scripting
{
    public class ScriptState : IPooledObject<ScriptState>
    {
        private ScriptingService m_Mgr;

        private ScriptFlags m_Flags;
        private IScriptContext m_Context;

        private Stack<ScriptNode> m_NodeStack;
        private CustomVariantResolver m_Resolver;
        private TempAlloc<VariantTable> m_TempTable;

        private FaderRect m_CurrentFader;
        private ScreenWipe m_CurrentWipe;
        private DialogPanel m_CurrentDialog;

        private int m_CutsceneCount;
        private Routine m_RunningRoutine;

        internal ScriptState()
        {
            m_Resolver = new CustomVariantResolver();
            m_NodeStack = new Stack<ScriptNode>(4);
        }

        public void Initialize(ScriptingService inMgr, CustomVariantResolver inBase)
        {
            m_Mgr = inMgr;
            m_Resolver.Base = inBase;
        }

        public void Prep(IScriptContext inContext, TempAlloc<VariantTable> inTempTable)
        {
            m_Context = inContext;
            m_TempTable = inTempTable;

            if (m_TempTable.Object != null)
            {
                m_Resolver.SetDefaultTable(inTempTable);
            }

            if (inContext?.Vars != null)
            {
                m_Resolver.SetTable("self", inContext.Vars);
            }

            m_CutsceneCount = 0;
        }

        #region IPooledObject

        void IPooledObject<ScriptState>.OnAlloc()
        {
        }

        void IPooledObject<ScriptState>.OnConstruct(IPool<ScriptState> inPool)
        {
            
        }

        void IPooledObject<ScriptState>.OnDestruct()
        {
        }

        void IPooledObject<ScriptState>.OnFree()
        {
            if (m_CurrentFader != null)
            {
                m_CurrentFader.Hide(0.1f);
                m_CurrentFader = null;
            }

            if (m_CurrentWipe != null)
            {
                m_CurrentWipe.Hide();
                m_CurrentWipe = null;
            }

            if (m_CurrentDialog != null)
            {
                m_CurrentDialog.Hide();
                m_CurrentDialog = null;
            }

            while(--m_CutsceneCount >= 0)
            {
                Services.UI.HideLetterbox();
            }

            m_NodeStack.Clear();

            m_RunningRoutine.Stop();
            m_Resolver.Clear();
            Ref.Dispose(ref m_TempTable);
        }

        #endregion // IPooledObject
    }

    public enum ScriptFlags : UInt32
    {
        None = 0x00,

        Skip = 0x10
    }
}