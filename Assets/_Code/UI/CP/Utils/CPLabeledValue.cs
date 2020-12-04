namespace ProtoCP
{
    public struct CPLabeledValue
    {
        public object Value;
        public string Label;

        private CPLabeledValue(object inValue)
        {
            Value = inValue;
            if (inValue == null)
                Label = "null";
            else
                Label = inValue.ToString();
        }

        private CPLabeledValue(object inValue, string inLabel)
        {
            Value = inValue;
            Label = inLabel;
        }

        static public CPLabeledValue Make(object inValue)
        {
            return new CPLabeledValue(inValue);
        }

        static public CPLabeledValue Make(object inValue, string inLabel)
        {
            return new CPLabeledValue(inValue, inLabel);
        }
    }

    public struct CPLabeledValue<T>
    {
        public T Value;
        public string Label;

        private CPLabeledValue(T inValue)
        {
            Value = inValue;
            if (inValue == null)
                Label = "null";
            else
                Label = inValue.ToString();
        }

        private CPLabeledValue(T inValue, string inLabel)
        {
            Value = inValue;
            Label = inLabel;
        }

        static public CPLabeledValue<T> Make(T inValue)
        {
            return new CPLabeledValue<T>(inValue);
        }

        static public CPLabeledValue<T> Make(T inValue, string inLabel)
        {
            return new CPLabeledValue<T>(inValue, inLabel);
        }
    }
}