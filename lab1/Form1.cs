using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace LabTemplateFSM
{
    public partial class Form1 : Form
    {
        enum SymKind { Upper, Lower, Hyphen, Space, Dot }
        sealed class Trans { public SymKind Kind; public int To; }
        sealed class State
        {
            public List<Trans> Edges = new List<Trans>();
            public List<int> Eps = new List<int>();
            public bool Accepting;
        }

        List<State> nfa = new List<State>();
        int startState = 0;
        string cachedPattern = null;
        bool inAutoFill = false;

        public Form1()
        {
            InitializeComponent();
            textBoxList.KeyPress += TextBoxList_KeyPress;
            textBoxList.KeyUp += TextBoxList_KeyUp;
            textBoxList.TextChanged += TextBoxList_TextChanged;
        }

        // NFA build
        void EnsureNfaUpToDate()
        {
            string pattern = GetFirstLine();
            if (pattern == cachedPattern) return;
            BuildNfa(pattern ?? string.Empty);
            cachedPattern = pattern;
        }

        string GetFirstLine()
        {
            var t = textBoxList.Text;
            if (string.IsNullOrEmpty(t)) return string.Empty;
            int nl = t.IndexOf('\n');
            return nl < 0 ? t : t.Substring(0, nl);
        }

        SymKind? CharToKind(char c)
        {
            if (c == '-') return SymKind.Hyphen;
            if (c == ' ') return SymKind.Space;
            if (c == '.') return SymKind.Dot;
            if (c == 'A') return SymKind.Upper;
            if (c == 'a') return SymKind.Lower;
            return null;
        }

        void BuildNfa(string pattern)
        {
            nfa = new List<State>();
            int NewState() { nfa.Add(new State()); return nfa.Count - 1; }

            int s0 = NewState();
            startState = s0;
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
                            int s2 = NewState();
                            nfa[curr].Edges.Add(new Trans { Kind = kind.Value, To = s2 });
                            curr = s2;
                            break;
                        }
                    case '+':
                        {
                            int s2 = NewState();
                            nfa[curr].Edges.Add(new Trans { Kind = kind.Value, To = s2 });
                            nfa[s2].Edges.Add(new Trans { Kind = kind.Value, To = s2 });
                            curr = s2;
                            break;
                        }
                    case '*':
                        {
                            nfa[curr].Edges.Add(new Trans { Kind = kind.Value, To = curr });
                            break;
                        }
                    case '?':
                        {
                            int s2 = NewState();
                            nfa[curr].Edges.Add(new Trans { Kind = kind.Value, To = s2 });
                            nfa[curr].Eps.Add(s2);
                            curr = s2;
                            break;
                        }
                }
            }
            nfa[curr].Accepting = true;
        }

        // NFA run
        struct Allowed
        {
            public bool Upper, Lower, Hyphen, Space, Dot;
            public bool HasAny => Upper || Lower || Hyphen || Space || Dot;
            public int CountLiteral => (Hyphen ? 1 : 0) + (Space ? 1 : 0) + (Dot ? 1 : 0);
            public char TheOnlyLiteral()
            {
                if (CountLiteral != 1) throw new InvalidOperationException();
                if (Hyphen) return '-';
                if (Space) return ' ';
                return '.';
            }
        }

        List<int> EpsClosure(IEnumerable<int> states)
        {
            var stack = new Stack<int>(states);
            var seen = new HashSet<int>(states);
            while (stack.Count > 0)
            {
                int s = stack.Pop();
                foreach (var e in nfa[s].Eps)
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
                foreach (var tr in nfa[s].Edges)
                {
                    switch (tr.Kind)
                    {
                        case SymKind.Upper: if (isUpper) dest.Add(tr.To); break;
                        case SymKind.Lower: if (isLower) dest.Add(tr.To); break;
                        case SymKind.Hyphen: if (ch == '-') dest.Add(tr.To); break;
                        case SymKind.Space: if (ch == ' ') dest.Add(tr.To); break;
                        case SymKind.Dot: if (ch == '.') dest.Add(tr.To); break;
                    }
                }
            }
            return dest;
        }

        Allowed ComputeAllowedAt(string line, int pos)
        {
            var curr = EpsClosure(new[] { startState });
            for (int i = 0; i < pos; i++)
                curr = EpsClosure(Move(curr, line[i]));

            Allowed a = default;
            foreach (int s in curr)
                foreach (var tr in nfa[s].Edges)
                    switch (tr.Kind)
                    {
                        case SymKind.Upper: a.Upper = true; break;
                        case SymKind.Lower: a.Lower = true; break;
                        case SymKind.Hyphen: a.Hyphen = true; break;
                        case SymKind.Space: a.Space = true; break;
                        case SymKind.Dot: a.Dot = true; break;
                    }
            return a;
        }

        // Autofill
        void AutoFillDeterministicLiterals()
        {
            if (inAutoFill) return;
            try
            {
                inAutoFill = true;

                GetCurrentLine(out var lineStart, out var lineText, out var caretInLine, out var isFirstLine);
                if (isFirstLine) return;
                if (caretInLine != lineText.Length) return;

                for (int guard = 0; guard < 512; guard++)
                {
                    var allowed = ComputeAllowedAt(lineText, lineText.Length);
                    bool hasLetterChoice = allowed.Upper || allowed.Lower;
                    if (!hasLetterChoice && allowed.CountLiteral == 1)
                    {
                        char c = allowed.TheOnlyLiteral();
                        textBoxList.AppendText(c.ToString());
                        lineText += c;
                        continue;
                    }
                    break;
                }
            }
            finally { inAutoFill = false; }
        }

        // Input handling
        void TextBoxList_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return) return;
            if (char.IsControl(e.KeyChar)) return;

            GetCurrentLine(out int lineStart, out string lineText, out int caretInLine, out bool isFirstLine);

            if (isFirstLine)
            {
                char ch = e.KeyChar;
                bool ok = ch == 'A' || ch == 'a' || ch == '-' || ch == ' ' || ch == '.' || ch == '+' || ch == '*' || ch == '?';
                if (!ok) { e.Handled = true; return; }
                return;
            }

            if (textBoxList.SelectionLength != 0 || caretInLine != lineText.Length)
            {
                e.Handled = true;
                return;
            }

            var allowed = ComputeAllowedAt(lineText, caretInLine);
            char c = e.KeyChar;

            if (char.IsLetter(c))
            {
                if (allowed.Upper && !allowed.Lower) { e.KeyChar = char.ToUpper(c); return; }
                if (allowed.Lower && !allowed.Upper) { e.KeyChar = char.ToLower(c); return; }
                if (allowed.Upper && allowed.Lower) { return; }
                e.Handled = true; return;
            }
            else
            {
                bool ok =
                    (c == '-' && allowed.Hyphen) ||
                    (c == ' ' && allowed.Space) ||
                    (c == '.' && allowed.Dot);
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
            EnsureNfaUpToDate();
            GetCurrentLine(out _, out _, out _, out bool isFirstLine);
            if (!isFirstLine) AutoFillDeterministicLiterals();
        }

        // Helpers
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
