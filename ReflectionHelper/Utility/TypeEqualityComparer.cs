namespace ReflectionHelper
{
    public class TypeEqualityComparer : IEqualityComparer<Type>
    {
        public bool Equals(Type? x, Type? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.TypeHandle.Equals(y.TypeHandle) && x.Assembly.Equals(y.Assembly);
        }

        public int GetHashCode(Type? obj)
        {
            if (obj is null)
            {
                return 0;
            }

            return HashCode.Combine(obj.TypeHandle, obj.Assembly);
        }
    }
}