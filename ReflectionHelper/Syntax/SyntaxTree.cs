using System;
using System.Collections.Generic;

namespace ReflectionHelper
{
    [Serializable]
    public class SyntaxTree
    {
        private readonly static Dictionary<Type, SyntaxNode> _SyntaxNodes = new Dictionary<Type, SyntaxNode>(new TypeEqualityComparer());

        private readonly static Dictionary<string, Accessor> _CompiledExpressions = new Dictionary<string, Accessor>(StringComparer.OrdinalIgnoreCase);


        public IEnumerable<Type> compiledTypes
        {
            get { return _SyntaxNodes.Keys; }
        }

        public IEnumerable<string> compiledPaths
        {
            get { return _CompiledExpressions.Keys; }
        }


        public void AddPath(Type type, string path)
        {
            if (_SyntaxNodes.ContainsKey(type) == false)
            {
                _SyntaxNodes[type] = new SyntaxNode(type);
            }

            List<SyntaxSegment> segments = PathUtility.ParsePath(path);
            _SyntaxNodes[type].AddPath(segments, type);
        }


        public void RemovePath(Type type, string path)
        {
            if (_SyntaxNodes.ContainsKey(type) == false)
            {
                return;
            }

            List<SyntaxSegment> segments = PathUtility.ParsePath(path);
            _SyntaxNodes[type].RemovePath(segments);
        }


        public bool IsValidPath(Type type, string path)
        {
            if (_SyntaxNodes.ContainsKey(type) == false)
            {
                return false;
            }

            List<SyntaxSegment> segments = PathUtility.ParsePath(path);
            return _SyntaxNodes[type].IsValidPath(segments);
        }


        public void RemoveInvalidPaths(Type type)
        {
            if (_SyntaxNodes.TryGetValue(type, out SyntaxNode node) == false)
            {
                return;
            }

            foreach (KeyValuePair<string, Accessor> pair in _CompiledExpressions)
            {
                string path = pair.Key[(type.Name.Length + 1)..];

                List<SyntaxSegment> segments = PathUtility.ParsePath(path);

                if (node.ValidatePathStructure(segments, type))
                {
                    continue;
                }

                this.RemovePath(type, path);
                _CompiledExpressions.Remove(pair.Key);
            }
        }


        public Func<object, object>? ResolveGetter(Type parsingObject, string path)
        {
            string key = $"{parsingObject.Name}.{path}";

            if (_CompiledExpressions.TryGetValue(key, out Accessor cached) && cached.getter != null)
            {
                return cached.getter;
            }

            List<SyntaxSegment> segments = PathUtility.ParsePath(path);

            if (segments.Count == 0)
            {
                return null;
            }

            Func<object, object>? getter = null;

            try
            {
                getter = _SyntaxNodes[parsingObject].ResolveGetter(segments);
            }
            catch (Exception e)
            {
                //TODO: 잘못된 경로와 정확한 에러 메세지를 출력하는 로직 작성.
                Console.WriteLine(e);
            }

            if (getter == null)
            {
                return null;
            }

            if (_CompiledExpressions.TryGetValue(key, out Accessor accessor))
            {
                accessor.getter = getter;
            }
            else
            {
                _CompiledExpressions.Add(key, new Accessor(getter, null));
            }

            return getter;
        }


        public Action<object, object>? ResolveSetter(Type parsingObject, string path)
        {
            string key = $"{parsingObject.Name}.{path}";

            if (_CompiledExpressions.TryGetValue(key, out Accessor cached) && cached.setter != null)
            {
                return cached.setter;
            }

            List<SyntaxSegment> segments = PathUtility.ParsePath(path);

            if (segments.Count == 0)
            {
                return null;
            }

            Action<object, object>? setter = null;

            try
            {
                setter = _SyntaxNodes[parsingObject].ResolveSetter(segments);
            }
            catch (Exception e)
            {
                //TODO: 잘못된 경로와 정확한 에러 메세지를 출력하는 로직 작성.
                Console.WriteLine(e);
            }

            if (setter == null)
            {
                return null;
            }

            if (_CompiledExpressions.TryGetValue(key, out Accessor accessor))
            {
                accessor.setter = setter;
            }
            else
            {
                _CompiledExpressions.Add(key, new Accessor(null, setter));
            }

            return setter;
        }
    }
}