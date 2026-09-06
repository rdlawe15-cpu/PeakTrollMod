using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace PeakTrollMod
{
    internal static class ReflectionHelpers
    {
        private static readonly Dictionary<string, MethodInfo> Methods = new Dictionary<string, MethodInfo>();
        private static readonly Dictionary<string, FieldInfo> Fields = new Dictionary<string, FieldInfo>();

        public static MethodInfo Method(Type type, string name, Type[] args)
        {
            string key = type.FullName + "::" + name + "/" + args.Length;
            MethodInfo method;
            if (!Methods.TryGetValue(key, out method))
            {
                method = AccessTools.Method(type, name, args);
                Methods[key] = method;
            }
            return method;
        }

        public static FieldInfo Field(Type type, string name)
        {
            string key = type.FullName + "::" + name;
            FieldInfo field;
            if (!Fields.TryGetValue(key, out field))
            {
                field = AccessTools.Field(type, name);
                Fields[key] = field;
            }
            return field;
        }

        public static object Invoke(object target, MethodInfo method, params object[] args)
        {
            if (target == null || method == null) return null;
            return method.Invoke(target, args);
        }

        public static bool HasMethod(Type type, string name, params Type[] args) { return Method(type, name, args) != null; }
        public static bool HasField(Type type, string name) { return Field(type, name) != null; }
    }
}
