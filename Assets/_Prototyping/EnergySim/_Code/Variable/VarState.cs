using System;
using System.Runtime.InteropServices;

namespace ProtoAqua.Energy
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VarState<T> where T : struct, IConvertible
    {
        public T Var0;
        public T Var1;
        public T Var2;
        public T Var3;
        public T Var4;
        public T Var5;
        public T Var6;
        public T Var7;
        public T Var8;
        public T Var9;
        public T Var10;
        public T Var11;
        public T Var12;
        public T Var13;
        public T Var14;
        public T Var15;

        public T this[int inIndex]
        {
            get
            {
                switch (inIndex)
                {
                    case 0:
                        return Var0;
                    case 1:
                        return Var1;
                    case 2:
                        return Var2;
                    case 3:
                        return Var3;
                    case 4:
                        return Var4;
                    case 5:
                        return Var5;
                    case 6:
                        return Var6;
                    case 7:
                        return Var7;
                    case 8:
                        return Var8;
                    case 9:
                        return Var9;
                    case 10:
                        return Var10;
                    case 11:
                        return Var11;
                    case 12:
                        return Var12;
                    case 13:
                        return Var13;
                    case 14:
                        return Var14;
                    case 15:
                        return Var15;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(inIndex));
                }
            }
            set
            {
                switch (inIndex)
                {
                    case 0:
                        Var0 = value;
                        break;
                    case 1:
                        Var1 = value;
                        break;
                    case 2:
                        Var2 = value;
                        break;
                    case 3:
                        Var3 = value;
                        break;
                    case 4:
                        Var4 = value;
                        break;
                    case 5:
                        Var5 = value;
                        break;
                    case 6:
                        Var6 = value;
                        break;
                    case 7:
                        Var7 = value;
                        break;
                    case 8:
                        Var8 = value;
                        break;
                    case 9:
                        Var9 = value;
                        break;
                    case 10:
                        Var10 = value;
                        break;
                    case 11:
                        Var11 = value;
                        break;
                    case 12:
                        Var12 = value;
                        break;
                    case 13:
                        Var13 = value;
                        break;
                    case 14:
                        Var14 = value;
                        break;
                    case 15:
                        Var15 = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(inIndex));
                }
            }
        }
    }
}