using Frida.NetStandard.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Frida.NetStandard
{
    public abstract class FridaRpcException : Exception
    {
        protected FridaRpcException(string message, Exception ex = null) : base(message, ex) { }
    }
    public class FridaRpcSerializationException : FridaRpcException
    {
        public FridaRpcSerializationException(Exception ex) : base(ex.Message, ex){ }
    }

    public class FridaRpcScriptException : FridaRpcException
    {
        public string ErrorName { get; private set; }
        public string JsErrorStack { get; private set; }

        public FridaRpcScriptException(string message, string errName, string errStack) : base(errStack ?? message)
        {
            this.ErrorName = errName;
            this.JsErrorStack = errStack;
        }

        public override string ToString()
        {
            return $"{Message} at \n {JsErrorStack}";
        }
    }
    public class ScriptWithRpc<TRpc> : Script, IInterfaceImplementation
    {
        public ScriptWithRpc(Func<IntPtr> builder)
            : base(builder)
        {
            Rpc = InterfaceImplementer<TRpc>.Create(this);
        }

        public TRpc Rpc { get; }

        object IInterfaceImplementation.Call(MethodInfo method, object[] arguments)
        {
            return ((IInterfaceImplementation)this).CallAsync(method, arguments)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        class Operation
        {
            public TaskCompletionSource<object> Source;
            public Type ExpectedReturnType;
        }
        Dictionary<Guid, Operation> operations = new Dictionary<Guid, Operation>();
        Task<object> IInterfaceImplementation.CallAsync(MethodInfo method, object[] arguments)
        {
            Type expectedReturnType = null;
            if (typeof(Task).IsAssignableFrom(method.ReturnType))
            {
                if (method.ReturnType.IsGenericType)
                    expectedReturnType = method.ReturnType.GetGenericArguments()[0];
            }
            else
                expectedReturnType = method.ReturnType;

            var id = Guid.NewGuid();
            var op = operations[id] = new Operation
            {
                Source = new TaskCompletionSource<object>(),
                ExpectedReturnType = expectedReturnType,
            };

            var payload = new object[]
            {
                "frida:rpc",
                id,
                "call",
                method.Name,
                arguments,
            };
            Post(payload);

            return op.Source.Task;
        }

        class FridaRpcMessage
        {
            public string type { get; set; }
            public string[] payload { get; set; }
        }

        protected override void ProcessMessage(string type, JObject data, byte[] dataBytes)
        {
            switch(type)
            {
                case "send":
                case "error":
                    JToken payload;
                    if(data.TryGetValue("payload", out payload) && payload is JArray args)
                    {
                        Guid callId;
                        if (args.Count >= 3
                            && args[0].Type == JTokenType.String
                            && args[1].Type == JTokenType.String
                            && args[2].Type == JTokenType.String
                            && args[0].Value<string>() == "frida:rpc"
                            && Guid.TryParse(args[1].Value<string>(), out callId))
                        {

                            Operation operation;
                            if (!operations.TryGetValue(callId, out operation))
                                return;

                            var opResult = args[2].Value<string>();
                            switch (opResult)
                            {
                                case "ok":
                                    try
                                    {
                                        object retValue = null;
                                        if (operation.ExpectedReturnType != null)
                                            retValue = dataBytes != null && operation.ExpectedReturnType == typeof(byte[])
                                                ? dataBytes
                                                : args[3].ToObject(operation.ExpectedReturnType);
                                        operation.Source.SetResult(retValue);
                                    }
                                    catch (Exception ex)
                                    {
                                        operation.Source.SetException(new FridaRpcSerializationException(ex));
                                    }
                                    break;
                                case "error":
                                    var errMessage = args[3].Value<string>();
                                    var errName = args.Count > 4 ? args[4].Value<string>() : null;
                                    var errStack = args.Count > 5 ? args[5].Value<string>() : null;
                                    operation.Source.SetException(new FridaRpcScriptException(errMessage, errName, errStack));
                                    break;
                                default:
                                    break;
                            }
                            operations.Remove(callId);

                            return;
                        }
                    }
                    break;
                default:
                    break;
            }
            base.ProcessMessage(type, data, dataBytes);
        }
    }
}
