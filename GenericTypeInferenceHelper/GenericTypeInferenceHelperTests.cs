using System;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;

namespace GenericTypeInferenceHelper
{
    [TestFixture]
    public class GenericTypeInferenceHelperTests
    {
        private static readonly TestCaseData[] TestCasesThatShouldWorkExactlyLikeTheCompiler =
        {
            new TestCaseData(
                GetMethodInfo(() => SimpleGenericMethod(0)), 
                new[] {typeof (int)}, 
                new[] {typeof (int)}
            ),
            new TestCaseData(
                GetMethodInfo(() => SimpleGenericMethod(0)), 
                new[] {typeof (long)},
                new[] {typeof (long)}
            ),
            new TestCaseData(
                GetMethodInfo(() => SimpleGenericMethod(0)),
                new[] {typeof (decimal)},
                new[] {typeof (decimal)}
            ),
            new TestCaseData(
                GetMethodInfo(() => SimpleGenericMethod(0)), 
                new[] {typeof (float)},
                new[] {typeof (float)}
            ),
            new TestCaseData(
                GetMethodInfo(() => SimpleGenericMethod(0)), 
                new[] {typeof (double)},
                new[] {typeof (double)}
            ),
            new TestCaseData(
                GetMethodInfo(() => SimpleGenericMethod(0)), 
                new[] {typeof (object)},
                new[] {typeof (object)}
            ),
            new TestCaseData(
                GetMethodInfo(() => SimpleGenericMethod(0)),
                new[] {typeof (string)},
                new[] {typeof (string)}
            ),
            new TestCaseData(
                GetMethodInfo(() => SimpleGenericMethodWith2Args(0, 0)),
                new[] {typeof (int), typeof (int)},
                new[] {typeof (int)}
            ),
            new TestCaseData(
                GetMethodInfo(() => SimpleGenericMethodWith2Args(0, 0)),
                new[] {typeof (int), typeof (long)},
                new[] {typeof (long)}
            ),
            new TestCaseData(
                GetMethodInfo(() => SimpleGenericMethodWith2Args(0, 0)),
                new[] {typeof (long), typeof (float)},
                new[] {typeof (float)}
            ),
            new TestCaseData(
                GetMethodInfo(() => SimpleGenericMethodWith2Args(0, 0)),
                new[] {typeof (long), typeof (double)},
                new[] {typeof (double)}
            ),
            new TestCaseData(
                GetMethodInfo(() => SimpleGenericMethodWith2Args(0, 0)),
                new[] {typeof (long), typeof (decimal)},
                new[] {typeof (decimal)}
            ),
            new TestCaseData(
                GetMethodInfo(() => SimpleGenericMethodWith2Args(0, 0)),
                new[] {typeof (double), typeof (decimal)},
                new[] {typeof (decimal)}
            ),
            new TestCaseData(
                GetMethodInfo(() => SimpleGenericMethodWith2Args(0, 0)),
                new[] {typeof (Cat), typeof (Mouse)},
                new[] {typeof (Animal)}
            ),
            //new[] { GetMethodInfo(() => GenericMethodWithNullableArgument((int?)0) }, 
            //new[] { GetMethodInfo(() => GenericMethodWithGenericArgument(new Generic<Human>()) }, 
        };
        
        [Test]
        [TestCaseSource("TestCasesThatShouldWorkExactlyLikeTheCompiler")]
        public void InferTypeArguments(MethodInfo methodInfo, Type[] argumentTypes, Type[] expectedTypeArguments)
        {
            var inferredMethod =
                methodInfo.GetGenericMethodDefinition()
                          .InferTypeArguments()
                          .FromArgumentTypes(argumentTypes);

            Type[] actualTypeArguments = inferredMethod.GetGenericArguments();

            Assert.That(actualTypeArguments, Is.EqualTo(expectedTypeArguments));
        }

        private static object GetMethodInfo(Expression<Action> func)
        {
            return ((MethodCallExpression) func.Body).Method;
        }


        public static void SimpleGenericMethod<TType>(TType arg)
        {
        }

        private static void SimpleGenericMethodWith2Args<TType>(TType arg1, TType arg2)
        {
        }

        private static void GenericMethodWithNullableArgument<TType>(TType? arg)
            where TType : struct
        {
        }

        private static void GenericMethodWithGenericArgument<TType>(Generic<TType> arg)
        {
        }

        private class Human
        {
        }

        private class Generic<TType>
        {
            
        }

        private class Animal
        {
        }

        private class Cat : Animal
        {
        }

        private class Mouse : Animal
        {
        }
    }
}

