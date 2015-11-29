using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace CallLoggerApp
{
    public partial class MainForm : Form
    {
        delegate void OnAddLineToList(string text);

        public CallLogger.LogListenersContainer m_Container;
        
        public MainForm()
        {
            InitializeComponent();

            m_Container = new CallLogger.LogListenersContainer();
            //m_Container.OnNewEntry = new CallLogger.OnNewLogEntry(this.NewEntry);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            m_Container.Start();
        }

        private void AddLineToList(string aLine)
        {
            if (this.listBox1.InvokeRequired)
            {
                OnAddLineToList d = new OnAddLineToList(AddLineToList);
                this.Invoke(d, new object[] { aLine});
            }
            else
            {
                this.listBox1.Items.Insert(0, aLine);
            }
        }
        
        private void NewEntry(object sender, CallLogger.LogEntry e)
        {
            lock (this)
            {
                string line = e.ToString();
                AddLineToList(line);
            }
        }

        private void bttnClose_Click(object sender, EventArgs e)
        {
            m_Container.Stop(true);
            Thread.Sleep(1000);
            this.Close();
        }
    }
}
