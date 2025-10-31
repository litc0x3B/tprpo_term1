// using System;
// using System.Diagnostics;
// using System.IO;
// using System.Runtime.InteropServices;
// using System.Threading;
// using NUnit.Framework;
// using System.Windows.Automation;

// namespace LabTemplateFSM.UiaTests
// {
//     [TestFixture]
//     [Apartment(ApartmentState.STA)] // UIA требует STA
//     public class UiAutomationTests
//     {
//         private Process _proc;
//         private AutomationElement _main;
//         private AutomationElement _textBox;

//         // подстройте путь под свой билд
//         private string AppPath =>
//             Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
//                 @"..\..\..\..\LabTemplateFSM\bin\Debug\net8.0-windows\LabTemplateFSM.exe"));

//         [SetUp]
//         public void SetUp()
//         {
//             Assert.That(File.Exists(AppPath), $"exe не найден: {AppPath}");
//             _proc = Process.Start(new ProcessStartInfo(AppPath) { UseShellExecute = false });
//             Assert.NotNull(_proc);

//             _main = WaitForMainWindow(_proc, TimeSpan.FromSeconds(5));
//             Assert.NotNull(_main, "Главное окно не найдено");

//             // Ищем наш textbox по AutomationId (Name в дизайнере = textBoxList)
//             _textBox = _main.FindFirst(TreeScope.Descendants,
//                 new PropertyCondition(AutomationElement.AutomationIdProperty, "textBoxList"));
//             if (_textBox == null)
//             {
//                 // запасной вариант: первый Edit
//                 _textBox = _main.FindFirst(TreeScope.Descendants,
//                     new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit));
//             }
//             Assert.NotNull(_textBox, "textBoxList не найден");

//             _textBox.SetFocus();
//             WaitInputQuiet();
//             ClearAll();
//         }

//         [TearDown]
//         public void TearDown()
//         {
//             try { if (!_proc?.HasExited ?? false) _proc.Kill(); } catch { /* ignore */ }
//         }

//         // ---------- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ----------

//         private static AutomationElement WaitForMainWindow(Process p, TimeSpan timeout)
//         {
//             var sw = Stopwatch.StartNew();
//             while (sw.Elapsed < timeout)
//             {
//                 if (p.MainWindowHandle != IntPtr.Zero)
//                 {
//                     var el = AutomationElement.FromHandle(p.MainWindowHandle);
//                     if (el != null) return el;
//                 }
//                 Thread.Sleep(50);
//             }
//             return null;
//         }

//         private static void WaitInputQuiet() => Thread.Sleep(40);

//         private static string NormalizeNL(string s) => s?.Replace("\r\n", "\n") ?? string.Empty;

//         private string ReadTextBox()
//         {
//             // WinForms TextBox обычно поддерживает ValuePattern
//             if (_textBox.TryGetCurrentPattern(ValuePattern.Pattern, out object pat))
//             {
//                 return ((ValuePattern)pat).Current.Value;
//             }
//             // на всякий случай — TextPattern
//             if (_textBox.TryGetCurrentPattern(TextPattern.Pattern, out object tpat))
//             {
//                 var range = ((TextPattern)tpat).DocumentRange;
//                 return range.GetText(-1);
//             }
//             throw new InvalidOperationException("Элемент не поддерживает Value/TextPattern");
//         }

//         private void SetFocusTextBox()
//         {
//             _textBox.SetFocus();
//             WaitInputQuiet();
//         }

//         private void ClearAll()
//         {
//             SetFocusTextBox();
//             // Ctrl+A, Delete
//             KeyDown(VK_CONTROL); PressKey('A'); KeyUp(VK_CONTROL);
//             PressKey(VK_DELETE);
//             WaitInputQuiet();
//         }

//         private void TypeText(string s)
//         {
//             SetFocusTextBox();
//             foreach (var ch in s)
//             {
//                 if (ch == '\n') PressKey(VK_RETURN);
//                 else SendUnicodeChar(ch);
//                 WaitInputQuiet();
//             }
//         }

//         private void PressEnter() => TypeText("\n");

//         // ---------- Win32 SendInput для реальных клавиш ----------

//         private const uint INPUT_KEYBOARD = 1;
//         private const uint KEYEVENTF_KEYUP = 0x0002;
//         private const uint KEYEVENTF_UNICODE = 0x0004;

//         private const ushort VK_RETURN = 0x0D;
//         private const ushort VK_DELETE = 0x2E;
//         private const ushort VK_CONTROL = 0x11;

//         [StructLayout(LayoutKind.Sequential)]
//         private struct INPUT { public uint type; public InputUnion U; }

//         [StructLayout(LayoutKind.Explicit)]
//         private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }

//         [StructLayout(LayoutKind.Sequential)]
//         private struct KEYBDINPUT
//         {
//             public ushort wVk;
//             public ushort wScan;
//             public uint dwFlags;
//             public uint time;
//             public IntPtr dwExtraInfo;
//         }

//         [DllImport("user32.dll", SetLastError = true)]
//         private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

//         private static void SendUnicodeChar(char ch)
//         {
//             var down = new INPUT
//             {
//                 type = INPUT_KEYBOARD,
//                 U = new InputUnion
//                 {
//                     ki = new KEYBDINPUT { wVk = 0, wScan = ch, dwFlags = KEYEVENTF_UNICODE }
//                 }
//             };
//             var up = down; up.U.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
//             SendInput(2, new[] { down, up }, Marshal.SizeOf(typeof(INPUT)));
//         }

//         private static void PressKey(ushort vk)
//         {
//             KeyDown(vk); KeyUp(vk);
//         }

//         private static void KeyDown(ushort vk)
//         {
//             var input = new INPUT
//             {
//                 type = INPUT_KEYBOARD,
//                 U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = 0 } }
//             };
//             SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
//         }

//         private static void KeyUp(ushort vk)
//         {
//             var input = new INPUT
//             {
//                 type = INPUT_KEYBOARD,
//                 U = new InputUnion { ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = KEYEVENTF_KEYUP } }
//             };
//             SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
//         }

//         private static void PressKey(char letter)
//         {
//             // простой перевод char->VK: подойдёт для латиницы; для кириллицы — используйте SendUnicodeChar
//             SendUnicodeChar(letter);
//         }

//         // =========================
//         // ТЕСТЫ (примерно те же кейсы)
//         // =========================

//         // 1) Разрешение только допустимых символов
//         [Test]
//         public void OnlyAllowedSymbols_AreAccepted()
//         {
//             TypeText("Aa+ A.A.");
//             PressEnter(); // ко 2-й строке

//             TypeText("иванов");
//             TypeText(".,1"); // недопустимые, не должны появиться
//             TypeText(" ");
//             TypeText("ии");
//             WaitInputQuiet();

//             var text = NormalizeNL(ReadTextBox());
//             StringAssert.StartsWith("Aa+ A.A.\nИванов И.И.\n", text);
//             StringAssert.DoesNotContain("1", text);
//             StringAssert.DoesNotContain(",", text);
//             // в фамилии не должно быть точек
//             var secondLine = text.Split('\n')[1];
//             Assert.That(secondLine.StartsWith("Иванов "), "Фамилия не должна содержать '.'");
//         }

//         [Test]
//         public void Enter_IsBlocked_UntilTemplateCompleted()
//         {
//             TypeText("Aa+ A.A.");
//             PressEnter();

//             TypeText("Иван");
//             var before = NormalizeNL(ReadTextBox());
//             PressEnter(); // ещё рано
//             var after = NormalizeNL(ReadTextBox());
//             Assert.That(after, Is.EqualTo(before), "Enter до завершения шаблона должен блокироваться");
//         }

//         // 2) Автодополнение (и отсутствие при неоднозначности)
//         [Test]
//         public void Autofill_Literals_And_NewLine_OnlyWhenUnique()
//         {
//             TypeText("Aa+ A.A.");
//             PressEnter();

//             TypeText("иванов и");
//             WaitInputQuiet();

//             var t1 = NormalizeNL(ReadTextBox());
//             StringAssert.Contains("Иванов ", t1);
//             StringAssert.Contains("И.", t1);

//             TypeText("и");
//             WaitInputQuiet();

//             var t2 = NormalizeNL(ReadTextBox());
//             StringAssert.Contains("И.И.", t2);
//             StringAssert.Contains("И.И.\n", t2); // NewLine в конце строки
//         }

//         [Test]
//         public void NoAutofill_WhenAmbiguous_LetterVsSpace()
//         {
//             ClearAll();
//             TypeText("Aa* A.A.");
//             PressEnter();

//             TypeText("Иван");
//             WaitInputQuiet();

//             var t = NormalizeNL(ReadTextBox());
//             Assert.That(t.EndsWith("Иван"), "При неоднозначности пробел не автоподставляется");
//         }

//         // 3) Корректировка регистра
//         [Test]
//         public void CaseCorrection_Works_ForSurnameAndInitials()
//         {
//             TypeText("Aa+ A.A.");
//             PressEnter();

//             TypeText("иВАНОв ии");
//             WaitInputQuiet();

//             var text = NormalizeNL(ReadTextBox());
//             var line2 = text.Split('\n')[1];
//             Assert.That(line2.StartsWith("Иванов "), "Ожидали 'Иванов '");
//             StringAssert.Contains(" И.И.", line2);
//         }

//         [Test]
//         public void CaseCorrection_DoubleHyphenSurname()
//         {
//             ClearAll();
//             TypeText("Aa*-Aa* A.A.");
//             PressEnter();

//             TypeText("сАЛТЫКОВ-ЩеДРиН ме");
//             WaitInputQuiet();

//             var text = NormalizeNL(ReadTextBox());
//             var line2 = text.Split('\n')[1];

//             StringAssert.StartsWith("Салтыков-Щедрин ", line2);
//             StringAssert.Contains(" М.Е.", line2);
//         }
//     }
// }
