namespace KrasCore.AccumulatorGenerator
{
    internal readonly struct AccumulatorData
    {
        public AccumulatorData(string typeName, string fullTypeName, string structName, string divisor)
        {
            TypeName = typeName;
            FullTypeName = fullTypeName;
            StructName = structName;
            Divisor = divisor;
        }

        public string TypeName { get; }

        public string FullTypeName { get; }

        public string StructName { get; }

        public string Divisor { get; }
    }
}
