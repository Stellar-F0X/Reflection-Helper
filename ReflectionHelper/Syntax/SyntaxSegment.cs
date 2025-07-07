namespace ReflectionHelper
{
    public readonly struct SyntaxSegment
    {
        public SyntaxSegment(in string name)
        {
            this.name = name;
            hasIndex = false;
            isStringIndex = false;
            stringIndex = null;
            index = 0;
        }

        public SyntaxSegment(in string name, int index)
        {
            this.name = name;
            this.index = index;
            hasIndex = true;
            isStringIndex = false;
            stringIndex = null;
        }

        public SyntaxSegment(in string name, in string stringIndex)
        {
            this.name = name;
            this.stringIndex = stringIndex;
            hasIndex = true;
            isStringIndex = true;
            index = 0;
        }

        public string name { get; }
        public int index { get; }
        public string stringIndex { get; }
        public bool hasIndex { get; }
        public bool isStringIndex { get; }
    }
}