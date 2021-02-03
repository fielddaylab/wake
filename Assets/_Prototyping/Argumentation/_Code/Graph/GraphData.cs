using BeauUtil;
using BeauUtil.Blocks;

namespace ProtoAqua.Argumentation
{
    public class GraphData : IDataBlock
    {
        protected StringHash32 m_Id = null;

        public GraphData(string inId)
        {
            m_Id = inId;
        }

        public StringHash32 Id
        {
            get { return m_Id; }
        }
    }
}
