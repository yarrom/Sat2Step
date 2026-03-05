
using System.Text.RegularExpressions;

namespace SimpleSatParser
{

    public static class SatLexer
    {
        // Разбивает вход на строки и возвращает токены строки
        public static IEnumerable<string> Lines(string text)
        {
            // Убираем CRLF и возвращаем непустые строки
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (var l in lines)
            {
                var t = l.Trim();
                if (string.IsNullOrEmpty(t)) continue;
                yield return t;
            }
        }

        public static List<string> Tokens(string line)
        {
            var tokens = new List<string>();
            // простая токенизация: разделение пробелами, но сохраняем слова и символы
            var rx = new Regex(@"(-?\d+)|(-?\d+\.\d+([eE][-+]?\d+)?)|([A-Za-z_][A-Za-z0-9_\-\.]*)|(\S)");
            foreach (Match m in rx.Matches(line))
            {
                tokens.Add(m.Value);
            }
            return tokens;
        }
    }

}