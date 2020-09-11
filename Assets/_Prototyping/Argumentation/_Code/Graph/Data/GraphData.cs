using BeauUtil.Blocks;

namespace ProtoAqua.Argumentation
{
    public class GraphData : IDataBlock
    {
        protected string m_Id = null;

        public GraphData(string inId)
        {
            m_Id = inId;
        }

        public string Id() { return m_Id; }
    }
}
