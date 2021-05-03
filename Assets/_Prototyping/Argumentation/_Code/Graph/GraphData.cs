using BeauUtil;
using BeauUtil.Blocks;

namespace ProtoAqua.Argumentation
{
    public class GraphData : IDataBlock
    {
        protected StringHash32 m_Id = null;
        protected string m_Name = null;

        public GraphData(string inId)
        {
            m_Id = inId;
            m_Name = inId;
        }

        public string Name
        {
            get { return m_Name; }
        }

        public StringHash32 Id
        {
            get { return m_Id; }
        }
    }
}
