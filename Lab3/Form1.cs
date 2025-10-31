using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Lab3
{

    public partial class Form1 : Form
    {
        MiniList<CheckBox> miniList;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {

        }


        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private bool _isBusy = false;
        private string _lastStatusString = "";

        private bool CheckNotBusyAndSet(string statusString)
        {
            bool flag = CheckNotBusy();
            if (flag)
            {
                _lastStatusString = statusString;
                toolStripStatusLabel1.Text = statusString;
                Application.DoEvents();
                _isBusy = true;
            }
            return flag;
        }

        private void addListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckNotBusyAndSet("Создание списка")) return;
            miniList = new MiniList<CheckBox>();
            panel1.Controls.Clear();
            toolStripStatusLabel1.Text = "Список создан.";
            _isBusy = false;
        }

        private bool CheckNotBusy()
        {
            if (_isBusy)
            {
                toolStripStatusLabel1.Text = $"Дождитесь завершения комманды: {_lastStatusString}";
                toolStripStatusLabel1.ForeColor = Color.Red;
                Application.DoEvents();
                SystemSounds.Exclamation.Play();
                System.Threading.Thread.Sleep(100);
                toolStripStatusLabel1.ForeColor = SystemColors.ControlText;
                Application.DoEvents();
                return false;
            }
            return true;
        }

        private void addElementToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckNotBusyAndSet("Добавление кнопки")) return;

            if (miniList != null)
            {
                CheckBox checkBox = new CheckBox();
                checkBox.Appearance = Appearance.Button;
                checkBox.FlatStyle = FlatStyle.Standard;
                checkBox.Top = 50;
                checkBox.Width = 50;
                checkBox.Left = 20 + checkBox.Width * miniList.Append(checkBox);
                checkBox.Text = Convert.ToString(checkBox.Left);
                this.panel1.Controls.Add(checkBox);
                toolStripStatusLabel1.Text = "Добавлен элемент.";
            }
            else
            {
                toolStripStatusLabel1.Text = "Список не создан.";
            }
            _isBusy = false;
             
        }

        private void buttonEnumToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (!CheckNotBusyAndSet("Перебор кнопок")) return;

            if (panel1.Controls.Count == 0)
            {
                toolStripStatusLabel1.Text = "На панели нет элементов.";
                _isBusy = false;
                return;
            }

            foreach (var cb in panel1.Controls.OfType<CheckBox>().OrderBy(c => c.Left))
            {
                var old = cb.BackColor;
                cb.Checked = true;
                //toolStripStatusLabel1.Text = $"Перебор кнопок: значение Left={cb.Left}";
                Application.DoEvents();
                System.Threading.Thread.Sleep(160);
                cb.Checked = false;
            }
            toolStripStatusLabel1.Text = "Перебор кнопок завершён.";
            _isBusy = false;
        }

        private void objectEnumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!CheckNotBusyAndSet("Перебор объектов")) return;

            if (miniList == null || miniList.Top == null)
            {
                toolStripStatusLabel1.Text = "Список пуст.";
                _isBusy = false;
                return;
            }

            foreach (var cb in miniList)
            {
                var old = cb.BackColor;
                cb.Checked = true;
                //toolStripStatusLabel1.Text = $"Перебор объектов списка: значение Left={cb.Left}";
                Application.DoEvents(); 
                System.Threading.Thread.Sleep(160);
                cb.Checked = false;
            }

            toolStripStatusLabel1.Text = $"Перебор объектов списка завершён.";
            _isBusy = false;
        }
    }

    class MiniList<T> : IEnumerable<T>
    {
        int num = 0;
        public Node Top;
        public class Node
        {
            public T Content;
            public Node Next = null;
        }
        public int Append(T s)
        {
            Node p = new Node();
            p.Content = s;
            if (Top != null) p.Next = Top;
            Top = p;
            return num++;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new MiniEnumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    class MiniEnumerator<T> : IEnumerator<T>
    {
        private readonly MiniList<T> _list;
        private MiniList<T>.Node _beforeFirst;
        private MiniList<T>.Node _current;
        private bool _disposed;

        public MiniEnumerator(MiniList<T> list)
        {
            _list = list ?? throw new ArgumentNullException(nameof(list));
            _beforeFirst = new MiniList<T>.Node { Next = _list.Top };
            _current = null;
        }

        public T Current
        {
            get
            {
                if (_current == null) throw new InvalidOperationException("Enumeration has not started.");
                return _current.Content;
            }
        }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MiniEnumerator<T>));

            if (_current == null)
            {
                _current = _beforeFirst.Next;
            }
            else
            {
                _current = _current.Next;
            }
            return _current != null;
        }

        public void Reset()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MiniEnumerator<T>));
            _current = null;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
