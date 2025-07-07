namespace ReflectionHelper.Test
{
    class A
    {
        public B b;
    }

    class B
    {
        public C[] c { get; set; }
    }

    class C
    {
        public D d;
        public int value;
    }

    class D
    {
        public int value { get; set; }
    }


    public class Program
    {
        static void Main(string[] args)
        {
            A instance = new A();
            instance.b = new B();
            instance.b.c = new C[1];
            instance.b.c[0] = new C();
            instance.b.c[0].d = new D();
            
            SyntaxTree pathTree = new SyntaxTree();
            pathTree.AddPath(typeof(A), "b.c[0].d.value");
            
            Console.WriteLine(instance.b.c[0].value);
            
            Action<object, object>? valueSetter = pathTree.ResolveSetter(typeof(A), "b.c[0].value");
            Func<object, object>? valueGetter = pathTree.ResolveGetter(typeof(A), "b.c[0].value");
            valueSetter.Invoke(instance, 1000);

            Console.WriteLine(instance.b.c[0].value);
            valueSetter.Invoke(instance, 5000);
            Console.WriteLine("----------------------------------------");
            object value = valueGetter.Invoke(instance);
            Console.WriteLine(value);
        }
    }
}