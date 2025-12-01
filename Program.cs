using System;
using System.Threading;
using System.Windows.Forms;
using FormsApp = System.Windows.Forms.Application;

namespace AutoDrop
{
    internal static class Program
    {
        /// <summary>
        /// Điểm vào chính của ứng dụng.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Tạo Mutex với tên unique (global)
            using (Mutex mutex = new Mutex(true, "AutoDropSingleInstanceMutex", out bool createdNew))
            {
                if (createdNew)
                {
                    // Nếu Mutex mới được tạo (chưa có instance nào chạy), tiếp tục chạy ứng dụng
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    FormsApp.Run(new MainForm());
                }
                else
                {
                    // Nếu Mutex đã tồn tại (instance khác đang chạy), hiển thị thông báo và thoát
                    MessageBox.Show(
                        "Ứng dụng đã đang chạy. Không thể mở thêm phiên bản mới.",
                        "Thông báo",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    // Thoát ứng dụng ngay lập tức
                    Environment.Exit(0);
                }
            }
        }
    }
}
