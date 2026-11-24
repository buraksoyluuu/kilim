using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Kilim.Core
{
    /// <summary>
    /// Bağımlılıksız küçük JSON okuyucu (Unity JsonUtility iç içe dizileri okuyamadığı için).
    /// Çıktı: Dictionary&lt;string,object&gt;, List&lt;object&gt;, double, string, bool, null.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var p = new Parser(json);
            var v = p.ReadValue();
            p.SkipWs();
            if (!p.End) throw p.Error("Fazladan karakter");
            return v;
        }

        /// <summary>Dictionary/IList/sayı/metin/bool/null değerini JSON'a yazar (kayıt dosyası için).</summary>
        public static string Serialize(object value)
        {
            var sb = new StringBuilder();
            Write(sb, value);
            return sb.ToString();
        }

        private static void Write(StringBuilder sb, object v)
        {
            switch (v)
            {
                case null: sb.Append("null"); return;
                case string s: WriteString(sb, s); return;
                case bool b: sb.Append(b ? "true" : "false"); return;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); return;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); return;
                case double d: sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); return;
                case float f: sb.Append(((double)f).ToString("R", CultureInfo.InvariantCulture)); return;
                case System.Collections.IDictionary dict:
                {
                    sb.Append('{');
                    bool first = true;
                    foreach (System.Collections.DictionaryEntry e in dict)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        WriteString(sb, Convert.ToString(e.Key, CultureInfo.InvariantCulture));
                        sb.Append(':');
                        Write(sb, e.Value);
                    }
                    sb.Append('}');
                    return;
                }
                case System.Collections.IEnumerable list:
                {
                    sb.Append('[');
                    bool first = true;
                    foreach (var item in list)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        Write(sb, item);
                    }
                    sb.Append(']');
                    return;
                }
                default:
                    if (v is IConvertible) { Write(sb, Convert.ToDouble(v, CultureInfo.InvariantCulture)); return; }
                    throw new ArgumentException($"Serileştirilemeyen tür: {v.GetType()}");
            }
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char ch in s)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4"));
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
        }

        private sealed class Parser
        {
            private readonly string _s;
            private int _i;
            public Parser(string s) { _s = s; }
            public bool End => _i >= _s.Length;
            public FormatException Error(string m) => new FormatException($"JSON hatası @{_i}: {m}");

            public void SkipWs() { while (!End && char.IsWhiteSpace(_s[_i])) _i++; }

            public object ReadValue()
            {
                SkipWs();
                if (End) throw Error("Beklenmeyen son");
                char ch = _s[_i];
                switch (ch)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (ch == '-' || char.IsDigit(ch)) return ReadNumber();
                        throw Error($"Beklenmeyen karakter '{ch}'");
                }
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0) throw Error($"'{word}' bekleniyordu");
                _i += word.Length;
            }

            private Dictionary<string, object> ReadObject()
            {
                var d = new Dictionary<string, object>();
                _i++; SkipWs();
                if (!End && _s[_i] == '}') { _i++; return d; }
                while (true)
                {
                    SkipWs();
                    if (End || _s[_i] != '"') throw Error("Anahtar bekleniyordu");
                    var k = ReadString();
                    SkipWs();
                    if (End || _s[_i] != ':') throw Error("':' bekleniyordu");
                    _i++;
                    d[k] = ReadValue();
                    SkipWs();
                    if (End) throw Error("Nesne kapanmadı");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == '}') { _i++; return d; }
                    throw Error("',' ya da '}' bekleniyordu");
                }
            }

            private List<object> ReadArray()
            {
                var l = new List<object>();
                _i++; SkipWs();
                if (!End && _s[_i] == ']') { _i++; return l; }
                while (true)
                {
                    l.Add(ReadValue());
                    SkipWs();
                    if (End) throw Error("Dizi kapanmadı");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == ']') { _i++; return l; }
                    throw Error("',' ya da ']' bekleniyordu");
                }
            }

            private string ReadString()
            {
                var sb = new StringBuilder();
                _i++;
                while (true)
                {
                    if (End) throw Error("Metin kapanmadı");
                    char ch = _s[_i++];
                    if (ch == '"') return sb.ToString();
                    if (ch != '\\') { sb.Append(ch); continue; }
                    if (End) throw Error("Kaçış eksik");
                    char e = _s[_i++];
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
                            if (_i + 4 > _s.Length) throw Error("\\u eksik");
                            sb.Append((char)Convert.ToInt32(_s.Substring(_i, 4), 16));
                            _i += 4;
                            break;
                        default: throw Error($"Geçersiz kaçış \\{e}");
                    }
                }
            }

            private double ReadNumber()
            {
                int start = _i;
                while (!End && "+-0123456789.eE".IndexOf(_s[_i]) >= 0) _i++;
                return double.Parse(_s.Substring(start, _i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }
}
