namespace ProtoAqua.Energy
{
    public struct LabeledValue
    {
        public readonly object Value;
        public readonly string Label;

        private LabeledValue(object inValue)
        {
            Value = inValue;
            if (inValue == null)
                Label = "null";
            else
                Label = inValue.ToString();
        }

        private LabeledValue(object inValue, string inLabel)
        {
            Value = inValue;
            Label = inLabel;
        }

        static public LabeledValue Make(object inValue)
        {
            return new LabeledValue(inValue);
        }

        static public LabeledValue Make(object inValue, string inLabel)
        {
            return new LabeledValue(inValue, inLabel);
        }
    }
}