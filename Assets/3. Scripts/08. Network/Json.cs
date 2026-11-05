using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// 작은 JSON 읽기·쓰기. Firebase가 주는 자료는 키가 방 ID처럼 그때그때 달라서 JsonUtility로는 못 읽음.
// 읽으면: 객체 → Dictionary<string, object>, 배열 → List<object>, 정수 → long, 소수 → double, 문자열, bool, null.
public static class Json
{
    public static object Parse(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        int index = 0;
        object value = ParseValue(json, ref index);
        SkipWhitespace(json, ref index);
        if (index != json.Length) throw new FormatException($"JSON 끝에 남는 글자가 있습니다 (위치 {index}).");
        return value;
    }

    public static string Serialize(object value)
    {
        StringBuilder builder = new StringBuilder();
        Write(builder, value);
        return builder.ToString();
    }

    // ── 읽기 ────────────────────────────────────────────────────

    private static object ParseValue(string s, ref int i)
    {
        SkipWhitespace(s, ref i);
        if (i >= s.Length) throw new FormatException("JSON이 중간에 끝났습니다.");

        char c = s[i];
        if (c == '{') return ParseObject(s, ref i);
        if (c == '[') return ParseArray(s, ref i);
        if (c == '"') return ParseString(s, ref i);
        if (c == '-' || char.IsDigit(c)) return ParseNumber(s, ref i);
        if (Match(s, ref i, "true")) return true;
        if (Match(s, ref i, "false")) return false;
        if (Match(s, ref i, "null")) return null;
        throw new FormatException($"알 수 없는 JSON 값 (위치 {i}).");
    }

    private static Dictionary<string, object> ParseObject(string s, ref int i)
    {
        Dictionary<string, object> result = new Dictionary<string, object>();
        i++; // {
        SkipWhitespace(s, ref i);
        if (i < s.Length && s[i] == '}') { i++; return result; }

        while (true)
        {
            SkipWhitespace(s, ref i);
            string key = ParseString(s, ref i);
            SkipWhitespace(s, ref i);
            Expect(s, ref i, ':');
            result[key] = ParseValue(s, ref i);
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == ',') { i++; continue; }
            Expect(s, ref i, '}');
            return result;
        }
    }

    private static List<object> ParseArray(string s, ref int i)
    {
        List<object> result = new List<object>();
        i++; // [
        SkipWhitespace(s, ref i);
        if (i < s.Length && s[i] == ']') { i++; return result; }

        while (true)
        {
            result.Add(ParseValue(s, ref i));
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == ',') { i++; continue; }
            Expect(s, ref i, ']');
            return result;
        }
    }

    private static string ParseString(string s, ref int i)
    {
        Expect(s, ref i, '"');
        StringBuilder builder = new StringBuilder();
        while (i < s.Length)
        {
            char c = s[i++];
            if (c == '"') return builder.ToString();
            if (c != '\\') { builder.Append(c); continue; }

            char e = s[i++];
            switch (e)
            {
                case '"': builder.Append('"'); break;
                case '\\': builder.Append('\\'); break;
                case '/': builder.Append('/'); break;
                case 'b': builder.Append('\b'); break;
                case 'f': builder.Append('\f'); break;
                case 'n': builder.Append('\n'); break;
                case 'r': builder.Append('\r'); break;
                case 't': builder.Append('\t'); break;
                case 'u':
                    builder.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber));
                    i += 4;
                    break;
                default: throw new FormatException($"알 수 없는 이스케이프 \\{e}");
            }
        }
        throw new FormatException("문자열이 닫히지 않았습니다.");
    }

    private static object ParseNumber(string s, ref int i)
    {
        int start = i;
        bool isFraction = false;
        while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0)
        {
            if (s[i] == '.' || s[i] == 'e' || s[i] == 'E') isFraction = true;
            i++;
        }

        string text = s.Substring(start, i - start);
        if (!isFraction && long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long whole)) return whole;
        return double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static bool Match(string s, ref int i, string word)
    {
        if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) return false;
        i += word.Length;
        return true;
    }

    private static void Expect(string s, ref int i, char c)
    {
        if (i >= s.Length || s[i] != c) throw new FormatException($"'{c}'가 와야 합니다 (위치 {i}).");
        i++;
    }

    private static void SkipWhitespace(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
    }

    // ── 쓰기 ────────────────────────────────────────────────────

    private static void Write(StringBuilder b, object value)
    {
        switch (value)
        {
            case null: b.Append("null"); break;
            case string text: WriteString(b, text); break;
            case bool flag: b.Append(flag ? "true" : "false"); break;
            case Enum e: b.Append(Convert.ToInt64(e).ToString(CultureInfo.InvariantCulture)); break;
            case float f: b.Append(f.ToString("R", CultureInfo.InvariantCulture)); break;
            case double d: b.Append(d.ToString("R", CultureInfo.InvariantCulture)); break;
            case IDictionary dictionary:
                b.Append('{');
                bool first = true;
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (!first) b.Append(',');
                    first = false;
                    WriteString(b, entry.Key.ToString());
                    b.Append(':');
                    Write(b, entry.Value);
                }
                b.Append('}');
                break;
            case IEnumerable list:
                b.Append('[');
                bool firstItem = true;
                foreach (object item in list)
                {
                    if (!firstItem) b.Append(',');
                    firstItem = false;
                    Write(b, item);
                }
                b.Append(']');
                break;
            case IConvertible number:
                b.Append(number.ToString(CultureInfo.InvariantCulture));
                break;
            default:
                throw new ArgumentException($"JSON으로 바꿀 수 없는 값: {value.GetType().Name}");
        }
    }

    private static void WriteString(StringBuilder b, string text)
    {
        b.Append('"');
        foreach (char c in text)
        {
            switch (c)
            {
                case '"': b.Append("\\\""); break;
                case '\\': b.Append("\\\\"); break;
                case '\n': b.Append("\\n"); break;
                case '\r': b.Append("\\r"); break;
                case '\t': b.Append("\\t"); break;
                default:
                    if (c < ' ') b.Append("\\u").Append(((int)c).ToString("x4"));
                    else b.Append(c);
                    break;
            }
        }
        b.Append('"');
    }
}
