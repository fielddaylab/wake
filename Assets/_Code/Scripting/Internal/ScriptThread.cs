#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

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

        // temp state
        private ScriptFlags m_Flags;
        private ScriptObject m_Actor;
        private int m_CutsceneCount;

        // temp resources
        private FaderRect m_CurrentFader;
        private ScreenWipe m_CurrentWipe;
        private DialogPanel m_CurrentDialog;
        private Routine m_SkipRoutine;
        #if DEVELOPMENT
        private float m_SkipTimeScaleRestore;
        #endif // DEVELOPMENT

        // record state
        private bool m_RecordedDialog;
        private StringHash32 m_LastKnownCharacter;
        private string m_LastKnownName;
        private DialogRecord m_LastKnownChoiceDialog;

        // trigger state
        private string m_TriggerNodeName;
        private StringHash32 m_TriggerWho;
        private TriggerPriority m_TriggerPriority;
        private StringHash32 m_TriggerId;

        public ScriptThread(ScriptingService inMgr)
            : base(inMgr)
        {
            m_Mgr = inMgr;
        }

        #region Lifecycle

        public ScriptThreadHandle Prep(ScriptObject inContext, TempAlloc<VariantTable> inTempTable)
        {
            Setup(null, inContext, inTempTable);
            m_Actor = inContext;

            m_CutsceneCount = 0;

            return GetHandle();
        }

        public new ScriptThreadHandle GetHandle()
        {
            return new ScriptThreadHandle(base.GetHandle());
        }

        public void SyncPriority(ScriptNode inNode)
        {
            m_TriggerNodeName = inNode.FullName();
            m_TriggerId = inNode.TriggerOrFunctionId();
            m_TriggerWho = inNode.TargetId();
            m_TriggerPriority = inNode.Priority();
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

        public new ScriptObject Actor { get { return m_Actor; } }

        public bool IsCutscene() { return (m_Flags & ScriptFlags.Cutscene) != 0 || m_CutsceneCount > 0; }
        public string InitialNodeName() { return m_TriggerNodeName; }
        public StringHash32 TriggerId() { return m_TriggerId; }
        public StringHash32 Target() { return m_TriggerWho; }
        public TriggerPriority Priority() { return m_TriggerPriority; }

        #endregion // Temp State

        #region Records

        /// <summary>
        /// Records an instance of dialog text.
        /// </summary>
        public void RecordDialog(TagString inString)
        {
            if (inString.RichText.Length <= 0)
            {
                StringHash32 characterId;
                string characterName;
                if (ScriptingService.TryFindCharacter(inString, out characterId, out characterName))
                {
                    m_LastKnownCharacter = characterId;
                    m_LastKnownName = characterName;
                }
                return;
            }
            
            DialogRecord record = DialogRecord.FromTag(inString, m_LastKnownCharacter, m_LastKnownName, !m_RecordedDialog, false);
            m_LastKnownCharacter = record.CharacterId;
            m_LastKnownName = record.Name;
            m_RecordedDialog = true;
            m_LastKnownChoiceDialog = record;

            Services.Data.AddToDialogHistory(record);
        }

        /// <summary>
        /// Records a player choice.
        /// </summary>
        public void RecordChoice(string inChoice)
        {
            DialogRecord record = new DialogRecord()
            {
                CharacterId = "player",
                Text = inChoice,
                IsBoundary = true,
                IsChoice = true
            };

            m_LastKnownCharacter = record.CharacterId;
            m_LastKnownName = record.Name;
            m_RecordedDialog = false;

            Services.Data.AddToDialogHistory(record);
        }

        #endregion // Records

        #region Cutscene

        public void PushCutscene()
        {
            m_CutsceneCount++;
            Services.UI.ShowLetterbox();
        }

        public void PopCutscene()
        {
            if (m_CutsceneCount > 0)
            {
                --m_CutsceneCount;
                Services.UI.HideLetterbox();
            }
        }

        #endregion // Cutscene
        
        #region Skipping

        public void Skip()
        {
            if ((m_Flags & ScriptFlags.Skip) == 0 && !InChoice() && !m_SkipRoutine)
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

        public bool StopSkipping()
        {
            if ((m_Flags & ScriptFlags.Skip) != 0)
            {
                m_Flags &= ~ScriptFlags.Skip;
                if (IsCutscene())
                {
                    m_SkipRoutine.Stop();
                    #if DEVELOPMENT
                    Time.timeScale = m_SkipTimeScaleRestore;
                    #else
                    Time.timeScale = 1;
                    #endif // DEVELOPMENT
                    Services.Input.ResumeAll();
                    Services.UI.StopSkipCutscene();
                    m_Routine.SetTimeScale(1);
                }

                return true;
            }

            return false;
        }

        public bool IsSkipping()
        {
            return (m_Flags & ScriptFlags.Skip) != 0;
        }

        private IEnumerator SkipCutsceneRoutine()
        {
            m_Routine.Pause();
            Services.Input.PauseAll();
            yield return Services.UI.StartSkipCutscene();
            InternalSkip();
            yield return 0.1f;
            m_Routine.Resume();
        }

        private void InternalSkip()
        {
            m_Flags |= ScriptFlags.Skip;

            #if DEVELOPMENT
            m_SkipTimeScaleRestore = Time.timeScale;
            #endif // DEVELOPMENT
            Time.timeScale = 100;
            m_Routine.SetTimeScale(1000);
            if (IsCutscene())
            {
                Services.Events.Dispatch(GameEvents.CutsceneSkip);
            }
            if (Dialog != null)
            {
                Dialog.Skip();
            }
        }

        public void MarkChoice()
        {
            m_Flags |= ScriptFlags.InChoice;
            Services.Events.Dispatch(GameEvents.ScriptChoicePresented, m_LastKnownChoiceDialog);
        }

        public void EndChoice()
        {
            m_Flags &= ~ScriptFlags.InChoice;
        }

        private bool InChoice()
        {
            return (m_Flags & ScriptFlags.InChoice) != 0;
        }

        #endregion // Skipping

        protected override void Reset()
        {
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
                #if DEVELOPMENT
                Time.timeScale = m_SkipTimeScaleRestore;
                #else
                Time.timeScale = 1;
                #endif // DEVELOPMENT
                Services.Input.ResumeAll();
                Services.UI.StopSkipCutscene();
            }

            m_SkipRoutine.Stop();

            m_Mgr.UntrackThread(this);

            m_Actor = null;
            m_Flags = 0;

            m_TriggerWho = StringHash32.Null;
            m_TriggerPriority = TriggerPriority.Low;

            m_LastKnownCharacter = StringHash32.Null;
            m_LastKnownName = null;
            m_RecordedDialog = false;
            m_LastKnownChoiceDialog = default(DialogRecord);

            base.Reset();

            while(m_CutsceneCount > 0)
            {
                Services.UI.HideLetterbox();
                --m_CutsceneCount;
            }

            m_Pool.Free(this);
        }

        #region IPooledObject

        void IPooledObject<ScriptThread>.OnAlloc()
        {
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
        }

        #endregion // IPooledObject
    }

    internal enum ScriptFlags : UInt32
    {
        None = 0x00,

        Skip = 0x10,
        Cutscene = 0x20,
        InChoice = 0x40,
    }

    [UnityEngine.Scripting.Preserve]
    public class BindThreadHandleAttribute : BindThreadAttribute
    {
        public override object Bind(object inSource)
        {
            ScriptThread thread = (ScriptThread) base.Bind(inSource);
            if (thread != null)
                return thread.GetHandle();
            return default(ScriptThreadHandle);
        }
    }
}