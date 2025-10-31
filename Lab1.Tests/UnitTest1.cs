using System.Text;
using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.Fluent;


namespace LabTemplateFSM.Tests
{
    public static class PatternProps
    {
        // Ограничение перебора по длине
        private const int MaxStringLen = 8;
        private const int MaxPatternLen = 5;

        // Небольшие подмножества букв для перебора
        private static readonly char[] UpperSample = { 'A' };
        private static readonly char[] LowerSample = { 'a' };

        // ---------- Генерация шаблонов ----------

        private struct Atom
        {
            public char Symbol;   // 'A' | 'a' | '-' | ' ' | '.'
            public char? Quant;   // null | '+' | '*' | '?'
            public override string ToString() => Quant is char q ? $"{Symbol}{q}" : Symbol.ToString();
        }

        private static Gen<Atom> AtomGen =>
            from sym in Gen.Elements('A', 'a', '-', ' ', '.')
            from q in Gen.Frequency<char?>(
                        (5, Gen.Constant<char?>(null)),
                        (2, Gen.Constant<char?>('+')),
                        (2, Gen.Constant<char?>('*')),
                        (1, Gen.Constant<char?>('?')))
            select new Atom { Symbol = sym, Quant = q };

        private static Gen<string> PatternGen =>
            from len in Gen.Choose(1, MaxPatternLen)
            from atoms in Gen.ListOf(AtomGen, len)
            select string.Concat(atoms.Select(a => a.ToString()));

        private static Arbitrary<string> PatternArb() => Arb.From(PatternGen);

        // ---------- Маппинг: шаблон -> Regex (эквивалентно NFA) ----------

        private static string ToRegexPatternEquivalentToNfa(string pattern)
        {
            // Каждый атом — одиночный символ; квантор, если есть, относится к нему.
            var sb = new StringBuilder();
            sb.Append('^');

            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];

                // захват квантора, если он есть
                char? quant = null;
                if (i + 1 < pattern.Length)
                {
                    char q = pattern[i + 1];
                    if (q == '+' || q == '*' || q == '?')
                    {
                        quant = q;
                        i++;
                    }
                }

                string core = c switch
                {
                    'A' => @"[\p{Lu}]",
                    'a' => @"[\p{Ll}]",
                    '-' => @"\-",
                    ' ' => " ",
                    '.' => @"\.",
                    _   => "" // игнор нежданных символов, как и в билдере NFA
                };
                if (core.Length == 0) continue;

                string qout = quant switch
                {
                    '+' => "+",
                    '*' => "*", 
                    '?' => "?",
                    null => "",
                    _ => ""
                };

                sb.Append(core);
                sb.Append(qout);
            }

            sb.Append('$');
            return sb.ToString();
        }

        // ---------- Алфавит для перебора строк ----------

        private static IReadOnlyList<char> BuildAlphabetForPattern(string pattern)
        {
            bool hasA = pattern.IndexOf('A') >= 0;
            bool hasa = pattern.IndexOf('a') >= 0;
            bool hasHyphen = pattern.IndexOf('-') >= 0;
            bool hasSpace = pattern.IndexOf(' ') >= 0;
            bool hasDot = pattern.IndexOf('.') >= 0;

            var alph = new List<char>(8);
            if (hasA) alph.AddRange(UpperSample);
            if (hasa) alph.AddRange(LowerSample);
            if (hasHyphen) alph.Add('-');
            if (hasSpace) alph.Add(' ');
            if (hasDot) alph.Add('.');

            if (alph.Count == 0)
                alph.AddRange(UpperSample); // чтобы не получилось пустого перебора

            return alph;
        }

        private static IEnumerable<string> EnumerateAllStrings(IReadOnlyList<char> a)
        {
            yield return string.Empty;
            for (int len = 1; len <= MaxStringLen; len++)
                foreach (var s in EnumerateFixedLen(a, len))
                    yield return s;

            static IEnumerable<string> EnumerateFixedLen(IReadOnlyList<char> a, int len)
            {
                var buf = new char[len];
                foreach (var _ in Recurse(0))
                    yield return new string(buf);

                IEnumerable<int> Recurse(int i)
                {
                    if (i == len) { yield return 0; yield break; }
                    for (int k = 0; k < a.Count; k++)
                    {
                        buf[i] = a[k];
                        foreach (var __ in Recurse(i + 1))
                            yield return 0;
                    }
                }
            }
        }

        // Принятие автоматом: на конце строки допустим NewLine (он обязателен в шаблоне)
        private static bool AcceptedByNfa(NfaEngine engine, string s) =>
            engine.ComputeAllowedAt(s, s.Length).NewLine;

        private static bool AcceptedByRegex(Regex rx, string s) => rx.IsMatch(s);

        // ---------- Property-тест ----------
        [Test]
        public static void Pattern_ToRegex_Mapping_Is_Correct_For_All_Strings_Up_To_MaxLen()
        {
            Prop.ForAll(PatternArb().Generator.ToArbitrary(), pattern =>
            {
                // 1) строим NFA и Regex по одному и тому же шаблону
                var engine = new NfaEngine(pattern);
                var rx = new Regex(ToRegexPatternEquivalentToNfa(pattern), RegexOptions.CultureInvariant);

                // 2) алфавит выводим из шаблона
                var alphabet = BuildAlphabetForPattern(pattern);

                // 3) перебираем ВСЕ строки над этим алфавитом до MaxStringLen включительно
                foreach (var s in EnumerateAllStrings(alphabet))
                {
                    bool nfaOk = AcceptedByNfa(engine, s);
                    bool rxOk  = AcceptedByRegex(rx, s);

                    if (nfaOk != rxOk)
                    {
                        throw new AssertionException(
                            $"Mismatch: pattern='{pattern}', regex='{ToRegexPatternEquivalentToNfa(pattern)}', s='{Escape(s)}', NFA={nfaOk}, Regex={rxOk}");
                    }
                }
                TestContext.Progress.WriteLine($"OK: pattern='{pattern}'");
                return true;
            }).QuickCheckThrowOnFailure();
        }

        private static string Escape(string s) =>
            s.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
    }
}
