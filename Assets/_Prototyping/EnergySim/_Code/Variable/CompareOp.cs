using System;

namespace ProtoAqua.Energy
{
    public enum CompareOp : byte
    {
        LessThan,
        LessThanOrEqualTo,
        GreaterThan,
        GreaterThanOrEqualTo
    }

    static public class CompareOpExt
    {
        static public bool Evaluate(this CompareOp inOp, float inLeft, float inRight)
        {
            switch(inOp)
            {
                case CompareOp.LessThan:
                    return inLeft < inRight;
                case CompareOp.LessThanOrEqualTo:
                    return inLeft <= inRight;
                case CompareOp.GreaterThan:
                    return inLeft > inRight;
                case CompareOp.GreaterThanOrEqualTo:
                    return inLeft >= inRight;
                default:
                    throw new ArgumentException("Unrecognized value", nameof(inOp));
            }
        }

        static public bool Evaluate(this CompareOp inOp, int inLeft, int inRight)
        {
            switch(inOp)
            {
                case CompareOp.LessThan:
                    return inLeft < inRight;
                case CompareOp.LessThanOrEqualTo:
                    return inLeft <= inRight;
                case CompareOp.GreaterThan:
                    return inLeft > inRight;
                case CompareOp.GreaterThanOrEqualTo:
                    return inLeft >= inRight;
                default:
                    throw new ArgumentException("Unrecognized value", nameof(inOp));
            }
        }
    }
}