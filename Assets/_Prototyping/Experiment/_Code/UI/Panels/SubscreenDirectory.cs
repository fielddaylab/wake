using System;
using UnityEngine;
using System.Collections.Generic;

namespace ProtoAqua.Experiment {
    public class SubscreenDirectory {
        private List<ExpSubscreen> SubEnum = new List<ExpSubscreen>((ExpSubscreen[])Enum.GetValues(typeof(ExpSubscreen)));

        private List<ExperimentSetupSubscreen> SubScreens = new List<ExperimentSetupSubscreen>();

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

        public List<ExperimentSetupSubscreen> AllSubscreens() {
            return SubScreens;
        }

        public void SetSequence(ExpSubscreen[] Seq) {
            if(Sequence.Count > 0) {
                Sequence.Clear();
            }
            Sequence = new List<ExpSubscreen>(Seq);

        }

        public ExpSubscreen[] GetSequence() {
            return Sequence.ToArray();
        }

        public ExperimentSetupSubscreen GetNext(ExpSubscreen curr) {
            if((Sequence.IndexOf(curr) < Sequence.Count -1) && (Sequence.IndexOf(curr) >= 0))
                return SubScreens[SubEnum.IndexOf(Sequence[Sequence.IndexOf(curr) + 1])];
            else {
                return null;
            }
        }

        public ExperimentSetupSubscreen GetPrevious(ExpSubscreen curr) {
            if((Sequence.IndexOf(curr) <= Sequence.Count -1) && (Sequence.IndexOf(curr) > 0))
                return SubScreens[SubEnum.IndexOf(Sequence[Sequence.IndexOf(curr) - 1])];
            else {
                return null;
            }
        }

        public bool HasNext(ExpSubscreen curr) {
            return Sequence.IndexOf(curr) < Sequence.Count - 1;
        }

        public bool HasPrev(ExpSubscreen curr) {
            return Sequence.IndexOf(curr) > 1;
        }

        public bool HasPrevNext(ExpSubscreen curr) {
            return HasPrev(curr) & HasNext(curr);
        }



    }
}