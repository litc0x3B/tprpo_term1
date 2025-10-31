// using System;
// using System.Reflection;

// namespace LabTemplateFSM.Tests
// {
//     public class Form1Proxy : Form1
//     {
//         readonly MethodInfo _buildNfa;
//         readonly MethodInfo _computeAllowed;

//         public Form1Proxy(string pattern)
//         {
//             _buildNfa = typeof(Form1).GetMethod("BuildNfa",
//                 BindingFlags.NonPublic | BindingFlags.Instance);
//             _computeAllowed = typeof(Form1).GetMethod("ComputeAllowedAt",
//                 BindingFlags.NonPublic | BindingFlags.Instance);

//             _buildNfa?.Invoke(this, new object[] { pattern });
//         }

//         public (bool Upper, bool Lower, bool Hyphen, bool Space, bool Dot, bool HasAny, int CountLiteral)
//             AllowedAt(string line)
//         {
//             dynamic a = _computeAllowed.Invoke(this, new object[] { line, line.Length });
//             return (a.Upper, a.Lower, a.Hyphen, a.Space, a.Dot, a.HasAny, a.CountLiteral);
//         }

//         // Имитируем TextBoxList_KeyPress логику для одного символа
//         public (bool accepted, char output) TryTypeChar(string prefix, char ch)
//         {
//             var a = AllowedAt(prefix);

//             if (char.IsLetter(ch))
//             {
//                 if (a.Upper && !a.Lower) return (true, char.ToUpper(ch));
//                 if (a.Lower && !a.Upper) return (true, char.ToLower(ch));
//                 if (a.Upper && a.Lower) return (true, ch);
//                 return (false, default);
//             }
//             else
//             {
//                 bool ok = (ch == '-' && a.Hyphen) || (ch == ' ' && a.Space) || (ch == '.' && a.Dot);
//                 return ok ? (true, ch) : (false, default);
//             }
//         }
//     }
// }
