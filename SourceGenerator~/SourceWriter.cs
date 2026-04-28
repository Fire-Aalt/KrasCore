namespace KrasCore.AccumulatorGenerator
{
    using System;
    using System.Text;

    internal sealed class SourceWriter
    {
        private readonly StringBuilder builder = new StringBuilder();
        private int indent;

        public void Line(string value)
        {
            this.builder.Append(' ', this.indent * 4);
            this.builder.AppendLine(value);
        }

        public void Blank()
        {
            this.builder.AppendLine();
        }

        public void Indent()
        {
            this.indent++;
        }

        public void Unindent()
        {
            this.indent--;
        }

        public void Block(string declaration, Action<SourceWriter> writeBody)
        {
            this.Line(declaration);
            this.Block(writeBody);
        }

        public void Block(Action<SourceWriter> writeBody)
        {
            this.Line("{");
            this.Indent();
            writeBody(this);
            this.Unindent();
            this.Line("}");
        }

        public override string ToString()
        {
            return this.builder.ToString();
        }
    }
}
