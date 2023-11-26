﻿using System;
using System.Text;
using UnityEngine;
using Conditional = System.Diagnostics.ConditionalAttribute;
using Object = UnityEngine.Object;
#if UIP_LOG
using System.Reflection;
using System.Collections.Generic;
#endif

namespace Coffee.UIParticleExtensions
{
    internal static class Logging
    {
        private const string k_EnableSymbol = "UIP_LOG";

        [Conditional(k_EnableSymbol)]
        private static void Log_Internal(LogType type, object tag, object message, Object context)
        {
#if UIP_LOG
            AppendTag(s_Sb, tag);
            s_Sb.Append(message);
            switch (type)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    Debug.LogError(s_Sb, context);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(s_Sb, context);
                    break;
                case LogType.Log:
                    Debug.Log(s_Sb, context);
                    break;
            }

            s_Sb.Length = 0;
#endif
        }

        [Conditional(k_EnableSymbol)]
        public static void LogIf(bool enable, object tag, object message, Object context = null)
        {
            if (!enable) return;
            Log_Internal(LogType.Log, tag, message, context ? context : tag as Object);
        }

        [Conditional(k_EnableSymbol)]
        public static void Log(object tag, object message, Object context = null)
        {
            Log_Internal(LogType.Log, tag, message, context ? context : tag as Object);
        }

        [Conditional(k_EnableSymbol)]
        public static void LogWarning(object tag, object message, Object context = null)
        {
            Log_Internal(LogType.Warning, tag, message, context ? context : tag as Object);
        }

        public static void LogError(object tag, object message, Object context = null)
        {
#if UIP_LOG
            Log_Internal(LogType.Error, tag, message, context ? context : tag as Object);
#else
            Debug.LogError($"{tag}: {message}", context);
#endif
        }

        [Conditional(k_EnableSymbol)]
        public static void LogMulticast(Type type, string fieldName, object instance = null, string message = null)
        {
#if UIP_LOG
            AppendTag(s_Sb, instance ?? type);

            var handler = type
                .GetField(fieldName,
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)
                ?.GetValue(instance);

            var list = ((MulticastDelegate)handler)?.GetInvocationList() ?? Array.Empty<Delegate>();
            s_Sb.Append("<color=orange>");
            s_Sb.Append(type.Name);
            s_Sb.Append(".");
            s_Sb.Append(fieldName);
            s_Sb.Append(" has ");
            s_Sb.Append(list.Length);
            s_Sb.Append(" callbacks");
            if (message != null)
            {
                s_Sb.Append(" (");
                s_Sb.Append(message);
                s_Sb.Append(")");
            }

            s_Sb.Append(":</color>");

            for (var i = 0; i < list.Length; i++)
            {
                s_Sb.Append("\n - ");
                s_Sb.Append(list[i].Method.DeclaringType?.Name);
                s_Sb.Append(".");
                s_Sb.Append(list[i].Method.Name);
            }

            Debug.Log(s_Sb);
            s_Sb.Length = 0;
#endif
        }

        [Conditional(k_EnableSymbol)]
        private static void AppendTag(StringBuilder sb, object tag)
        {
#if UIP_LOG
            try
            {
                sb.Append("f");
                sb.Append(Time.frameCount);
                sb.Append(":<color=#");
                AppendReadableCode(sb, tag);
                sb.Append("><b>[");

                switch (tag)
                {
                    case Type type:
                        AppendType(sb, type);
                        break;
                    case Object uObject:
                        AppendType(sb, tag.GetType());
                        sb.Append(" #");
                        sb.Append(uObject.name);
                        break;
                    default:
                        AppendType(sb, tag.GetType());
                        break;
                }

                sb.Append("]</b></color> ");
            }
            catch
            {
                sb.Append("f");
                sb.Append(Time.frameCount);
                sb.Append(":<b>[");
                sb.Append(tag);
                sb.Append("]</b> ");
            }
#endif
        }

        [Conditional(k_EnableSymbol)]
        private static void AppendType(StringBuilder sb, Type type)
        {
#if UIP_LOG
            if (s_TypeNameCache.TryGetValue(type, out var name))
            {
                sb.Append(name);
                return;
            }

            // New type found
            var start = sb.Length;
            sb.Append(type.Name);
            if (type.IsGenericType)
            {
                sb.Length -= 2;
                sb.Append("<");
                foreach (var gType in type.GetGenericArguments())
                {
                    AppendType(sb, gType);
                    sb.Append(", ");
                }

                sb.Length -= 2;
                sb.Append(">");
            }

            s_TypeNameCache.Add(type, sb.ToString(start, sb.Length - start));
#endif
        }


        [Conditional(k_EnableSymbol)]
        private static void AppendReadableCode(StringBuilder sb, object tag)
        {
#if UIP_LOG
            int hash;
            try
            {
                switch (tag)
                {
                    case string text:
                        hash = text.GetHashCode();
                        break;
                    case Type type:
                        type = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                        hash = type.FullName?.GetHashCode() ?? 0;
                        break;
                    default:
                        hash = tag.GetType().FullName?.GetHashCode() ?? 0;
                        break;
                }
            }
            catch
            {
                sb.Append("FFFFFF");
                return;
            }

            hash = hash & (s_Codes.Length - 1);
            if (s_Codes[hash] == null)
            {
                var hue = hash / (float)s_Codes.Length;
                var modifier = 1f - Mathf.Clamp01(Mathf.Abs(hue - 0.65f) / 0.2f);
                var saturation = 0.8f + modifier * -0.2f;
                var value = 0.7f + modifier * 0.3f;
                s_Codes[hash] = ColorUtility.ToHtmlStringRGB(Color.HSVToRGB(hue, saturation, value));
            }

            sb.Append(s_Codes[hash]);
#endif
        }

#if UIP_LOG
        private static readonly StringBuilder s_Sb = new StringBuilder();
        private static readonly string[] s_Codes = new string[32];
        private static readonly Dictionary<Type, string> s_TypeNameCache = new Dictionary<Type, string>();
#endif
    }
}
