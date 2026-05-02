using System;
using Mono.Cecil;

namespace KrasCore.NativeLinq.CodeGen
{
    internal sealed partial class ILPostProcessor
    {
        private static TypeReference RewriteTypeReference(
            TypeReference type,
            Func<GenericParameter, TypeReference> resolveGenericParameter,
            Func<TypeReference, TypeReference> mapReference)
        {
            if (type is GenericParameter genericParameter)
            {
                var resolved = resolveGenericParameter(genericParameter);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            switch (type)
            {
                case GenericInstanceType genericInstance:
                    var closedInstance = new GenericInstanceType(mapReference(genericInstance.ElementType));
                    foreach (var argument in genericInstance.GenericArguments)
                    {
                        closedInstance.GenericArguments.Add(RewriteTypeReference(argument, resolveGenericParameter, mapReference));
                    }

                    return closedInstance;
                case ByReferenceType byReference:
                    return new ByReferenceType(RewriteTypeReference(byReference.ElementType, resolveGenericParameter, mapReference));
                case PointerType pointer:
                    return new PointerType(RewriteTypeReference(pointer.ElementType, resolveGenericParameter, mapReference));
                case RequiredModifierType requiredModifier:
                    return new RequiredModifierType(
                        mapReference(requiredModifier.ModifierType),
                        RewriteTypeReference(requiredModifier.ElementType, resolveGenericParameter, mapReference));
                case OptionalModifierType optionalModifier:
                    return new OptionalModifierType(
                        mapReference(optionalModifier.ModifierType),
                        RewriteTypeReference(optionalModifier.ElementType, resolveGenericParameter, mapReference));
                default:
                    return mapReference(type);
            }
        }
    }
}
