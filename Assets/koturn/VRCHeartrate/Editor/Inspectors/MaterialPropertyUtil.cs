using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
using UnityEngine;


namespace Koturn.VRCHeartrate.Inspectors
{
    /// <summary>
    /// Provides utility methods for <see cref="MaterialProperty"/>.
    /// </summary>
    public static class MaterialPropertyUtil
    {
        /// <summary>
        /// Cache of compiled reflection result of <see cref="CreateClearPropertyHandlerCacheAction"/>.
        /// </summary>
        private static Action<List<string>> _clearPropertyHandlerCache;
        /// <summary>
        /// Cache of compiled reflection result of <see cref="CreateClearDecoratorDrawersFunc"/>.
        /// </summary>
        private static Func<Shader, string, string> _clearDecoratorDrawers;
        /// <summary>
        /// Cache of compiled reflection result of <see cref="CreateClearCustomDrawersFunc"/>.
        /// </summary>
        private static Func<Shader, string, string> _clearCustomDrawers;


        /// <summary>
        /// Clear cache of MaterialPropertyHandler.
        /// </summary>
        public static void ClearPropertyHandlerCache(List<string> propStringList)
        {
            (_clearPropertyHandlerCache ?? (_clearPropertyHandlerCache = CreateClearPropertyHandlerCacheAction()))(propStringList);
        }

        /// <summary>
        /// Remove all decorator drawers from all MaterialProperties.
        /// </summary>
        /// <param name="shader">Target <see cref="Shader"/>.</param>
        /// <param name="mps">Target <see cref="MaterialProperty"/> array.</param>
        /// <returns>Property string list of <see cref="MaterialProperty"/> whose decorator drawers are removed.</returns>
        public static List<string> ClearDecoratorDrawers(Shader shader, MaterialProperty[] mps)
        {
            var clearDecoratorDrawers = _clearDecoratorDrawers ?? (_clearDecoratorDrawers = CreateClearDecoratorDrawersFunc());

            var propStringList = new List<string>();
            foreach (var mp in mps)
            {
                var propString = clearDecoratorDrawers(shader, mp.name);
                if (propString != null)
                {
                    propStringList.Add(propString);
                }
            }

            return propStringList;
        }

        /// <summary>
        /// Remove all custom property drawer and decorator drawers from all MaterialProperties.
        /// </summary>
        /// <param name="shader">Target <see cref="Shader"/>.</param>
        /// <param name="mps">Target <see cref="MaterialProperty"/> array.</param>
        /// <returns>Property string list of <see cref="MaterialProperty"/> whose decorator drawers are removed.</returns>
        public static List<string> ClearCustomDrawers(Shader shader, MaterialProperty[] mps)
        {
            var clearCustomDrawers = _clearCustomDrawers ?? (_clearCustomDrawers = CreateClearCustomDrawersFunc());

            var propStringList = new List<string>();
            foreach (var mp in mps)
            {
                var propString = clearCustomDrawers(shader, mp.name);
                if (propString != null)
                {
                    propStringList.Add(propString);
                }
            }

            return propStringList;
        }


        /// <summary>
        /// <para>Create delegate of compiled reflection result which clears cache of MaterialPropertyHandler.</para>
        /// <para>Psudo-code of result lambda is following.</para>
        /// <code>
        /// (List&lt;string&gt; keyList) =>
        /// {
        ///     Dictionary&lt;string, MaterialPropertyHandler&gt; propertyHandlers = MaterialPropertyHandler.s_PropertyHandlers;
        ///     int count = keyList.Count;
        ///     for (int i = 0; i &lt; count; i++)
        ///     {
        ///         propertyHandlers.Remove(keyList[i]);
        ///     }
        /// };
        /// </code>
        /// </summary>
        /// <returns>Compiled reflection result which removes MaterialPropertyHandlers from the cache Dictionary.</returns>
        private static Action<List<string>> CreateClearPropertyHandlerCacheAction()
        {
            // Types
            var tMaterialPropertyHandler = GetTypeStrict(Assembly.GetAssembly(typeof(MaterialPropertyDrawer)), "UnityEditor.MaterialPropertyHandler");
            var tDictionary = GetTypeStrict(Assembly.GetAssembly(typeof(object)), "System.Collections.Generic.Dictionary`2")
                .MakeGenericType(typeof(string), tMaterialPropertyHandler);

            // Arguments
            var pList = Expression.Parameter(typeof(List<string>), "list");

            // Local variables
            var pDict = Expression.Parameter(tDictionary, "propertyHandlers");
            var pCount = Expression.Parameter(typeof(int), "count");
            var pIndex = Expression.Parameter(typeof(int), "i");

            // Labels
            var labelLoopEnd = Expression.Label();

            return Expression.Lambda<Action<List<string>>>(
                Expression.Block(
                    new []
                    {
                        pDict,
                        pCount,
                        pIndex
                    },
                    Expression.Assign(
                        pDict,
                        Expression.Field(
                            null,
                            GetFieldStrict(
                                tMaterialPropertyHandler,
                                "s_PropertyHandlers",
                                BindingFlags.NonPublic | BindingFlags.Static))),
                    Expression.Assign(
                        pCount,
                        Expression.Property(
                            pList,
                            GetPropertyStrict(
                                typeof(List<string>),
                                "Count",
                                BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance))),
                    Expression.Loop(
                        Expression.IfThenElse(
                            Expression.LessThan(
                                pIndex,
                                pCount),
                            Expression.Block(
                                Expression.Call(
                                    pDict,
                                    GetMethodStrict(
                                        tDictionary,
                                        "Remove",
                                        new []
                                        {
                                            typeof(string)
                                        }),
                                    Expression.Property(
                                        pList,
                                        typeof(List<string>).GetProperty(
                                            typeof(List<string>)
                                                .GetCustomAttribute<DefaultMemberAttribute>()
                                                .MemberName,
                                            new []
                                            {
                                                typeof(int)
                                            }),
                                        new []
                                        {
                                            pIndex
                                        })),
                                Expression.PostIncrementAssign(pIndex)),
                            Expression.Break(labelLoopEnd)),
                        labelLoopEnd)),
                "ClearPropertyHandlerCache",
                new[]
                {
                    pList
                }).Compile();
        }

        /// <summary>
        /// <para>Create delegate of compiled reflection result which clears decorator drawers from <see cref="MaterialProperty"/>.</para>
        /// <para>Psudo-code of result lambda is following.</para>
        /// <code>
        /// (Shader shader, string propName) =>
        /// {
        ///     MaterialPropertyHandler mph = UnityEditor.MaterialPropertyHandler.GetHandler(shader, propName);
        ///     if (mph is null)
        ///     {
        ///         return null;
        ///     }
        ///     List&lt;MaterialPropertyDrawers&gt; decoratorDrawers = mph.m_DecoratorDrawers;
        ///     string propString = null;
        ///     if (decoratorDrawers is not null)
        ///     {
        ///         decoratorDrawers.Clear();
        ///         propString = MaterialPropertyDrawers.GetPropertyString(shader, propName);
        ///     }
        ///
        ///     return propString;
        /// };
        /// </code>
        /// </summary>
        /// <returns>Compiled reflection result which removes decorator drawers from MaterialPropertyHandlers.</returns>
        private static Func<Shader, string, string> CreateClearDecoratorDrawersFunc()
        {
            // Get type of UnityEditor.MaterialPropertyHandler which is the internal class.
            var tMaterialPropertyHandler = GetTypeStrict(Assembly.GetAssembly(typeof(MaterialPropertyDrawer)), "UnityEditor.MaterialPropertyHandler");

            // Constants
            var cNull = Expression.Constant(null);

            // Arguments
            var pShader = Expression.Parameter(typeof(Shader), "shader");
            var pPropName = Expression.Parameter(typeof(string), "propName");

            // Local variables
            var pMaterialPropertyHandler = Expression.Parameter(tMaterialPropertyHandler, "mph");
            var pDecoratorDrawers = Expression.Parameter(typeof(List<MaterialPropertyDrawer>), "decoratorDrawers");
            var pPropString = Expression.Parameter(typeof(string), "propString");

            // Labels
            var returnTarget = Expression.Label();

            return Expression.Lambda<Func<Shader, string, string>>(
                Expression.Block(
                    new[]
                    {
                        pMaterialPropertyHandler,
                        pDecoratorDrawers,
                        pPropString
                    },
                    Expression.Assign(
                        pMaterialPropertyHandler,
                        Expression.Call(
                            GetMethodStrict(
                                tMaterialPropertyHandler,
                                "GetHandler",
                                BindingFlags.NonPublic | BindingFlags.Static),
                            pShader,
                            pPropName)),
                    Expression.IfThen(
                        Expression.Equal(
                            pMaterialPropertyHandler,
                            cNull),
                        Expression.Return(returnTarget)),
                    Expression.Assign(
                        pDecoratorDrawers,
                        Expression.Field(
                            pMaterialPropertyHandler,
                            GetFieldStrict(
                                tMaterialPropertyHandler,
                                "m_DecoratorDrawers",
                                BindingFlags.NonPublic | BindingFlags.Instance))),
                    Expression.IfThen(
                        Expression.NotEqual(
                            pDecoratorDrawers,
                            cNull),
                        Expression.Block(
                            Expression.Call(
                                pDecoratorDrawers,
                                GetMethodStrict(
                                    typeof(List<MaterialPropertyDrawer>),
                                    "Clear",
                                    BindingFlags.Public | BindingFlags.Instance)),
                            Expression.Assign(
                                pPropString,
                                Expression.Call(
                                    GetMethodStrict(
                                        tMaterialPropertyHandler,
                                        "GetPropertyString",
                                        BindingFlags.NonPublic | BindingFlags.Static),
                                    pShader,
                                    pPropName)))),
                    Expression.Label(returnTarget),
                    pPropString),
                "ClearDecoratorDrawers",
                new []
                {
                    pShader,
                    pPropName
                }).Compile();
        }

        /// <summary>
        /// <para>Create delegate of compiled reflection result which clears decorator drawers from <see cref="MaterialProperty"/>.</para>
        /// <para>Psudo-code of result lambda is following.</para>
        /// <code>
        /// (Shader shader, string propName) =>
        /// {
        ///     MaterialPropertyHandler mph = UnityEditor.MaterialPropertyHandler.GetHandler(shader, propName);
        ///     if (mph is null)
        ///     {
        ///         return null;
        ///     }
        ///
        ///     bool isChanged = false;
        ///
        ///     MaterialPropertyDrawer propertyDrawer = handler.propertyDrawer;
        ///     if (!(propertyDrawer is MaterialToggleUIDrawer
        ///         || propertyDrawer is MaterialPowerSliderDrawer
        ///         || propertyDrawer is MaterialIntRangeDrawer
        ///         || propertyDrawer is MaterialEnumDrawer
        ///         || propertyDrawer is MaterialKeywordEnumDrawer))
        ///     {
        ///         handler.m_PropertyDrawer = null;
        ///         isChanged = true;
        ///     }
        ///
        ///     List&lt;MaterialPropertyDrawer&gt; decoratorDrawers = mph.m_DecoratorDrawers;
        ///     if (decoratorDrawers is not null)
        ///     {
        ///         for (int i = count - 1; i >= 0; i--)
        ///         {
        ///             propertyDrawer = decoratorDrawers[i];
        ///             if (!(propertyDrawer is MaterialSpaceDecorator
        ///                 || propertyDrawer is MaterialHeaderDecorator))
        ///             {
        ///                 decoratorDrawers.RemoveAt(i);
        ///                 isChanged = true;
        ///             }
        ///         }
        ///     }
        ///
        ///     return isChanged ? MaterialPropertyDrawers.GetPropertyString(shader, propName) : null;
        /// };
        /// </code>
        /// </summary>
        /// <returns>Compiled reflection result which removes decorator drawers from MaterialPropertyHandlers.</returns>
        private static Func<Shader, string, string> CreateClearCustomDrawersFunc()
        {
            var asm = Assembly.GetAssembly(typeof(MaterialPropertyDrawer));

            // Get type of UnityEditor.MaterialPropertyHandler which is the internal class.
            var tMaterialPropertyHandler = GetTypeStrict(asm, "UnityEditor.MaterialPropertyHandler");

            // Constants
            var cNull = Expression.Constant(null);
            var cTrue = Expression.Constant(true);

            // Arguments
            var pShader = Expression.Parameter(typeof(Shader), "shader");
            var pPropName = Expression.Parameter(typeof(string), "propName");

            // Local variables
            var pIndex = Expression.Parameter(typeof(int), "i");
            var pMaterialPropertyHandler = Expression.Parameter(tMaterialPropertyHandler, "mph");
            var pDecoratorDrawers = Expression.Parameter(typeof(List<MaterialPropertyDrawer>), "decoratorDrawers");
            var pPropertyDrawer = Expression.Parameter(typeof(MaterialPropertyDrawer), "propertyDrawer");
            var pIsChanged = Expression.Parameter(typeof(bool), "isChanged");

            // Labels
            var labelLoopEnd = Expression.Label();
            var labelReturn = Expression.Label();

            return Expression.Lambda<Func<Shader, string, string>>(
                Expression.Block(
                    new[]
                    {
                        pIndex,
                        pMaterialPropertyHandler,
                        pDecoratorDrawers,
                        pPropertyDrawer,
                        pIsChanged
                    },
                    Expression.Assign(
                        pMaterialPropertyHandler,
                        Expression.Call(
                            GetMethodStrict(
                                tMaterialPropertyHandler,
                                "GetHandler",
                                BindingFlags.NonPublic | BindingFlags.Static),
                            pShader,
                            pPropName)),
                    Expression.IfThen(
                        Expression.Equal(
                            pMaterialPropertyHandler,
                            cNull),
                        Expression.Return(labelReturn)),
                    Expression.Assign(
                        pPropertyDrawer,
                        Expression.Property(
                            pMaterialPropertyHandler,
                            GetPropertyStrict(
                                tMaterialPropertyHandler,
                                "propertyDrawer",
                                BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance))),
                    Expression.IfThen(
                        Expression.Not(
                            Expression.OrElse(
                                Expression.TypeIs(
                                    pPropertyDrawer,
                                    GetTypeStrict(asm, "UnityEditor.MaterialToggleUIDrawer")),
                                Expression.OrElse(
                                    Expression.TypeIs(
                                        pPropertyDrawer,
                                        GetTypeStrict(asm, "UnityEditor.MaterialPowerSliderDrawer")),
                                    Expression.OrElse(
                                        Expression.TypeIs(
                                            pPropertyDrawer,
                                            GetTypeStrict(asm, "UnityEditor.MaterialIntRangeDrawer")),
                                        Expression.OrElse(
                                            Expression.TypeIs(
                                                pPropertyDrawer,
                                                GetTypeStrict(asm, "UnityEditor.MaterialEnumDrawer")),
                                            Expression.TypeIs(
                                                pPropertyDrawer,
                                                GetTypeStrict(asm, "UnityEditor.MaterialKeywordEnumDrawer"))))))),
                        Expression.Block(
                            Expression.Assign(
                                Expression.Field(
                                    pMaterialPropertyHandler,
                                    GetFieldStrict(
                                        tMaterialPropertyHandler,
                                        "m_PropertyDrawer",
                                        BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Instance)),
                                Expression.Constant(null, typeof(MaterialPropertyDrawer))),
                            Expression.Assign(
                                pIsChanged,
                                cTrue))),
                    Expression.Assign(
                        pDecoratorDrawers,
                        Expression.Field(
                            pMaterialPropertyHandler,
                            GetFieldStrict(
                                tMaterialPropertyHandler,
                                "m_DecoratorDrawers",
                                BindingFlags.NonPublic | BindingFlags.Instance))),
                    Expression.IfThen(
                        Expression.NotEqual(
                            pDecoratorDrawers,
                            cNull),
                        Expression.Block(
                            Expression.Assign(
                                pIndex,
                                Expression.Decrement(
                                    Expression.Property(
                                        pDecoratorDrawers,
                                        GetPropertyStrict(
                                            typeof(List<MaterialPropertyDrawer>),
                                            "Count",
                                            BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance)))),
                            Expression.Loop(
                                Expression.IfThenElse(
                                    Expression.LessThan(
                                        pIndex,
                                        Expression.Constant(0)),
                                    Expression.Break(labelLoopEnd),
                                    Expression.Block(
                                        Expression.Assign(
                                            pPropertyDrawer,
                                            Expression.Property(
                                                pDecoratorDrawers,
                                                typeof(List<MaterialPropertyDrawer>).GetProperty(
                                                    typeof(List<MaterialPropertyDrawer>)
                                                        .GetCustomAttribute<DefaultMemberAttribute>()
                                                        .MemberName,
                                                    new []
                                                    {
                                                        typeof(int)
                                                    }),
                                                new []
                                                {
                                                    pIndex
                                                })),
                                        Expression.IfThen(
                                            Expression.Not(
                                                Expression.OrElse(
                                                    Expression.TypeIs(
                                                        pPropertyDrawer,
                                                        GetTypeStrict(asm, "UnityEditor.MaterialSpaceDecorator")),
                                                    Expression.TypeIs(
                                                        pPropertyDrawer,
                                                        GetTypeStrict(asm, "UnityEditor.MaterialHeaderDecorator")))),
                                            Expression.Block(
                                                Expression.Call(
                                                    pDecoratorDrawers,
                                                    GetMethodStrict(
                                                        typeof(List<MaterialPropertyDrawer>),
                                                        "RemoveAt",
                                                        new []
                                                        {
                                                            typeof(int)
                                                        }),
                                                        pIndex),
                                                Expression.Assign(
                                                    pIsChanged,
                                                    cTrue))),
                                        Expression.PostDecrementAssign(pIndex))),
                                labelLoopEnd))),
                    Expression.Label(labelReturn),
                    Expression.Condition(
                        pIsChanged,
                        Expression.Call(
                            GetMethodStrict(
                                tMaterialPropertyHandler,
                                "GetPropertyString",
                                BindingFlags.NonPublic | BindingFlags.Static),
                            pShader,
                            pPropName),
                        Expression.Constant(null, typeof(string)))),
                "ClearCustomDrawers",
                new []
                {
                    pShader,
                    pPropName
                }).Compile();
        }

        /// <summary>
        /// Try to get <see cref="Type"/>.
        /// </summary>
        /// <param name="asm"><see cref="Assembly"/> to serach.</param>
        /// <param name="typeName">Full name of <see cref="Type"/>.</param>
        /// <exception cref="ArgumentException">Throw when specified type is not found.</exception>
        private static Type GetTypeStrict(Assembly asm, string typeName)
        {
            return asm.GetType(typeName)
                ?? throw new ArgumentException("Type not found: " + typeName);
        }

        /// <summary>
        /// Try to get <see cref="MethodInfo"/>.
        /// </summary>
        /// <param name="type"><see cref="Type"/> to search.</param>
        /// <param name="methodName">Name of method.</param>
        /// <param name="flags">TODO</param>
        /// <exception cref="ArgumentException">Throw when specified method is not found.</exception>
        private static MethodInfo GetMethodStrict(Type type, string methodName, BindingFlags flags)
        {
            return type.GetMethod(methodName, flags)
                ?? throw new ArgumentException("MethodInfo not found: " + type.FullName + "." + methodName);
        }

        /// <summary>
        /// Try to get <see cref="MethodInfo"/>.
        /// </summary>
        /// <param name="type"><see cref="Type"/> to search.</param>
        /// <param name="methodName">Name of method.</param>
        /// <param name="argTypes">Type array of arguments.</param>
        /// <exception cref="ArgumentException">Throw when specified method is not found.</exception>
        private static MethodInfo GetMethodStrict(Type type, string methodName, Type[] argTypes)
        {
            return type.GetMethod(methodName, argTypes)
                ?? throw new ArgumentException("MethodInfo not found: " + type.FullName + "." + methodName);
        }

        /// <summary>
        /// Try to get <see cref="FieldInfo"/>.
        /// </summary>
        /// <param name="type"><see cref="Type"/> to search.</param>
        /// <param name="fieldName">Name of field.</param>
        /// <param name="flags">TODO</param>
        /// <exception cref="ArgumentException">Throw when specified field is not found.</exception>
        private static FieldInfo GetFieldStrict(Type type, string fieldName, BindingFlags flags)
        {
            return type.GetField(fieldName, flags)
                ?? throw new ArgumentException("FieldInfo not found: " + type.FullName + "." + fieldName);
        }

        /// <summary>
        /// Try to get <see cref="PropertyInfo"/>.
        /// </summary>
        /// <param name="type"><see cref="Type"/> to search.</param>
        /// <param name="propertyName">Name of property.</param>
        /// <param name="flags">TODO</param>
        /// <exception cref="ArgumentException">Throw when specified property is not found.</exception>
        private static PropertyInfo GetPropertyStrict(Type type, string propertyName, BindingFlags flags)
        {
            return type.GetProperty(propertyName, flags)
                ?? throw new ArgumentException("PropertyInfo not found: " + type.FullName + "." + propertyName);
        }
    }
}
