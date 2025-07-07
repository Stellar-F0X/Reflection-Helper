using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ReflectionHelper
{
    [Serializable]
    public struct SyntaxNode
    {
        public SyntaxNode(Type type)
        {
            this.type = type;
            this.isEndpoint = false;
            this.children = new Dictionary<string, SyntaxNode>(StringComparer.OrdinalIgnoreCase);
        }

        private const BindingFlags _BINDING_FLAGS = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        public Dictionary<string, SyntaxNode> children;

        public Type type;

        public bool isEndpoint;


        public void AddPath(List<SyntaxSegment> segments, Type currentType, int startIndex = 0)
        {
            if (startIndex == segments.Count)
            {
                isEndpoint = true;
                return;
            }

            SyntaxSegment currentSegment = segments[startIndex];

            if (children.ContainsKey(currentSegment.name) == false)
            {
                Type childType = this.GetChildType(currentSegment, currentType);

                children[currentSegment.name] = new SyntaxNode(childType);
            }

            Type nextType = this.GetChildType(currentSegment, currentType);
            children[currentSegment.name].AddPath(segments, nextType, startIndex + 1);
        }


        public void RemovePath(List<SyntaxSegment> segments, int startIndex = 0)
        {
            if (startIndex == segments.Count)
            {
                isEndpoint = false;
                return;
            }

            SyntaxSegment currentSegment = segments[startIndex];

            if (children.ContainsKey(currentSegment.name))
            {
                children[currentSegment.name].RemovePath(segments, startIndex + 1);

                // 자식 노드가 더 이상 유효하지 않으면 제거
                if (children[currentSegment.name].IsValid() == false)
                {
                    children.Remove(currentSegment.name);
                }
            }
        }


        public bool IsValidPath(List<SyntaxSegment> segments, int startIndex = 0)
        {
            if (startIndex == segments.Count)
            {
                return isEndpoint;
            }

            SyntaxSegment currentSegment = segments[startIndex];

            if (children.ContainsKey(currentSegment.name) == false)
            {
                return false;
            }

            return children[currentSegment.name].IsValidPath(segments, startIndex + 1);
        }


        public bool IsValid()
        {
            if (isEndpoint)
            {
                return true;
            }

            foreach (KeyValuePair<string, SyntaxNode> childPair in children)
            {
                MemberInfo? memberInfo = null;

                try
                {
                    memberInfo = this.GetMemberInfo(type, childPair.Key);
                }
                catch
                {
                    continue;
                }

                if (memberInfo == null)
                {
                    continue;
                }

                if (childPair.Value.IsValid())
                {
                    return true;
                }
            }

            return false;
        }


        public Expression? ResolveExpression(List<SyntaxSegment> segments)
        {
            if (segments.Count == 0)
            {
                return null;
            }

            ParameterExpression parameter = Expression.Parameter(type, "obj");

            return this.BuildExpression(parameter, segments, 0, segments.Count);
        }


        public Action<object, object>? ResolveSetter(List<SyntaxSegment> segments)
        {
            if (segments.Count == 0)
            {
                return null;
            }

            ParameterExpression parameter = Expression.Parameter(typeof(object), "obj");
            ParameterExpression valueParameter = Expression.Parameter(typeof(object), "value");

            UnaryExpression typedParameter = Expression.Convert(parameter, type);

            Expression? targetExpression;

            if (segments.Count > 1)
            {
                targetExpression = this.BuildExpression(typedParameter, segments, 0, segments.Count - 1);
            }
            else
            {
                targetExpression = typedParameter;
            }

            if (targetExpression == null)
            {
                return null;
            }

            SyntaxSegment lastSegment = segments.Last();

            MemberInfo memberInfo = this.GetMemberInfo(targetExpression.Type, lastSegment.name);

            if (this.CanWrite(memberInfo) == false)
            {
                return null;
            }

            Type memberType = this.GetMemberType(memberInfo);
            UnaryExpression convertedValue = Expression.Convert(valueParameter, memberType);
            MemberExpression memberAccess = Expression.MakeMemberAccess(targetExpression, memberInfo);
            BinaryExpression assignment = Expression.Assign(memberAccess, convertedValue);
            return Expression.Lambda<Action<object, object>>(assignment, parameter, valueParameter).Compile();
        }



        public Func<object, object>? ResolveGetter(List<SyntaxSegment> segments)
        {
            if (segments.Count == 0)
            {
                return null;
            }

            ParameterExpression parameter = Expression.Parameter(typeof(object), "obj");
            UnaryExpression typedParameter = Expression.Convert(parameter, type);

            Expression? targetExpression = this.BuildExpression(typedParameter, segments, 0, segments.Count);

            if (targetExpression == null)
            {
                return null;
            }

            // 결과를 object 타입으로 변환
            UnaryExpression convertedResult = Expression.Convert(targetExpression, typeof(object));

            return Expression.Lambda<Func<object, object>>(convertedResult, parameter).Compile();
        }


        private Expression? BuildExpression(Expression current, List<SyntaxSegment> segments, int start, int end)
        {
            Expression expr = current;

            for (int index = start; index < end; ++index)
            {
                SyntaxSegment segment = segments[index];

                MemberInfo memberInfo = this.GetMemberInfo(expr.Type, segment.name);

                MemberExpression memberAccess = Expression.MakeMemberAccess(expr, memberInfo);

                if (segment.hasIndex == false)
                {
                    expr = memberAccess;
                    continue;
                }

                if (memberAccess.Type.IsArray)
                {
                    expr = Expression.ArrayIndex(memberAccess, Expression.Constant(segment.index));
                }
                else
                {
                    PropertyInfo indexer = memberAccess.Type.GetProperty("Item");

                    if (indexer == null)
                    {
                        return null;
                    }

                    ConstantExpression indexerExpression = Expression.Constant(segment.isStringIndex ? segment.stringIndex : segment.index);

                    expr = Expression.Property(memberAccess, indexer, indexerExpression);
                }
            }

            return expr;
        }


        /// <summary> 자식 타입을 반환하는 함수. </summary>
        /// <param name="segment"></param>
        /// <param name="currentType"></param>
        /// <returns></returns>
        private Type GetChildType(SyntaxSegment segment, Type currentType)
        {
            MemberInfo memberInfo = this.GetMemberInfo(currentType, segment.name);

            Type memberType = this.GetMemberType(memberInfo);

            if (segment.hasIndex)
            {
                if (memberType.IsArray)
                {
                    return memberType.GetElementType();
                }

                if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return memberType.GetGenericArguments()[0];
                }
            }

            return memberType;
        }


        /// <summary>
        /// 경로가 현재 타입 구조에서 유효한지 실시간으로 검증하는 함수.
        /// </summary>
        /// <param name="segments">접근할 경로 세그먼트 리스트</param>
        /// <param name="currentType">검증을 시작할 루트 타입</param>
        /// <returns>경로가 유효하면 true, 아니면 false</returns>
        public bool ValidatePathStructure(List<SyntaxSegment> segments, Type currentType)
        {
            foreach (SyntaxSegment segment in segments)
            {
                try
                {
                    //현재 타입에서 세그먼트 이름에 해당하는 멤버를 조회함.
                    MemberInfo memberInfo = this.GetMemberInfo(currentType, segment.name);

                    Type memberType = this.GetMemberType(memberInfo);

                    currentType = this.GetChildType(segment, memberType);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }



        /// <summary> 프로퍼티와 필드를 모두 검색하는 함수. </summary>
        /// <param name="type"></param>
        /// <param name="memberName"></param>
        /// <returns></returns>
        private MemberInfo GetMemberInfo(Type type, string memberName)
        {
            PropertyInfo propertyInfo = type.GetProperty(memberName, _BINDING_FLAGS);

            if (propertyInfo != null)
            {
                return propertyInfo;
            }

            return type.GetField(memberName, _BINDING_FLAGS);
        }


        /// <summary> 멤버의 타입을 반환하는 함수. </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private Type GetMemberType(MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case PropertyInfo propertyInfo: return propertyInfo.PropertyType;

                case FieldInfo fieldInfo: return fieldInfo.FieldType;
            }

            throw new ArgumentException($"Unsupported member type: {memberInfo.GetType()}");
        }


        /// <summary> 멤버가 쓰기 가능한지 확인하는 함수. </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        private bool CanWrite(MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case PropertyInfo propertyInfo: return propertyInfo.CanWrite;

                case FieldInfo fieldInfo: return fieldInfo.IsInitOnly == false && fieldInfo.IsLiteral == false;
            }

            return false;
        }
    }
}