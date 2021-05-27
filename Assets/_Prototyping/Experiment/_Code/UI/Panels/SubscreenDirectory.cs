using System;
using UnityEngine;
using System.Collections.Generic;
using BeauUtil;

namespace ProtoAqua.Experiment {
    
    public class SubscreenDirectory {

        private ExperimentSetupSubscreen[] m_Screens = null;
        private ExpSubscreen[] m_Sequence = Array.Empty<ExpSubscreen>();
        private uint m_VisitedMask;

        public SubscreenDirectory(params ExperimentSetupSubscreen[] screens) {
            m_Screens = (ExperimentSetupSubscreen[]) screens.Clone();
        }
        
        public ExperimentSetupSubscreen GetSubscreen(ExpSubscreen sEnum) {
            return m_Screens[(int) sEnum];
        }

        public ExpSubscreen GetEnum(ExperimentSetupSubscreen screen) {
            return (ExpSubscreen) Array.IndexOf(m_Screens, screen);
        }

        public void Refresh() {
            ArrayUtils.Clear(ref m_Sequence);
            m_VisitedMask = 0;
        }

        public ListSlice<ExperimentSetupSubscreen> AllSubscreens() {
            return m_Screens;
        }

        public bool HasSequence() {
            return m_Sequence.Length > 0;
        }

        public void SetSequence(ExpSubscreen[] Seq) {
            if(SeqEquals(Seq))
                return;

            m_Sequence = (ExpSubscreen[]) Seq.Clone();
            m_VisitedMask = 0;
        }

        public bool HasReuse(ExpSubscreen sEnum) {
            int i = 0;
            foreach(var seq in m_Sequence) {
                if(seq == sEnum) ++i;
            }

            return i != 1;

        }

        public bool SeqEquals(ExpSubscreen[] Seq) {
            return ArrayUtils.ContentEquals(m_Sequence, Seq);
        }

        public bool InSequence(ExpSubscreen sEnum) {
            if(m_Sequence.Length < 1) return false;
            return ArrayUtils.Contains(m_Sequence, sEnum);
        }

        public void SetVisited(ExpSubscreen sEnum) {
            int index = Array.IndexOf(m_Sequence, sEnum);
            Bits.Add(ref m_VisitedMask, index);
        }

        public bool IsVisited(ExpSubscreen sEnum) {
            int index = Array.IndexOf(m_Sequence, sEnum);
            return Bits.Contains(m_VisitedMask, index);
        }

        public bool IsVisited(int idx) {
            return Bits.Contains(m_VisitedMask, idx);
        }

        public ListSlice<ExpSubscreen> GetSequence() {
            return m_Sequence;
        }

        // TODO: Consider Reuses
        public ExperimentSetupSubscreen GetNext(ExpSubscreen curr) {
            int currIdx = Array.IndexOf(m_Sequence, curr);
            if(HasReuse(curr) && IsVisited(currIdx)) {
                currIdx = Array.IndexOf(m_Sequence, curr, currIdx + 1);
            } 

            if((currIdx < m_Sequence.Length -1) && (currIdx >= 0))
                return m_Screens[(int) m_Sequence[currIdx + 1]];
            else {
                return null;
            }
        }

        public ExperimentSetupSubscreen GetPrevious(ExpSubscreen curr) {
            int currIdx = Array.IndexOf(m_Sequence, curr);
            if(HasReuse(curr) && IsVisited(currIdx)) {
                currIdx = Array.LastIndexOf(m_Sequence, curr, 0, currIdx);
            }
            if((currIdx <= m_Sequence.Length -1) && (currIdx > 0))
                return m_Screens[(int) m_Sequence[currIdx - 1]];
            else {
                return null;
            }
        }

        public bool HasNext(ExpSubscreen curr) {
            return Array.IndexOf(m_Sequence, curr) < m_Sequence.Length - 1;
        }

        public bool HasPrev(ExpSubscreen curr) {
            return Array.IndexOf(m_Sequence, curr) > 0;
        }

        public bool HasPrevNext(ExpSubscreen curr) {
            return HasPrev(curr) & HasNext(curr);
        }

    }
}