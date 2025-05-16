using System;
using System.Windows.Forms;

namespace PasteTool
{
    public partial class NotifyForm : Form
    {
        public NotifyForm(string message)
        {
            InitializeComponent();
            this.labelMessage.Text = message;
            this.TopMost = true; // Đảm bảo Form này luôn nổi trên tất cả ứng dụng khác
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}