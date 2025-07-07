using System;

namespace ReflectionHelper
{
    public struct Accessor
    {
        public Accessor(Func<object, object> getter, Action<object, object> setter)
        {
            this.setter = setter;
            this.getter = getter;
        }

        public Func<object, object> getter;

        public Action<object, object> setter;
    }
}