using System;
using System.Drawing;
using System.Windows.Forms;

namespace RippleServerSwitcher
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            versionLabel.Text = String.Format("v{0}", Program.Version);
            label1.Font = new Font(label1.Font.FontFamily, label1.Font.Size,FontStyle.Strikeout);
        }

        private void closeButton_Click(object sender, EventArgs e) => Close();

        private void genuineTheme1_Click(object sender, EventArgs e)
        {
            // Hello Ripple, i love you <3
        }
    }
}
