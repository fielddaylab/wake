namespace Aqua
{
    public interface IFactVisitor
    {
        void Visit(BFBase inFact, PlayerFactParams inParams = null);

        void Visit(BFBody inFact, PlayerFactParams inParams = null);
        void Visit(BFWaterProperty inFact, PlayerFactParams inParams = null);
        
        void Visit(BFEat inFact, PlayerFactParams inParams = null);
        void Visit(BFGrow inFact, PlayerFactParams inParams = null);
        void Visit(BFReproduce inFact, PlayerFactParams inParams = null);
        void Visit(BFConsume inFact, PlayerFactParams inParams = null);
        void Visit(BFProduce inFact, PlayerFactParams inParams = null);

        void Visit(BFStateStarvation inFact, PlayerFactParams inParams = null);
        void Visit(BFStateRange inFact, PlayerFactParams inParams = null);
        void Visit(BFStateAge inFact, PlayerFactParams inParams = null);
    }
}