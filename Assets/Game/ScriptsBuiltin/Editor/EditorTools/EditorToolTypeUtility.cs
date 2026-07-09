using GameFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UGF.EditorTools
{
    internal static class EditorToolTypeUtility
    {
        public static List<Type> FindTypes(Func<Type, bool> predicate)
        {
            var result = new List<Type>();
            foreach (Assembly assembly in Utility.Assembly.GetAssemblies())
            {
                foreach (Type type in GetTypesSafe(assembly))
                {
                    if (type != null && predicate(type))
                    {
                        result.Add(type);
                    }
                }
            }

            return result;
        }

        private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }
    }
}
