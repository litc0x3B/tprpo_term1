using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace LabTemplateFSM
{
    // -----------------------------
    // Выделенный класс автомата NFA
    // -----------------------------
    public sealed class NfaEngine
    {
        // Внешний результат из ComputeAllowedAt
        public struct Allowed
        {
            public bool Upper, Lower, Hyphen, Space, Dot, NewLine;

            public bool HasAny =>
                Upper || Lower || Hyphen || Space || Dot || NewLine;

            public int CountLiteral =>
                (Hyphen ? 1 : 0) + (Space ? 1 : 0) + (Dot ? 1 : 0) + (NewLine ? 1 : 0);

            public char TheOnlyLiteral()
            {
                if (CountLiteral != 1) throw new InvalidOperationException("Not a single literal.");
                if (Hyphen) return '-';
                if (Space) return ' ';
                if (Dot) return '.';
                return '\n';
            }
        }

        // Внутренние типы
        enum SymKind { Upper, Lower, Hyphen, Space, Dot, NewLine }

        sealed class Trans { public SymKind Kind; public int To; }

        sealed class State
        {
            public List<Trans> Edges = new List<Trans>();
            public List<int> Eps = new List<int>();
            public bool Accepting;
        }

        // Поля автомата
        List<State> _nfa = new List<State>();
        int _startState = 0;

        // Конструктор: строит НКА по шаблону + обязательный NewLine в конце
        public NfaEngine(string pattern)
        {
            BuildNfa(pattern ?? string.Empty);
        }

        // Вернуть допустимые символы на позиции pos в строке line
        public Allowed ComputeAllowedAt(string line, int pos)
        {
            var curr = EpsClosure(new[] { _startState });

            // Идём по введённым символам текущей строки
            for (int i = 0; i < pos; i++)
                curr = EpsClosure(Move(curr, line[i]));

            Allowed a = default;
            foreach (int s in curr)
            {
                foreach (var tr in _nfa[s].Edges)
                {
                    switch (tr.Kind)
                    {
                        case SymKind.Upper: a.Upper = true; break;
                        case SymKind.Lower: a.Lower = true; break;
                        case SymKind.Hyphen: a.Hyphen = true; break;
                        case SymKind.Space: a.Space = true; break;
                        case SymKind.Dot: a.Dot = true; break;
                        case SymKind.NewLine: a.NewLine = true; break;
                    }
                }
            }
            return a;
        }

        // ----------------- Внутренняя реализация -----------------

        SymKind? CharToKind(char c)
        {
            if (c == '-') return SymKind.Hyphen;
            if (c == ' ') return SymKind.Space;
            if (c == '.') return SymKind.Dot;
            if (c == 'A') return SymKind.Upper;
            if (c == 'a') return SymKind.Lower;
            return null; // NewLine автоматически добавляется в конце шаблона
        }

        void BuildNfa(string pattern)
        {
            _nfa = new List<State>();
            int NewState() { _nfa.Add(new State()); return _nfa.Count - 1; }

            int s0 = NewState();
            _startState = s0;
            int curr = s0;

            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];
                var kind = CharToKind(c);
                if (kind == null) continue;

                char next = (i + 1 < pattern.Length) ? pattern[i + 1] : '\0';
                bool hasQuant = next == '+' || next == '*' || next == '?';
                char quant = hasQuant ? next : '\0';
                if (hasQuant) i++;

                switch (quant)
                {
                    case '\0':
                    {
                        int outS = NewState();
                        _nfa[curr].Edges.Add(new Trans { Kind = kind.Value, To = outS });
                        curr = outS;
                        break;
                    }
                    case '+': // 1+
                    {
                        int loop = NewState();
                        _nfa[curr].Edges.Add(new Trans { Kind = kind.Value, To = loop });
                        _nfa[loop].Edges.Add(new Trans { Kind = kind.Value, To = loop });
                        curr = loop;
                        break;
                    }
                    case '*': // 0+
                    {
                        int loop = NewState();
                        int outS = NewState();

                        // ноль раз: можно сразу уйти дальше
                        _nfa[curr].Eps.Add(outS);

                        // один и более раз
                        _nfa[curr].Edges.Add(new Trans { Kind = kind.Value, To = loop });
                        _nfa[loop].Edges.Add(new Trans { Kind = kind.Value, To = loop });

                        // после любых повторов выходим в outS
                        _nfa[loop].Eps.Add(outS);

                        curr = outS;
                        break;
                    }
                    case '?': // 0/1
                    {
                        int outS = NewState();
                        _nfa[curr].Edges.Add(new Trans { Kind = kind.Value, To = outS }); // 1 раз
                        _nfa[curr].Eps.Add(outS);                                         // 0 раз
                        curr = outS;
                        break;
                    }
                }
            }

            // Обязательный перевод строки в конце шаблона
            int sEnd = NewState();
            _nfa[curr].Edges.Add(new Trans { Kind = SymKind.NewLine, To = sEnd });
            _nfa[sEnd].Accepting = true;
        }


        List<int> EpsClosure(IEnumerable<int> states)
        {
            var stack = new Stack<int>(states);
            var seen = new HashSet<int>(states);
            while (stack.Count > 0)
            {
                int s = stack.Pop();
                foreach (var e in _nfa[s].Eps)
                    if (seen.Add(e)) stack.Push(e);
            }
            return seen.ToList();
        }

        List<int> Move(IEnumerable<int> states, char ch)
        {
            bool isLetter = char.IsLetter(ch);
            bool isUpper = isLetter && char.IsUpper(ch);
            bool isLower = isLetter && char.IsLower(ch);
            var dest = new List<int>();

            foreach (int s in states)
            {
                foreach (var tr in _nfa[s].Edges)
                {
                    switch (tr.Kind)
                    {
                        case SymKind.Upper:
                            if (isUpper) dest.Add(tr.To);
                            break;
                        case SymKind.Lower:
                            if (isLower) dest.Add(tr.To);
                            break;
                        case SymKind.Hyphen:
                            if (ch == '-') dest.Add(tr.To);
                            break;
                        case SymKind.Space:
                            if (ch == ' ') dest.Add(tr.To);
                            break;
                        case SymKind.Dot:
                            if (ch == '.') dest.Add(tr.To);
                            break;
                        case SymKind.NewLine:
                            if (ch == '\n' || ch == '\r') dest.Add(tr.To);
                            break;
                    }
                }
            }
            return dest;
        }
    }

    // -----------------------------
    // WinForms-форма, использующая NfaEngine
    // -----------------------------
    public partial class Form1 : Form
    {
        NfaEngine _engine;
        string _cachedPattern = null;
        bool _inAutoFill = false;

        public Form1()
        {
            InitializeComponent();
            textBoxList.KeyPress += TextBoxList_KeyPress;
            textBoxList.KeyUp += TextBoxList_KeyUp;
            textBoxList.TextChanged += TextBoxList_TextChanged;
        }

        // Обновление/создание автомата при изменении шаблона (первая строка)
        void EnsureEngineUpToDate()
        {
            string pattern = GetFirstLine();
            if (pattern == _cachedPattern && _engine != null) return;
            _engine = new NfaEngine(pattern ?? string.Empty);
            _cachedPattern = pattern;
        }

        string GetFirstLine()
        {
            var t = textBoxList.Text;
            if (string.IsNullOrEmpty(t)) return string.Empty;
            int nl = t.IndexOf('\n');
            return nl < 0 ? t : t.Substring(0, nl);
        }

        // Автоподстановка литералов (пробел/дефис/точка). \n не вставляем автоматически.
        void AutoFillDeterministicLiterals()
        {
            if (_inAutoFill) return;
            try
            {
                _inAutoFill = true;

                GetCurrentLine(out _, out var lineText, out var caretInLine, out var isFirstLine);
                if (isFirstLine) return;
                if (_engine == null) return;
                if (caretInLine != lineText.Length) return;

                for (int guard = 0; guard < 512; guard++)
                {
                    var allowed = _engine.ComputeAllowedAt(lineText, lineText.Length);
                    bool hasLetterChoice = allowed.Upper || allowed.Lower;
                    if (!hasLetterChoice && allowed.CountLiteral == 1)
                    {
                        char c = allowed.TheOnlyLiteral();
                        if (c == '\n') break; // перевод строки не подставляем автоматически
                        textBoxList.AppendText(c.ToString());
                        lineText += c;
                        continue;
                    }
                    break;
                }
            }
            finally { _inAutoFill = false; }
        }

        // Обработка клавиш
        void TextBoxList_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar) && e.KeyChar != (char)Keys.Return) return;

            GetCurrentLine(out _, out string lineText, out int caretInLine, out bool isFirstLine);

            // Enter
            if (e.KeyChar == (char)Keys.Return)
            {
                if (isFirstLine) return; // в строке шаблона Enter не ограничиваем
                if (_engine == null) { e.Handled = true; return; }
                if (textBoxList.SelectionLength != 0 || caretInLine != lineText.Length)
                {
                    e.Handled = true; return;
                }
                var allowed = _engine.ComputeAllowedAt(lineText, caretInLine);
                if (!allowed.NewLine) { e.Handled = true; return; }
                return;
            }

            // Первая строка: разрешаем только символы шаблона
            if (isFirstLine)
            {
                char ch = e.KeyChar;
                bool ok = ch == 'A' || ch == 'a' || ch == '-' || ch == ' ' || ch == '.' || ch == '+' || ch == '*' || ch == '?';
                if (!ok) { e.Handled = true; return; }
                return;
            }

            // Ввод в конец строки
            if (textBoxList.SelectionLength != 0 || caretInLine != lineText.Length)
            {
                e.Handled = true;
                return;
            }

            if (_engine == null) { e.Handled = true; return; }

            var allowed2 = _engine.ComputeAllowedAt(lineText, caretInLine);
            char c2 = e.KeyChar;

            if (char.IsLetter(c2))
            {
                if (allowed2.Upper && !allowed2.Lower) { e.KeyChar = char.ToUpper(c2); return; }
                if (allowed2.Lower && !allowed2.Upper) { e.KeyChar = char.ToLower(c2); return; }
                if (allowed2.Upper && allowed2.Lower) { return; }
                e.Handled = true; return;
            }
            else
            {
                bool ok =
                    (c2 == '-' && allowed2.Hyphen) ||
                    (c2 == ' ' && allowed2.Space) ||
                    (c2 == '.' && allowed2.Dot);
                if (!ok) { e.Handled = true; return; }
            }
        }

        void TextBoxList_KeyUp(object sender, KeyEventArgs e)
        {
            GetCurrentLine(out _, out _, out _, out bool isFirstLine);
            if (!isFirstLine) AutoFillDeterministicLiterals();
        }

        void TextBoxList_TextChanged(object sender, EventArgs e)
        {
            EnsureEngineUpToDate();
            GetCurrentLine(out _, out _, out _, out bool isFirstLine);
            if (!isFirstLine) AutoFillDeterministicLiterals();
        }

        // Текущая строка и позиция курсора
        void GetCurrentLine(out int lineStart, out string lineText, out int caretInLine, out bool isFirstLine)
        {
            string all = textBoxList.Text ?? string.Empty;

            int caret = textBoxList.SelectionStart;
            if (caret < 0) caret = 0;
            if (caret > all.Length) caret = all.Length;

            int prevNl = -1;
            if (caret > 0)
                prevNl = all.LastIndexOf('\n', caret - 1);

            lineStart = (prevNl < 0) ? 0 : prevNl + 1;

            int nextNl = all.IndexOf('\n', lineStart);
            int lineEnd = (nextNl < 0) ? all.Length : nextNl;

            int len = Math.Max(0, lineEnd - lineStart);
            lineText = (len > 0) ? all.Substring(lineStart, len) : string.Empty;

            caretInLine = caret - lineStart;
            if (caretInLine < 0) caretInLine = 0;
            if (caretInLine > lineText.Length) caretInLine = lineText.Length;

            isFirstLine = (lineStart == 0);
        }
    }
}
