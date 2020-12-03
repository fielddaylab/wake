using BeauUtil;
using UnityEngine;

namespace Aqua
{
    public interface IFactVisitor
    {
        void Visit(BestiaryFactBase inFact, PlayerFactParams inParams = null);
        void Visit(BestiaryFactEat inFact, PlayerFactParams inParams = null);
    }
}