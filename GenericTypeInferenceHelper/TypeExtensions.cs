using System;
using System.Collections.Generic;
using System.Linq;

namespace GenericTypeInferenceHelper
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Get the ancestor type of the given <paramref name="type"/> that matches the given <paramref name="ancestorType"/>.
        /// The <paramref name="ancestorType"/> may be a concrety type or a generic type definiton.
        /// </summary>
        /// <param name="type">the type</param>
        /// <param name="ancestorType">the ancestor type</param>
        /// <returns>the ancestor type, if existing or <code>null</code></returns>
        public static Type GetAncestor(this Type type, Type ancestorType)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (ancestorType == null) throw new ArgumentNullException("ancestorType");

            if (ancestorType.IsGenericParameter)
            {
                return type;
            }

            var isMatchingTypePredicate = MakeIsMatchingTypePredicate(ancestorType);
            var candidates = new[] { type }.Concat(type.GetBaseTypes()).Concat(type.GetInterfaces());
            return candidates.SingleOrDefault(isMatchingTypePredicate);
        }

        private static Func<Type, bool> MakeIsMatchingTypePredicate(Type ancestorType)
        {
            if (!ancestorType.IsGenericTypeDefinition)
            {
                return t => t == ancestorType;
            }

            return t => t.IsGenericType && t.GetGenericTypeDefinition() == ancestorType;
        }

        /// <summary>
        /// Get the base types of the given  <paramref name="type"/>.
        /// </summary>
        /// <param name="type">the type</param>
        /// <returns>the base types</returns>
        public static IEnumerable<Type> GetBaseTypes(this Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            var current = type.BaseType;

            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static Type GetUnbound(this Type type)
        {
            return !type.IsGenericType
                ? type
                : type.GetGenericTypeDefinition();
        }


    }
}