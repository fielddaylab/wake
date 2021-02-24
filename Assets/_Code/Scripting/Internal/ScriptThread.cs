using System;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Variants;
using BeauUtil.Tags;
using System.Collections;
using Leaf.Runtime;
using UnityEngine;

namespace Aqua.Scripting
{
    internal class ScriptThread : LeafThreadState<ScriptNode>, IPooledObject<ScriptThread>
    {
        // ownership
        private ScriptingService m_Mgr;
        private IPool<ScriptThread> m_Pool;
        
        public readonly CustomVariantResolver Resolver;
        public TagString TagString;

        // identification
        private string m_Name;
        private uint m_Id;

        // temp state
        private ScriptFlags m_Flags;
        private IScriptContext m_Context;
        private int m_CutsceneCount;
        private Routine m_RunningRoutine;
        private bool m_Active;

        // temp resources
        private TempAlloc<VariantTable> m_TempTable;
        private FaderRect m_CurrentFader;
        private ScreenWipe m_CurrentWipe;
        private DialogPanel m_CurrentDialog;
        private Routine m_SkipRoutine;

        // trigger state
        private StringHash32 m_TriggerWho;
        private TriggerPriority m_TriggerPriority;

        // cached callbacks
        private readonly Action m_KillCallback;

        public ScriptThread()
        {
            Resolver = new CustomVariantResolver();
            TagString = new TagString();

            m_KillCallback = Kill;
        }

        #region Lifecycle

        public void Initialize(ScriptingService inMgr, IVariantResolver inBase)
        {
            m_Mgr = inMgr;
            Resolver.Base = inBase;
        }

        public ScriptThreadHandle Prep(string inName, IScriptContext inContext, TempAlloc<VariantTable> inTempTable)
        {
            m_Context = inContext;
            m_TempTable = inTempTable;

            if (m_TempTable?.Object != null)
            {
                Resolver.SetDefaultTable(inTempTable);
            }

            if (inContext?.Vars != null)
            {
                Resolver.SetTable("self", inContext.Vars);
            }

            m_CutsceneCount = 0;
            m_Name = inName;
            m_Id = (m_Id == uint.MaxValue ? 1 : m_Id + 1);

            return GetHandle();
        }

        public ScriptThreadHandle GetHandle()
        {
            return new ScriptThreadHandle(this, m_Id);
        }

        public void AttachToRoutine(Routine inRoutine)
        {
            m_RunningRoutine = inRoutine;
            inRoutine.OnComplete(m_KillCallback);
        }

        public void SyncPriority(ScriptNode inNode)
        {
            m_TriggerWho = inNode.TargetId();
            m_TriggerPriority = inNode.Priority();
        }

        public void Delay(float inDelay)
        {
            m_RunningRoutine.DelayBy(inDelay);
        }

        public bool HasId(uint inId)
        {
            return m_Active && m_Id == inId;
        }

        public void Kill()
        {
            if (m_Active)
            {
                m_Pool.Free(this);
            }
        }

        #endregion // Lifecycle

        #region Temp Ownership

        public DialogPanel Dialog
        {
            get { return m_CurrentDialog; }
            set
            {
                if (m_CurrentDialog != value)
                {
                    if (m_CurrentDialog != null)
                    {
                        m_CurrentDialog.CompleteSequence();
                    }
                    m_CurrentDialog = value;
                }
            }
        }

        public FaderRect ScreenFader
        {
            get { return m_CurrentFader; }
            set
            {
                if (m_CurrentFader != value)
                {
                    if (m_CurrentFader != null)
                    {
                        m_CurrentFader.Hide(0.1f, true);
                    }

                    m_CurrentFader = value;
                }
            }
        }

        public void ClearFaderWithoutHide()
        {
            m_CurrentFader = null;
        }

        public ScreenWipe ScreenWipe
        {
            get { return m_CurrentWipe; }
            set
            {
                if (m_CurrentWipe != value)
                {
                    if (m_CurrentWipe != null)
                    {
                        m_CurrentWipe.Hide(true);
                    }

                    m_CurrentWipe = value;
                }
            }
        }

        public void ClearWipeWithoutHide()
        {
            m_CurrentWipe = null;
        }

        #endregion // Temp Ownership

        #region Temp State

        public string Name { get { return m_Name; } }
        public IScriptContext Context { get { return m_Context; } }
        public VariantTable Locals { get { return m_TempTable; } }

        public bool IsRunning() { return m_RunningRoutine; }
        public void Pause() { m_RunningRoutine.Pause(); }
        public void Resume() { m_RunningRoutine.Resume(); }
        public bool IsPaused() { return m_RunningRoutine.GetPaused(); }
        public IEnumerator Wait() { return m_RunningRoutine.Wait(); }

        public bool IsCutscene() { return (m_Flags & ScriptFlags.Cutscene) != 0 || m_CutsceneCount > 0; }
        public StringHash32 Target() { return m_TriggerWho; }
        public TriggerPriority Priority() { return m_TriggerPriority; }

        #endregion // Temp State

        #region Cutscene

        public void PushCutscene()
        {
            m_CutsceneCount++;
            Services.UI.ShowLetterbox();
        }

        public void PopCutscene()
        {
            --m_CutsceneCount;
            Services.UI.HideLetterbox();
        }

        #endregion // Cutscene
        
        #region Skipping

        public void Tick()
        {
            m_RunningRoutine.TryManuallyUpdate(0);
        }

        public void Skip()
        {
            if ((m_Flags & ScriptFlags.Skip) == 0)
            {
                if (IsCutscene())
                {
                    m_Flags |= ScriptFlags.Cutscene;
                    m_SkipRoutine = Routine.Start(m_Mgr, SkipCutsceneRoutine());
                }
                else
                {
                    InternalSkip();
                }
            }
        }

        public bool IsSkipping()
        {
            return (m_Flags & ScriptFlags.Skip) != 0;
        }

        private IEnumerator SkipCutsceneRoutine()
        {
            m_RunningRoutine.Pause();
            Services.Input.PauseAll();
            yield return Services.UI.StartSkipCutscene();
            InternalSkip();
            yield return 0.1f;
            m_RunningRoutine.Resume();
        }

        private void InternalSkip()
        {
            m_Flags |= ScriptFlags.Skip;
            Time.timeScale = 100;
            m_RunningRoutine.SetTimeScale(1000);
            if (IsCutscene())
            {
                Services.Events.Dispatch(GameEvents.CutsceneSkip);
            }
            if (Dialog != null)
            {
                Dialog.Skip();
            }
        }

        #endregion // Skipping

        #region IPooledObject

        void IPooledObject<ScriptThread>.OnAlloc()
        {
            m_Active = true;
        }

        void IPooledObject<ScriptThread>.OnConstruct(IPool<ScriptThread> inPool)
        {
            m_Pool = inPool;
        }

        void IPooledObject<ScriptThread>.OnDestruct()
        {
        }

        void IPooledObject<ScriptThread>.OnFree()
        {
            m_Active = false;

            if (m_CurrentFader != null)
            {
                m_CurrentFader.Hide(0.1f, true);
                m_CurrentFader = null;
            }

            if (m_CurrentWipe != null)
            {
                m_CurrentWipe.Hide(true);
                m_CurrentWipe = null;
            }

            if (m_CurrentDialog != null)
            {
                m_CurrentDialog.CompleteSequence();
                m_CurrentDialog = null;
            }

            if (IsCutscene() && (m_Flags & ScriptFlags.Skip) != 0)
            {
                Time.timeScale = 1;
                Services.Input.ResumeAll();
                Services.UI.StopSkipCutscene();
            }

            while(m_CutsceneCount > 0)
            {
                Services.UI.HideLetterbox();
                --m_CutsceneCount;
            }

            Reset(m_Mgr);

            m_RunningRoutine.Stop();
            m_SkipRoutine.Stop();

            m_Mgr.UntrackThread(this);

            Resolver.Clear();
            Ref.Dispose(ref m_TempTable);
            m_Context = null;
            m_Name = null;
            m_Flags = 0;

            m_TriggerWho = StringHash32.Null;
            m_TriggerPriority = TriggerPriority.Low;
        }

        #endregion // IPooledObject
    }

    internal enum ScriptFlags : UInt32
    {
        None = 0x00,

        Skip = 0x10,
        Cutscene = 0x20
    }
}