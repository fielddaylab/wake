using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ProtoAqua.Experiment {
    
    public class SubscreenDirectory {

        // TODO : Convert into hashmaps
        private List<ExpSubscreen> SubEnum = new List<ExpSubscreen>((ExpSubscreen[])Enum.GetValues(typeof(ExpSubscreen)));

        private List<ExperimentSetupSubscreen> SubScreens = new List<ExperimentSetupSubscreen>();

        private List<bool> Visited = new List<bool>();

        private List<ExpSubscreen> Sequence = new List<ExpSubscreen>();

        public SubscreenDirectory(params ExperimentSetupSubscreen[] screens) {
            foreach(var sc in screens) {
                SubScreens.Add(sc);
            }
            if (screens[0] != null) {
                SubScreens.Insert(0, null);
            }
        }

        
        public ExperimentSetupSubscreen GetSubscreen(ExpSubscreen sEnum) {
            return SubScreens[SubEnum.IndexOf(sEnum)];
        }

        public ExpSubscreen GetEnum(ExperimentSetupSubscreen screen) {
            return SubEnum[SubScreens.IndexOf(screen)];
        }

        public void Refresh() {
            Sequence.Clear();
            Visited.Clear();
        }

        public List<ExperimentSetupSubscreen> AllSubscreens() {
            return SubScreens;
        }

        public bool HasSequence() {
            return Sequence.Count > 0;
        }

        public void SetSequence(ExpSubscreen[] Seq) {
            if(Sequence.Count < 0 || !SeqEquals(Seq))
                Sequence.Clear();
                Visited.Clear();
                Sequence = new List<ExpSubscreen>(Seq);
                Visited = new List<bool>(Enumerable.Repeat(false, Seq.Length));
        }

        public bool HasReuse(ExpSubscreen sEnum) {
            int i = 0;
            foreach(var seq in Sequence) {
                if(seq == sEnum) ++i;
            }

            return i != 1;

        }

        public bool SeqEquals(ExpSubscreen[] Seq) {
            return Sequence.SequenceEqual(Seq);
        }

        public bool InSequence(ExpSubscreen sEnum) {
            if(Sequence.Count < 1) return false;
            return Sequence.Contains(sEnum);
        }

        public void SetVisited(ExpSubscreen sEnum) {
            if(Visited.Count > 0) Visited[Sequence.IndexOf(sEnum)] = true;
        }

        public bool IsVisited(ExpSubscreen sEnum) {
            if(Visited.Count < 1) return false;
            return Visited[Sequence.IndexOf(sEnum)];
        }

        public bool IsVisited(int idx) {
            return Visited[idx];
        }


        public ExpSubscreen[] GetSequence() {
            return Sequence.ToArray();
        }
        // TODO: Consider Reuses
        public ExperimentSetupSubscreen GetNext(ExpSubscreen curr) {
            int currIdx = Sequence.IndexOf(curr);
            if(HasReuse(curr) && IsVisited(currIdx)) {
                currIdx = Sequence.IndexOf(curr, currIdx + 1);
            } 

            if((currIdx < Sequence.Count -1) && (currIdx >= 0))
                return SubScreens[SubEnum.IndexOf(Sequence[currIdx + 1])];
            else {
                return null;
            }
        }

        public ExperimentSetupSubscreen GetPrevious(ExpSubscreen curr) {
            int currIdx = Sequence.IndexOf(curr);
            if(HasReuse(curr) && IsVisited(currIdx)) {
                currIdx = Sequence.IndexOf(curr, currIdx + 1);
            }
            if((currIdx <= Sequence.Count -1) && (currIdx > 0))
                return SubScreens[SubEnum.IndexOf(Sequence[currIdx - 1])];
            else {
                return null;
            }
        }

        public bool HasNext(ExpSubscreen curr) {
            return Sequence.IndexOf(curr) < Sequence.Count - 1;
        }

        public bool HasPrev(ExpSubscreen curr) {
            return Sequence.IndexOf(curr) > 0;
        }

        public bool HasPrevNext(ExpSubscreen curr) {
            return HasPrev(curr) & HasNext(curr);
        }



    }
}