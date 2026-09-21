using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MountainWardBarrier.Core
{
    /// <summary>
    /// 极简 JSON 解析 / 序列化器（纯 C# 实现，不依赖 UnityEngine，也不依赖任何第三方库）。
    ///
    /// 之所以自己写一个，是因为 Unity 自带的 JsonUtility 无法处理字典与嵌套数组，
    /// 而把配置解析放在纯逻辑层可以让它在 Unity 之外被单元测试覆盖。
    ///
    /// 解析结果映射：
    ///   object  -> Dictionary&lt;string, object&gt;
    ///   array   -> List&lt;object&gt;
    ///   number  -> double
    ///   string  -> string
    ///   bool    -> bool
    ///   null    -> null
    /// </summary>
    public static class MiniJson
    {
        // ---------------------------------------------------------------- 解析

        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            int index = 0;
            try
            {
                object result = ParseValue(json, ref index);
                SkipWhitespace(json, ref index);
                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Dictionary<string, object> DeserializeObject(string json)
        {
            object o = Deserialize(json);
            return AsDict(o);
        }

        private static object ParseValue(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            if (i >= s.Length)
            {
                throw new FormatException("Unexpected end of JSON.");
            }

            char c = s[i];
            if (c == '{')
            {
                return ParseObject(s, ref i);
            }
            if (c == '[')
            {
                return ParseArray(s, ref i);
            }
            if (c == '"')
            {
                return ParseString(s, ref i);
            }
            if (c == 't')
            {
                Expect(s, ref i, "true");
                return true;
            }
            if (c == 'f')
            {
                Expect(s, ref i, "false");
                return false;
            }
            if (c == 'n')
            {
                Expect(s, ref i, "null");
                return null;
            }
            return ParseNumber(s, ref i);
        }

        private static void Expect(string s, ref int i, string token)
        {
            if (i + token.Length > s.Length || string.CompareOrdinal(s, i, token, 0, token.Length) != 0)
            {
                throw new FormatException("Expected token '" + token + "' at " + i);
            }
            i += token.Length;
        }

        private static Dictionary<string, object> ParseObject(string s, ref int i)
        {
            Dictionary<string, object> table = new Dictionary<string, object>();
            i++; // consume '{'
            while (true)
            {
                SkipWhitespace(s, ref i);
                if (i >= s.Length)
                {
                    throw new FormatException("Unterminated object.");
                }
                if (s[i] == '}')
                {
                    i++;
                    return table;
                }
                if (s[i] == ',')
                {
                    i++;
                    continue;
                }

                string key = ParseString(s, ref i);
                SkipWhitespace(s, ref i);
                if (i >= s.Length || s[i] != ':')
                {
                    throw new FormatException("Expected ':' at " + i);
                }
                i++;
                object value = ParseValue(s, ref i);
                table[key] = value;
            }
        }

        private static List<object> ParseArray(string s, ref int i)
        {
            List<object> list = new List<object>();
            i++; // consume '['
            while (true)
            {
                SkipWhitespace(s, ref i);
                if (i >= s.Length)
                {
                    throw new FormatException("Unterminated array.");
                }
                if (s[i] == ']')
                {
                    i++;
                    return list;
                }
                if (s[i] == ',')
                {
                    i++;
                    continue;
                }
                list.Add(ParseValue(s, ref i));
            }
        }

        private static string ParseString(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            if (i >= s.Length || s[i] != '"')
            {
                throw new FormatException("Expected string at " + i);
            }
            i++;
            StringBuilder sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '"')
                {
                    i++;
                    return sb.ToString();
                }
                if (c == '\\')
                {
                    i++;
                    if (i >= s.Length)
                    {
                        break;
                    }
                    char e = s[i];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            {
                                if (i + 4 >= s.Length)
                                {
                                    throw new FormatException("Bad \\u escape at " + i);
                                }
                                string hex = s.Substring(i + 1, 4);
                                int code = int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                                sb.Append((char)code);
                                i += 4;
                                break;
                            }
                        default:
                            sb.Append(e);
                            break;
                    }
                    i++;
                    continue;
                }
                sb.Append(c);
                i++;
            }
            throw new FormatException("Unterminated string.");
        }

        private static object ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length)
            {
                char c = s[i];
                if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E')
                {
                    i++;
                }
                else
                {
                    break;
                }
            }
            if (i == start)
            {
                throw new FormatException("Expected number at " + start);
            }
            string token = s.Substring(start, i - start);
            double d;
            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
            {
                return d;
            }
            throw new FormatException("Bad number '" + token + "'");
        }

        private static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length)
            {
                char c = s[i];
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                {
                    i++;
                }
                else
                {
                    break;
                }
            }
        }

        // ---------------------------------------------------------------- 序列化

        public static string Serialize(object value)
        {
            StringBuilder sb = new StringBuilder();
            WriteValue(sb, value);
            return sb.ToString();
        }

        private static void WriteValue(StringBuilder sb, object value)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            if (value is string)
            {
                WriteString(sb, (string)value);
                return;
            }

            if (value is bool)
            {
                sb.Append(((bool)value) ? "true" : "false");
                return;
            }

            if (value is float || value is double || value is int || value is long || value is uint || value is short)
            {
                double d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (Math.Abs(d - Math.Round(d)) < 0.0000001 && Math.Abs(d) < 1e15)
                {
                    sb.Append(((long)Math.Round(d)).ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
                }
                return;
            }

            Dictionary<string, object> dict = value as Dictionary<string, object>;
            if (dict != null)
            {
                sb.Append('{');
                bool first = true;
                foreach (KeyValuePair<string, object> kv in dict)
                {
                    if (!first)
                    {
                        sb.Append(',');
                    }
                    first = false;
                    WriteString(sb, kv.Key);
                    sb.Append(':');
                    WriteValue(sb, kv.Value);
                }
                sb.Append('}');
                return;
            }

            System.Collections.IEnumerable seq = value as System.Collections.IEnumerable;
            if (seq != null)
            {
                sb.Append('[');
                bool first = true;
                foreach (object item in seq)
                {
                    if (!first)
                    {
                        sb.Append(',');
                    }
                    first = false;
                    WriteValue(sb, item);
                }
                sb.Append(']');
                return;
            }

            WriteString(sb, value.ToString());
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 32)
                        {
                            sb.Append("\\u");
                            sb.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
        }

        // ---------------------------------------------------------------- 取值助手

        public static Dictionary<string, object> AsDict(object o)
        {
            return o as Dictionary<string, object>;
        }

        public static List<object> AsList(object o)
        {
            return o as List<object>;
        }

        public static bool Has(Dictionary<string, object> d, string key)
        {
            return d != null && key != null && d.ContainsKey(key);
        }

        public static string GetString(Dictionary<string, object> d, string key, string fallback)
        {
            if (d == null || !d.ContainsKey(key))
            {
                return fallback;
            }
            object v = d[key];
            if (v == null)
            {
                return fallback;
            }
            string s = v as string;
            if (s != null)
            {
                return s;
            }
            return v.ToString();
        }

        public static float GetFloat(Dictionary<string, object> d, string key, float fallback)
        {
            if (d == null || !d.ContainsKey(key))
            {
                return fallback;
            }
            object v = d[key];
            if (v is double)
            {
                return (float)(double)v;
            }
            if (v is string)
            {
                float parsed;
                if (float.TryParse((string)v, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                {
                    return parsed;
                }
            }
            return fallback;
        }

        public static int GetInt(Dictionary<string, object> d, string key, int fallback)
        {
            if (d == null || !d.ContainsKey(key))
            {
                return fallback;
            }
            object v = d[key];
            if (v is double)
            {
                return (int)Math.Round((double)v);
            }
            if (v is string)
            {
                int parsed;
                if (int.TryParse((string)v, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    return parsed;
                }
            }
            return fallback;
        }

        public static bool GetBool(Dictionary<string, object> d, string key, bool fallback)
        {
            if (d == null || !d.ContainsKey(key))
            {
                return fallback;
            }
            object v = d[key];
            if (v is bool)
            {
                return (bool)v;
            }
            if (v is double)
            {
                return Math.Abs((double)v) > 0.0001;
            }
            return fallback;
        }

        public static List<object> GetList(Dictionary<string, object> d, string key)
        {
            if (d == null || !d.ContainsKey(key))
            {
                return null;
            }
            return AsList(d[key]);
        }

        public static Dictionary<string, object> GetDict(Dictionary<string, object> d, string key)
        {
            if (d == null || !d.ContainsKey(key))
            {
                return null;
            }
            return AsDict(d[key]);
        }
    }
}
