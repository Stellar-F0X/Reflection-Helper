namespace ReflectionHelper
{
    public static class PathUtility
    {
        private readonly static List<SyntaxSegment> _PathSegments = new List<SyntaxSegment>(15);


        public static List<SyntaxSegment> ParsePath(ReadOnlySpan<char> path)
        {
            _PathSegments.Clear();

            Span<char> buffer = stackalloc char[path.Length]; // 임시 이름/인덱스 버퍼

            int i = 0;

            while (i < path.Length)
            {
                int nameLength = 0;

                while (i < path.Length && (char.IsLetterOrDigit(path[i]) || path[i] == '_'))
                {
                    buffer[nameLength++] = path[i++];
                }

                string name = new string(buffer.Slice(0, nameLength));

                if (i < path.Length && path[i] == '[')
                {
                    i++; // skip '['

                    int indexLength = 0;

                    while (i < path.Length && path[i] != ']')
                    {
                        buffer[indexLength++] = path[i++];
                    }

                    i++; // skip ']'

                    Span<char> indexSpan = buffer.Slice(0, indexLength);

                    if (int.TryParse(indexSpan, out int index))
                    {
                        _PathSegments.Add(new SyntaxSegment(name, index));
                    }
                    else
                    {
                        _PathSegments.Add(new SyntaxSegment(name, indexSpan.ToString()));
                    }
                }
                else
                {
                    _PathSegments.Add(new SyntaxSegment(name));
                }

                if (i < path.Length && path[i] == '.')
                {
                    i++;
                }
            }

            return _PathSegments;
        }
    }
}