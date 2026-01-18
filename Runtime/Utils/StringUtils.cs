namespace KrasCore
{
    public static class StringUtils
    {
        public static string RemoveAllWhitespace(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var buffer = new char[input.Length];
            var idx = 0;

            foreach (var c in input)
            {
                if (!char.IsWhiteSpace(c))
                    buffer[idx++] = c;
            }

            return new string(buffer, 0, idx);
        }
    }
}