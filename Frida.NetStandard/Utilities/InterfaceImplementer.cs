using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Frida.NetStandard.Utilities
{
    class InterfaceImplementer<T>
    {
        static InterfaceImplementer implem = new InterfaceImplementer(typeof(T));
        public static T Create(IInterfaceImplementation callback)
            => (T)implem.Create(callback);
    }

    public interface IInterfaceImplementation
    {
        object Call(MethodInfo method, object[] arguments);
        Task<object> CallAsync(MethodInfo method, object[] arguments);
    }

    public class InterfaceImplementer
    {
        private readonly Type interfaceType;
        Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();
        private TypeInfo created;


        public InterfaceImplementer(Type interfaceType)
        {
            this.interfaceType = interfaceType;
            AssemblyName assemblyName = new AssemblyName(string.Concat(interfaceType.Namespace, "_", Guid.NewGuid().ToString()));
            var ab = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
            var mb = ab.DefineDynamicModule(string.Concat(assemblyName.Name, ".dll"));



            var tb = mb.DefineType(interfaceType.Name + Guid.NewGuid(), TypeAttributes.Public);
            tb.AddInterfaceImplementation(interfaceType);
            var owner = tb.DefineField("implem", typeof(Store), FieldAttributes.Private);

            GenerateConstructor(tb, owner);
            foreach (MethodInfo mi in GetMethods())
                GenerateMethod(tb, mi, owner);

            created = tb.CreateTypeInfo();
        }

        HashSet<MethodInfo> GetMethods()
        {
            var methods = new HashSet<MethodInfo>();
            var types = new Queue<Type>(new[] { interfaceType });
            while (types.Count > 0)
            {
                var type = types.Dequeue();
                foreach (var m in type.GetMethods())
                    methods.Add(m);
                foreach (var t in type.GetInterfaces())
                    types.Enqueue(t);
            }
            return methods;
        }
        public object Create(IInterfaceImplementation implem)
        {
            return Activator.CreateInstance(created, new[] { new Store(this, implem) });
        }

        public class Store
        {
            public IInterfaceImplementation implem;
            private InterfaceImplementer owner;

            public Store(InterfaceImplementer interfaceImplementer, IInterfaceImplementation implem)
            {
                this.owner = interfaceImplementer;
                this.implem = implem;
            }

            public T Invoke<T>(string method, object[] arguments)
            {
                var m = owner.methods[method];
                var ret = implem.Call(m, arguments);
                return (T)ret;
            }

            public async Task<T> InvokeAsync<T>(string method, object[] arguments)
            {
                var m = owner.methods[method];
                var ret = await implem.CallAsync(m, arguments);
                return (T)ret;
            }
        }


        private void GenerateConstructor(TypeBuilder tb, FieldBuilder owner)
        {
            var ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard
                , new[] { typeof(Store) });
            var il = ctor.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, owner);
            il.Emit(OpCodes.Ret);
        }

        private void GenerateMethod(TypeBuilder tb, MethodInfo mi, FieldInfo owner)
        {
            var parameters = mi.GetParameters();
            var genericArgumentArray = mi.GetGenericArguments();

            MethodBuilder mb = tb.DefineMethod(mi.Name, MethodAttributes.Public | MethodAttributes.Virtual, mi.ReturnType, parameters.Select(pi => pi.ParameterType).ToArray());
            if (genericArgumentArray.Any())
            {
                mb.DefineGenericParameters(genericArgumentArray.Select(s => s.Name).ToArray());
            }


            var il = mb.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, owner);
            il.Emit(OpCodes.Ldstr, mi.Name);

            // method has parameters ?
            if (parameters.Length > 0)
            {
                il.Emit(OpCodes.Ldc_I4_S, parameters.Length);
                il.Emit(OpCodes.Newarr, typeof(object));
                for (int i = 0; i < parameters.Length; i++)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldc_I4_S, i);
                    il.Emit(OpCodes.Ldarg, i + 1);
                    if (parameters[i].ParameterType.IsValueType)
                        il.Emit(OpCodes.Box, parameters[i].ParameterType);
                    il.Emit(OpCodes.Stelem_Ref);
                }
            }
            else
                il.Emit(OpCodes.Ldnull);

            MethodInfo method;
            if (typeof(Task).IsAssignableFrom(mi.ReturnType))
            {
                method = typeof(Store).GetMethod(nameof(Store.InvokeAsync));
                if (mi.ReturnType.IsGenericType)
                    method = method.MakeGenericMethod(mi.ReturnType.GetGenericArguments());
                else
                    method = method.MakeGenericMethod(typeof(string)); // yea, whatever
            }
            else
            {
                method = typeof(Store).GetMethod(nameof(Store.Invoke));
                if (mi.ReturnType == typeof(void))
                    method = method.MakeGenericMethod(typeof(string)); // yea, whatever
                else
                    method = method.MakeGenericMethod(mi.ReturnType);
            }

            il.Emit(OpCodes.Call, method);

            if (mi.ReturnType == typeof(void)) // method has no return type => just pop the result and return.
                il.Emit(OpCodes.Pop);

            il.Emit(OpCodes.Ret);

            tb.DefineMethodOverride(mb, mi);
            methods[mi.Name] = mi;
        }
    }
}
