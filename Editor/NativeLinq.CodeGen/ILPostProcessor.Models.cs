using System.Collections.Generic;
using Mono.Cecil;
    
namespace KrasCore.NativeLinq.CodeGen
{
    internal sealed partial class ILPostProcessor
    {
        private sealed class AdapterInfo
        {
            public AdapterInfo(TypeReference adapterType, TypeReference interfaceType)
            {
                AdapterType = adapterType;
                InterfaceType = interfaceType;
            }

            public TypeReference AdapterType { get; }

            public TypeReference InterfaceType { get; }
        }

        private sealed class DelegateSignature
        {
            public DelegateSignature(IReadOnlyList<TypeReference> parameterTypes, TypeReference returnType)
            {
                ParameterTypes = parameterTypes;
                ReturnType = returnType;
            }

            public IReadOnlyList<TypeReference> ParameterTypes { get; }

            public TypeReference ReturnType { get; }
        }

        private sealed class TargetMethodInfo
        {
            public TargetMethodInfo(MethodReference call, TypeReference returnType)
            {
                Call = call;
                ReturnType = returnType;
            }

            public MethodReference Call { get; }

            public TypeReference ReturnType { get; }
        }
    }
}
