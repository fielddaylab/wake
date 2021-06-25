using BeauData;
using BeauUtil;

namespace Aqua
{
    public static class SerializerUtils
    {
        static public void ObjectArray<T>(this Serializer ioSerializer, string inKey, ref RingBuffer<T> ioBuffer) where T : ISerializedObject
        {
            T[] array = null;
            if (ioSerializer.IsWriting && ioBuffer != null)
                array = ioBuffer.ToArray();

            ioSerializer.ObjectArray(inKey, ref array);

            if (ioSerializer.IsReading)
            {
                ioBuffer.Clear();
                for(int i = 0; i < array.Length; i++)
                    ioBuffer.PushBack(array[i]);
            }
        }
    }
}