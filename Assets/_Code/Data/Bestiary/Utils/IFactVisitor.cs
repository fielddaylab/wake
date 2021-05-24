namespace Aqua
{
    public interface IFactVisitor
    {
        void Visit(BFBase inFact);

        void Visit(BFBody inFact);
        void Visit(BFWaterProperty inFact);
        void Visit(BFModel inModel);

        void Visit(BFEat inFact);
        void Visit(BFGrow inFact);
        void Visit(BFReproduce inFact);
        void Visit(BFConsume inFact);
        void Visit(BFProduce inFact);
        void Visit(BFState inFact);
        void Visit(BFDeath inFact);
    }
}