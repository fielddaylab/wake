using System;
using System.Collections;
using Aqua.Profile;
using Aqua.Scripting;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Services;
using Aqua.Portable;
using Leaf;
using Leaf.Runtime;
using UnityEngine.Scripting;

namespace Aqua.Argumentation {
    [ServiceDependency(typeof(DataService), typeof(AssetsService), typeof(EventService), typeof(ScriptingService))]
    internal partial class ArgumentationService : ServiceBehaviour {
        [ServiceReference, Preserve] static private ArgumentationService s_Instance;

        static private readonly StringHash32 ArgumentTableId = "argue";
        static public readonly StringHash32 CorrectArgumentChoice = "correct";
        static public readonly StringHash32 IncorrectArgumentChoice = "incorrect";

        [NonSerialized] private StringHash32 m_CurrentId;
        [NonSerialized] private bool m_CurrentCompleted;
        [NonSerialized] private ArgueData m_CurrentStatus;
        [NonSerialized] private bool m_ClaimSetup;
        private readonly StringHash32[] m_RevertSubmitState = new StringHash32[ArgueConsts.MaxFactsPerClaim]; 
        private Routine m_Sentinel;
        [NonSerialized] private bool m_ShouldRevert = false;

        #region Argue

        private bool LoadArgue(StringHash32 id, out bool created) {
            if (m_CurrentId == id) {
                created = false;
                return false;
            }

            if (id.IsEmpty) {
                UnloadArgue(true);
                created = false;
                return true;
            }

            UnloadArgue(false);

            Services.UI.ShowLetterbox();

            ScienceData profile = Save.Science;
            m_CurrentId = id;
            m_CurrentStatus = profile.GetArgue(id, out created);
            m_CurrentCompleted = m_CurrentStatus == null;
            if (!m_CurrentCompleted) {
                Services.Data.BindTable(ArgumentTableId, m_CurrentStatus.Vars);
            }
            Services.Events.Queue(ArgueEvents.Loaded, id);
            return true;
        }

        private void UnloadArgue(bool dispatchEvent) {
            if (m_CurrentId.IsEmpty) {
                return;
            }

            m_CurrentId = StringHash32.Null;
            m_CurrentCompleted = false;
            m_ClaimSetup = false;
            Services.Data.UnbindTable(ArgumentTableId);

            Services.UI.HideLetterbox();

            if (dispatchEvent) {
                Services.Events.Queue(ArgueEvents.Unloaded);
            }
        }

        private bool IsCompleted(StringHash32 id = default) {
            if (id.IsEmpty) {
                id = m_CurrentId;
                if (id.IsEmpty) {
                    return false;
                }
            }

            return Save.Science.IsArgueCompleted(id);
        }

        private bool Complete() {
            if (!m_CurrentId.IsEmpty && !m_CurrentCompleted) {
                Save.Science.CompleteArgue(m_CurrentId);
                m_CurrentCompleted = true;
                m_ClaimSetup = false;
                Services.Events.Queue(ArgueEvents.Completed, m_CurrentId);
                return true;
            }

            return false;
        }

        private IEnumerator SentinelRoutine(ScriptThreadHandle threadHandle) {
            yield return threadHandle.Wait();
            UnloadArgue(true);
        }

        #endregion // Argue

        #region Claim

        private bool SetClaim(StringHash32 id, StringHash32 labelId) {
            if (m_CurrentId.IsEmpty) {
                Log.Error("[ArgumentationService] No argument loaded");
                return false;
            }

            if (m_CurrentCompleted || m_CurrentStatus == null) {
                Log.Error("[ArgumentationService] Cannot set claim - argument {0} is already completed", m_CurrentId);
                return false;
            }
            
            if (m_CurrentStatus.ClaimId == id) {
                return false;
            }

            m_CurrentStatus.ClaimId = id;
            m_CurrentStatus.ClaimLabel = labelId;

            int factCount = m_CurrentStatus.ExpectedFacts.Count;
            Array.Clear(m_CurrentStatus.FactSlots, 0, factCount);
            Array.Clear(m_CurrentStatus.SubmittedFacts, 0, factCount);
            m_CurrentStatus.ExpectedFacts.Clear();
            m_CurrentStatus.OnChanged();
            m_ClaimSetup = !id.IsEmpty;
            return true;
        }

        private void AddFactSlot(StringHash32 id) {
            if (m_CurrentId.IsEmpty) {
                Log.Error("[ArgumentationService] No argument loaded");
                return;
            }

            if (m_CurrentCompleted || m_CurrentStatus == null) {
                Log.Error("[ArgumentationService] Cannot add fact slot - argument {0} is already completed", m_CurrentId);
                return;
            }

            if (m_CurrentStatus.ClaimId.IsEmpty) {
                Log.Error("[ArgumentationService] Cannot add facts slots to empty claim on argument {0}", m_CurrentId);
                return;
            }

            if (!m_ClaimSetup) {
                Log.Error("[ArgumentationService] Cannot add fact slots to argument {0} claim {1} after initial setup", m_CurrentId, m_CurrentStatus.ClaimId);
                return;
            }

            m_CurrentStatus.ExpectedFacts.Add(id);
            var shape = BFType.Shape(Assets.Fact(id));
            m_CurrentStatus.FactSlots[m_CurrentStatus.ExpectedFacts.Count - 1] = shape;
            m_CurrentStatus.OnChanged();
        }

        private bool SubmitFact(StringHash32 id) {
            if (m_CurrentId.IsEmpty) {
                Log.Error("[ArgumentationService] No argument loaded");
                return false;
            }

            if (m_CurrentCompleted || m_CurrentStatus == null) {
                Log.Error("[ArgumentationService] Cannot accept fact - argument {0} is already completed", m_CurrentId);
                return false;
            }

            if (m_CurrentStatus.ClaimId.IsEmpty) {
                Log.Error("[ArgumentationService] Cannot accept fact on empty claim on argument {0}", m_CurrentStatus.ClaimId);
                return false;
            }

            int factIdx = NextAvailableSlot(Assets.Fact(id));
            if (factIdx < 0) {
                return false;
            }
            
            m_CurrentStatus.SubmittedFacts[factIdx] = id;
            m_CurrentStatus.OnChanged();
            Services.Events.Queue(ArgueEvents.FactSubmitted, id);
            return true;
        }

        private bool SubmitFact(BFBase inFact) {
            if (m_CurrentId.IsEmpty) {
                Log.Error("[ArgumentationService] No argument loaded");
                return false;
            }

            if (m_CurrentCompleted || m_CurrentStatus == null) {
                Log.Error("[ArgumentationService] Cannot accept fact - argument {0} is already completed", m_CurrentId);
                return false;
            }

            if (m_CurrentStatus.ClaimId.IsEmpty) {
                Log.Error("[ArgumentationService] Cannot accept fact on empty claim on argument {0}", m_CurrentStatus.ClaimId);
                return false;
            }

            int factIdx = NextAvailableSlot(inFact);
            if (factIdx < 0) {
                return false;
            }
            
            m_CurrentStatus.SubmittedFacts[factIdx] = inFact.Id;
            m_CurrentStatus.OnChanged();
            Services.Events.Queue(ArgueEvents.FactSubmitted, inFact.Id);
            return true;
        }

        private int NextAvailableSlot(BFBase inFact) {
            int factCount = m_CurrentStatus.ExpectedFacts.Count;
            BFShapeId shape = BFType.Shape(inFact);
            int lowestIndex = -1;
            int i = 0;
            for(; i < factCount; i++) {
                if (!m_CurrentStatus.SubmittedFacts[i].IsEmpty) {
                    if (m_CurrentStatus.SubmittedFacts[i] == inFact.Id) {
                        return -1;
                    } else {
                        continue;
                    }
                } else if (m_CurrentStatus.FactSlots[i] == shape) {
                    lowestIndex = i;
                    break;
                }
            }

            for(; i < factCount; i++) {
                if (m_CurrentStatus.SubmittedFacts[i] == inFact.Id) {
                    lowestIndex = -1;
                    break;
                }
            }

            return lowestIndex;
        }

        private bool RejectFact(StringHash32 id) {
            if (m_CurrentId.IsEmpty) {
                Log.Error("[ArgumentationService] No argument loaded");
                return false;
            }

            if (m_CurrentCompleted || m_CurrentStatus == null) {
                Log.Error("[ArgumentationService] Cannot reject fact - argument {0} is already completed", m_CurrentId);
                return false;
            }

            if (m_CurrentStatus.ClaimId.IsEmpty) {
                Log.Error("[ArgumentationService] Cannot reject fact on empty claim on argument {0}", m_CurrentStatus.ClaimId);
                return false;
            }

            int factIdx = ArrayUtils.IndexOf(m_CurrentStatus.SubmittedFacts, id);
            if (factIdx < 0) {
                return false;
            }

            m_CurrentStatus.SubmittedFacts[factIdx] = default;
            m_CurrentStatus.OnChanged();
            Services.Events.Queue(ArgueEvents.FactRejected, id);
            return true;
        }

        private bool RejectIncorrectFacts() {
            if (m_CurrentId.IsEmpty) {
                Log.Error("[ArgumentationService] No argument loaded");
                return false;
            }

            if (m_CurrentCompleted || m_CurrentStatus == null) {
                Log.Error("[ArgumentationService] Cannot reject incorrect facts - argument {0} is already completed", m_CurrentId);
                return false;
            }

            if (m_CurrentStatus.ClaimId.IsEmpty) {
                Log.Error("[ArgumentationService] Cannot reject incorrect facts on empty claim on argument {0}", m_CurrentStatus.ClaimId);
                return false;
            }

            bool bChanged = false;
            int factCount = m_CurrentStatus.ExpectedFacts.Count;
            for(int i = 0; i < factCount; i++) {
                StringHash32 id = m_CurrentStatus.SubmittedFacts[i];
                if (!id.IsEmpty && !m_CurrentStatus.ExpectedFacts.Contains(id)) {
                    bChanged = true;
                    m_CurrentStatus.SubmittedFacts[i] = default;
                    Services.Events.Queue(ArgueEvents.FactRejected, id);
                }
            }

            if (bChanged) {
                m_CurrentStatus.OnChanged();
            }
            return true;
        }

        private bool RejectAllFacts() {
            if (m_CurrentId.IsEmpty) {
                Log.Error("[ArgumentationService] No argument loaded");
                return false;
            }

            if (m_CurrentCompleted || m_CurrentStatus == null) {
                Log.Error("[ArgumentationService] Cannot reject all facts - argument {0} is already completed", m_CurrentId);
                return false;
            }

            if (m_CurrentStatus.ClaimId.IsEmpty) {
                Log.Error("[ArgumentationService] Cannot reject all facts on empty claim on argument {0}", m_CurrentStatus.ClaimId);
                return false;
            }

            Array.Clear(m_CurrentStatus.SubmittedFacts, 0, m_CurrentStatus.ExpectedFacts.Count);
            m_CurrentStatus.OnChanged();
            Services.Events.Queue(ArgueEvents.FactsCleared);
            return true;
        }

        private bool IsFactSubmitted(StringHash32 id) {
            if (m_CurrentId.IsEmpty || m_CurrentCompleted || m_CurrentStatus == null || m_CurrentStatus.ClaimId.IsEmpty) {
                return false;
            }

            return ArrayUtils.Contains(m_CurrentStatus.SubmittedFacts, id);;
        }

        private bool AreAllSlotsFilled() {
            if (m_CurrentCompleted) {
                return true;
            }

            if (m_CurrentId.IsEmpty || m_CurrentStatus == null || m_CurrentStatus.ClaimId.IsEmpty) {
                return false;
            }

            int factCount = m_CurrentStatus.ExpectedFacts.Count;
            StringHash32 check;
            for(int i = 0; i < factCount; i++) {
                check = m_CurrentStatus.SubmittedFacts[i];
                if (check.IsEmpty) {
                    return false;
                }
            }

            return true;
        }

        private bool AreAllFactsCorrect() {
            if (m_CurrentCompleted) {
                return true;
            }

            if (m_CurrentId.IsEmpty || m_CurrentStatus == null || m_CurrentStatus.ClaimId.IsEmpty) {
                return false;
            }

            int factCount = m_CurrentStatus.ExpectedFacts.Count;
            StringHash32 check;
            for(int i = 0; i < factCount; i++) {
                check = m_CurrentStatus.SubmittedFacts[i];
                if (check.IsEmpty) {
                    return false;
                }
                if (!m_CurrentStatus.ExpectedFacts.Contains(check)) {
                    return false;
                }
            }

            return true;
        }

        private bool CancelClaim() {
            if (m_CurrentId.IsEmpty || m_CurrentCompleted || m_CurrentStatus == null || m_CurrentStatus.ClaimId.IsEmpty) {
                return false;
            }

            m_CurrentStatus.ClaimId = StringHash32.Null;
            m_CurrentStatus.ClaimLabel = StringHash32.Null;

            int factCount = m_CurrentStatus.ExpectedFacts.Count;
            Array.Clear(m_CurrentStatus.FactSlots, 0, factCount);
            Array.Clear(m_CurrentStatus.SubmittedFacts, 0, factCount);
            m_CurrentStatus.ExpectedFacts.Clear();
            m_CurrentStatus.OnChanged();
            m_ClaimSetup = false;
            Services.Events.Queue(ArgueEvents.ClaimCancelled);
            return true;
        }

        #endregion // Claim

        #region Fact Selector

        private Future<StringHash32> SelectFact() {
            Assert.True(!m_CurrentId.IsEmpty && !m_CurrentCompleted, "Argument is not started or already completed - cannot request argument fact");

            Array.Copy(m_CurrentStatus.SubmittedFacts, m_RevertSubmitState, ArgueConsts.MaxFactsPerClaim);

            PortableRequest request = PortableRequest.SelectFact();
            request.CanSelect = CanSelectAndHasSlot;
            request.OnSelect = HandleSingleSelect;
            request.Response.OnComplete((f) => SubmitFact(f));

            PortableMenu.Request(request);
            return request.Response;
        }

        private Future<StringHash32> SelectFactSet() {
            Assert.True(!m_CurrentId.IsEmpty && !m_CurrentCompleted, "Argument is not started or already completed - cannot request argument fact");
            
            Array.Copy(m_CurrentStatus.SubmittedFacts, m_RevertSubmitState, ArgueConsts.MaxFactsPerClaim);
            m_ShouldRevert = true;

            PortableRequest request = PortableRequest.SelectFactSet(HandleMultiSelect);
            request.CanSelect = CanSelectAndHasSlot;

            PortableMenu.Request(request);
            return request.Response;
        }

        private bool CanSelect(BFBase inFact) {
            return !ArrayUtils.Contains(m_CurrentStatus.SubmittedFacts, inFact.Id);
        }

        private bool CanSelectAndHasSlot(BFBase inFact) {
            return !ArrayUtils.Contains(m_CurrentStatus.SubmittedFacts, inFact.Id) && NextAvailableSlot(inFact) >= 0;
        }

        private bool HandleSingleSelect(BFBase inFact, Future<StringHash32> ioFuture) {
            if (SubmitFact(inFact)) {
                ioFuture.Complete(inFact.Id);
                return false;
            }

            return true;
        }

        private bool HandleMultiSelect(BFBase inFact, Future<StringHash32> ioFuture) {
            if (SubmitFact(inFact) && AreAllSlotsFilled()) {
                if (AreAllFactsCorrect()) {
                    m_ShouldRevert = false;
                    ioFuture.Complete(CorrectArgumentChoice);
                    return false;
                } else {
                    m_ShouldRevert = false;
                    ioFuture.Complete(IncorrectArgumentChoice);
                    return false;
                }
            }

            return true;
        }

        private void TryRevert() {
            if (!m_ShouldRevert) {
                return;
            }

            Array.Copy(m_RevertSubmitState, m_CurrentStatus.SubmittedFacts, ArgueData.MaxFactsPerClaim);
            Services.Events.Queue(ArgueEvents.FactsRefreshed);
            m_ShouldRevert = false;
        }

        #endregion // Fact Selector

        #region IService

        protected override void Initialize() {
            base.Initialize();

            Services.Script.RegisterChoiceSelector("argueFactSet", SelectFactSet);
            Services.Script.RegisterChoiceSelector("argueFact", SelectFact);

            Services.Events.Register(GameEvents.PortableClosed, TryRevert, this);
        }

        protected override void Shutdown() {
            Services.Events?.DeregisterAll(this);
            base.Shutdown();
        }

        #endregion // IService

        #region Leaf

        // arguments

        [LeafMember("ArgueLoad"), Preserve]
        static private void LeafLoadArgument([BindThreadHandle] ScriptThreadHandle threadHandle, StringHash32 id) {
            s_Instance.LoadArgue(id, out bool _);
            s_Instance.m_Sentinel.Replace(s_Instance, s_Instance.SentinelRoutine(threadHandle));
        }

        [LeafMember("ArgueIsComplete"), Preserve]
        static internal bool LeafIsComplete(StringHash32 id = default) {
            return s_Instance.IsCompleted(id);
        }

        [LeafMember("ArgueComplete"), Preserve]
        static private bool LeafCompleteArgument() {
            return s_Instance.Complete();
        }

        [LeafMember("ArgueUnload"), Preserve]
        static private void LeafUnloadArgument() {
            s_Instance.UnloadArgue(true);
        }

        // claims

        [LeafMember("ArgueClaim"), Preserve]
        static private StringHash32 LeafGetClaim() {
            if (s_Instance.m_CurrentStatus != null) {
                return s_Instance.m_CurrentStatus.ClaimId;
            } else {
                return StringHash32.Null;
            }
        }

        [LeafMember("ArgueSetClaim"), Preserve]
        static private bool LeafSetClaim(StringHash32 id, StringHash32 label) {
            return s_Instance.SetClaim(id, label);
        }

        [LeafMember("ArgueFactSlot"), Preserve]
        static private void LeafFactSlot(StringHash32 id) {
            s_Instance.AddFactSlot(id);
        }

        [LeafMember("ArgueDisplayClaim"), Preserve]
        static private void LeafDisplayClaim() {
            s_Instance.m_ClaimSetup = false;
            if (s_Instance.m_CurrentStatus == null) {
                Log.Error("[ArgumentationService] Argumentation is not loaded, or was completed when loaded (id={0})", s_Instance.m_CurrentId);
                return;
            }
            Services.Events.Dispatch(ArgueEvents.ClaimDisplay, s_Instance.m_CurrentStatus);
        }

        [LeafMember("ArgueHideClaim"), Preserve]
        static private void LeafHideClaim() {
            Services.Events.Dispatch(ArgueEvents.ClaimHide);
        }

        [LeafMember("ArgueSubmitFact"), Preserve]
        static private bool LeafSubmitFact(StringHash32 id = default) {
            if (id.IsEmpty) {
                id = Script.ReadVariable("portable:lastSelectedFactId").AsStringHash();
            }
            return s_Instance.SubmitFact(id);
        }

        [LeafMember("ArgueRejectFact"), Preserve]
        static private bool LeafRejectFact(StringHash32 id = default) {
            if (id.IsEmpty) {
                id = Script.ReadVariable("portable:lastSelectedFactId").AsStringHash();
            }
            return s_Instance.RejectFact(id);
        }

        [LeafMember("ArgueRejectIncorrect"), Preserve]
        static private bool LeafRejectIncorrectFacts() {
            return s_Instance.RejectIncorrectFacts();
        }

        [LeafMember("ArgueClearFacts"), Preserve]
        static private bool LeafClearFacts() {
            return s_Instance.RejectAllFacts();
        }

        [LeafMember("ArgueIsFactSubmitted"), Preserve]
        static private bool LeafIsFactSubmitted(StringHash32 id) {
            return s_Instance.IsFactSubmitted(id);
        }

        [LeafMember("ArgueAllFactsCorrect"), Preserve]
        static private bool LeafAllFactsCorrect() {
            return s_Instance.AreAllFactsCorrect();
        }

        [LeafMember("ArgueCancelClaim"), Preserve]
        static private bool LeafCancelClaim() {
            return s_Instance.CancelClaim();
        }

        #endregion // Leaf
    }
}