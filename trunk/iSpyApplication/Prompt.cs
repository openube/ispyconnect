using System;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class Prompt : Form
    {
        public string Val;
        public Prompt()
        {
            InitializeComponent();
            button1.Text = LocRm.GetString("OK");

        }

        public Prompt(string label, string prefill)
        {
            InitializeComponent();
            Text = label;
            textBox1.Text = prefill;
        }

        public Prompt(string label, string prefill, bool isPassword)
        {
            InitializeComponent();
            Text = label;
            if (isPassword)
                textBox1.PasswordChar = '*';
            textBox1.Text = prefill;
            
        }

        public override sealed string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Val = textBox1.Text;
            Close();
        }

        private void Prompt_Load(object sender, EventArgs e)
        {

        }
    }
}
