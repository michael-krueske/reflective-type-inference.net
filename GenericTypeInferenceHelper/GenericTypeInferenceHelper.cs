using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GenericTypeInferenceHelper
{
    /// <summary>
    /// Utilities for generic type inference.
    /// </summary>
    public static class GenericTypeInferenceHelper
    {
        public static ITypeArgumentInferer InferTypeArguments(this MethodInfo methodDefinition)
        {
            if (methodDefinition == null) 
                throw new ArgumentNullException("methodDefinition");

            return new TypeArgumentInferer(methodDefinition);
        }

        public interface ITypeArgumentInferer
        {
            MethodInfo FromActualArguments(params object[] actualArguments);
            MethodInfo FromActualArguments(IList<object> actualArguments);

            MethodInfo FromArgumentTypes(params Type[] argumentTypes);
            MethodInfo FromArgumentTypes(IList<Type> argumentTypes);
        }

        internal class TypeArgumentInferer : ITypeArgumentInferer
        {
            private readonly MethodInfo _genericMethodDefinition;

            public TypeArgumentInferer(MethodInfo genericMethodDefinition)
            {
                if (genericMethodDefinition == null)
                    throw new ArgumentNullException("genericMethodDefinition");
                if (!genericMethodDefinition.IsGenericMethodDefinition)
                    throw new ArgumentException("The parameter must be a generic method definition.", "genericMethodDefinition");

                _genericMethodDefinition = genericMethodDefinition;
            }

            public MethodInfo FromActualArguments(params object[] actualArguments)
            {
                return FromActualArguments((IList<object>)actualArguments);
            }

            public MethodInfo FromActualArguments(IList<object> actualArguments)
            {
                var argumentTypes = actualArguments.Select(x => x.GetType()).ToArray();
                return FromArgumentTypes(argumentTypes);
            }

            public MethodInfo FromArgumentTypes(params Type[] argumentTypes)
            {
                return FromArgumentTypes((IList<Type>)argumentTypes);
            }

            public MethodInfo FromArgumentTypes(IList<Type> argumentTypes)
            {
                if (argumentTypes == null)
                    throw new ArgumentNullException("argumentTypes");

                if (!_genericMethodDefinition.IsGenericMethod && !_genericMethodDefinition.IsGenericMethodDefinition)
                    return _genericMethodDefinition;

                var unboundArgumentTypes =
                    _genericMethodDefinition.GetParameters()
                            .Select(param => param.ParameterType);

                // make the called method return a single value or null
                var inferredArgumentsList =
                    (from typeArgument in _genericMethodDefinition.GetGenericArguments()
                     select InferPossibleTypeArguments(typeArgument, unboundArgumentTypes, argumentTypes)
                     ).ToList();

                if (inferredArgumentsList.Any(x => x.Count != 1))
                {
                    throw new InvalidOperationException(
                            string.Format("The method's type arguments cannot be inferred: method is {0}, the arguments are [ {1} ]",
                                    _genericMethodDefinition, string.Join(", ", argumentTypes)));
                }

                var typeArguments = inferredArgumentsList.Select(x => x.Single()).ToArray();
                return _genericMethodDefinition.MakeGenericMethod(typeArguments);
            }
        }

        private static IList<Type> InferPossibleTypeArguments(Type typeArgument, IEnumerable<Type> unboundArgumentTypes, IList<Type> boundArgumentTypes)
        {
            var result =
                unboundArgumentTypes.FindTypeArgumentPaths(typeArgument)
                                .Select(path => path.Evaluate(boundArgumentTypes))
                                .Distinct()
                                .ToList();
            return result;
        }

        private static IEnumerable<GenericArgumentPathInParameters> FindTypeArgumentPaths(this IEnumerable<Type> unboundParamTypes, Type typeArgument)
        {
            return unboundParamTypes.SelectMany(
                (param, index) =>
                    param.FindTypeArgumentPaths(typeArgument)
                         .Select(path => new GenericArgumentPathInParameters(index, path)));
        }

        /// <summary>
        /// Find all paths to the usages of the given <paramref name="typeArgument"/> in the given <paramref name="unboundArgument"/>.
        /// </summary>
        /// <param name="unboundArgument">the generic type</param>
        /// <param name="typeArgument">the type to find</param>
        /// <returns></returns>
        private static IEnumerable<GenericArgumentPathInType> FindTypeArgumentPaths(this Type unboundArgument, Type typeArgument)
        {
            if (unboundArgument == typeArgument)
            {
                return new[] { new GenericArgumentPathInType(typeArgument) };
            }

            if (!unboundArgument.IsGenericType)
            {
                return Enumerable.Empty<GenericArgumentPathInType>();
            }

            // find all paths recursively
            return unboundArgument.GetGenericArguments()
                              .SelectMany(
                                  (paramType, index) =>
                                  paramType.FindTypeArgumentPaths(typeArgument)
                                              .Select(subPath => subPath.WithPrefix(unboundArgument, index)));
        }

        /// <summary>
        /// Path of a type argument in a list of generic parameter types.
        /// </summary>
        private class GenericArgumentPathInParameters
        {
            private readonly int _paramIndex;
            private readonly GenericArgumentPathInType _argumentPathInType;

            internal GenericArgumentPathInParameters(int paramIndex, GenericArgumentPathInType argumentPathInType)
            {
                _paramIndex = paramIndex;
                _argumentPathInType = argumentPathInType;
            }

            internal Type Evaluate(IList<Type> parameterTypes)
            {
                var parameterType = parameterTypes[_paramIndex];
                return _argumentPathInType.Evaluate(parameterType);
            }

            public override string ToString()
            {
                return String.Format("{{ ParamIndex: {0}, TypePath: {1} }}", _paramIndex, _argumentPathInType);
            }
        }

        /// <summary>
        /// Path of a type argument in a generic type.
        /// </summary>
        internal class GenericArgumentPathInType
        {
            private readonly Type _typeArgument;
            private readonly int[] _path;

            internal GenericArgumentPathInType(Type typeArgument, params int[] path)
            {
                if (typeArgument == null) throw new ArgumentNullException("typeArgument");
                if (path == null) throw new ArgumentNullException("path");

                _typeArgument = typeArgument;
                _path = path;
            }

            internal GenericArgumentPathInType WithPrefix(Type unboundArgument, int i)
            {
                return new GenericArgumentPathInType(unboundArgument, new[] { i }.Concat(_path).ToArray());
            }

            internal Type Evaluate(Type boundParamType)
            {
                // get the ancestor of the bound param type matching the constraints
                var ancestor = boundParamType.GetAncestor(_typeArgument.GetUnbound());

                if (ancestor == null)
                {
                    throw new InvalidOperationException(
                            string.Format("The type '{0}' has no ancestor matching '{1}'.", boundParamType, _typeArgument));
                }

                if (_path.Length == 0)
                {
                    return ancestor;
                }

                if (!ancestor.IsGenericType)
                    throw new InvalidOperationException("The type must be generic if the path's length is > 0.");

                // apply the path's indices successively to the type's arguments
                return _path.Aggregate(ancestor,
                                       (current, index) => current.GetGenericArguments()[index]);
            }

            public override string ToString()
            {
                return string.Format("{0} [{1}]", _typeArgument, string.Join(", ", _path));
            }
        }
    }
}
