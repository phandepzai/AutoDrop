using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

namespace PasteTool
{
    public partial class nongvanphan : Form
    {
        // Danh sách các dòng văn bản sẽ được dán
        private List<string> lines;
        // Chỉ số dòng hiện tại đang được xử lý
        private int currentLineIndex;
        // Trạng thái đang thực hiện dán hay không
        private bool isPasting;
        // Trạng thái hoàn thành quá trình dán
        private bool isCompleted;
        // Tốc độ dán mặc định (250ms)
        private const int DEFAULT_PASTE_SPEED = 250;

        // Nhãn hiển thị đồng hồ
        private Label lbClock;
        // Timer để cập nhật đồng hồ
        private System.Windows.Forms.Timer timer;

        // Hiệu ứng cầu vồng tên tác giả
        // Timer để tạo hiệu ứng cầu vồng cho nhãn tên tác giả
        private System.Windows.Forms.Timer rainbowTimer;
        // Trạng thái hiệu ứng cầu vồng có đang bật hay không
        private bool isRainbowActive;
        // Màu gốc của nhãn tên tác giả
        private Color originalAuthorColor;
        // Giai đoạn để tính toán màu sắc cầu vồng
        private double rainbowPhase;

        // Màu nền mặc định của TextBox
        private Color defaultTextboxBackColor;

        // Sử dụng thư viện user32.dll để đăng ký và hủy phím nóng
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Định nghĩa ID cho phím nóng F1 và ESC
        private const int HOTKEY_F1 = 1;
        private const int HOTKEY_ESC = 2;

        // Hàm khởi tạo form
        public nongvanphan()
        {
            InitializeComponent();
            // Khởi tạo công cụ dán
            InitializePasteTool();
            // Khởi tạo đồng hồ
            InitializeClock();
            this.TopMost = true;//Luôn hiện trên các ứng dụng khác
            // Khởi tạo hiệu ứng cầu vồng cho tên tác giả
            rainbowTimer = new System.Windows.Forms.Timer();
            rainbowTimer.Interval = 100; // Cập nhật màu mỗi 100ms
            rainbowTimer.Tick += RainbowTimer_Tick; // Gắn sự kiện Tick
            isRainbowActive = false;
            originalAuthorColor = lbAuthor.ForeColor; // Lưu màu gốc của nhãn
            rainbowPhase = 0;
            lbAuthor.MouseEnter += LbAuthor_MouseEnter; // Sự kiện khi chuột di vào nhãn
            lbAuthor.MouseLeave += LbAuthor_MouseLeave; // Sự kiện khi chuột rời nhãn

            // Gắn sự kiện chỉ cho phép nhập số vào ô txtSpeed
            txtSpeed.KeyPress += TxtSpeed_KeyPress;

            // Lưu màu nền mặc định của TextBox
            defaultTextboxBackColor = txtTextbox.BackColor;
        }

        // Sự kiện Tick của timer cầu vồng, cập nhật màu sắc
        private void RainbowTimer_Tick(object sender, EventArgs e)
        {
            rainbowPhase += 0.1; // Tăng giai đoạn để thay đổi màu

            // Tính toán màu cầu vồng dựa trên giai đoạn
            Color newColor = CalculateRainbowColor(rainbowPhase);
            lbAuthor.ForeColor = newColor; // Áp dụng màu mới cho nhãn
        }

        // Tính toán màu sắc cầu vồng dựa trên giai đoạn
        private Color CalculateRainbowColor(double phase)
        {
            // Sử dụng hàm sin để tạo màu đỏ, xanh lá, xanh dương
            double red = Math.Sin(phase) * 127 + 128;
            double green = Math.Sin(phase + 2 * Math.PI / 3) * 127 + 128;
            double blue = Math.Sin(phase + 4 * Math.PI / 3) * 127 + 128;

            // Đảm bảo giá trị màu nằm trong khoảng 0-255
            red = Math.Max(0, Math.Min(255, red));
            green = Math.Max(0, Math.Min(255, green));
            blue = Math.Max(0, Math.Min(255, blue));

            return Color.FromArgb((int)red, (int)green, (int)blue);
        }

        // Khi chuột di vào nhãn tên tác giả, kích hoạt hiệu ứng cầu vồng
        private void LbAuthor_MouseEnter(object sender, EventArgs e)
        {
            if (!isRainbowActive)
            {
                isRainbowActive = true;
                originalAuthorColor = lbAuthor.ForeColor; // Lưu màu gốc
                rainbowTimer.Start(); // Bắt đầu timer
            }
        }

        // Khi chuột rời nhãn, tắt hiệu ứng và khôi phục màu gốc
        private void LbAuthor_MouseLeave(object sender, EventArgs e)
        {
            if (isRainbowActive)
            {
                isRainbowActive = false;
                rainbowTimer.Stop(); // Dừng timer
                lbAuthor.ForeColor = originalAuthorColor; // Khôi phục màu gốc
            }
        }

        // Khởi tạo đồng hồ hiển thị thời gian
        private void InitializeClock()
        {
            lbClock = new Label();
            lbClock.AutoSize = true;
            lbClock.Font = new Font("Arial", 11, FontStyle.Bold);
            lbClock.ForeColor = Color.FromArgb(255,69,0);
            lbClock.BackColor = Color.Transparent; // Nền trong suốt
            lbClock.TextAlign = ContentAlignment.MiddleCenter;
            // Đặt vị trí đồng hồ phía trên nút START
            lbClock.Location = new Point(btnSTART.Left +5, 25);
            lbClock.Size = new Size(btnRESET.Right - btnSTART.Left, 25);
            this.Controls.Add(lbClock); // Thêm đồng hồ vào form

            timer = new System.Windows.Forms.Timer();
            timer.Interval = 500; // Cập nhật mỗi 0.5 giây
            timer.Tick += Timer_Tick;
            timer.Start();

            UpdateClock(); // Cập nhật thời gian ngay khi khởi tạo
        }

        // Sự kiện Tick của timer đồng hồ
        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateClock(); // Cập nhật thời gian
        }

        // Cập nhật thời gian theo múi giờ GMT+7
        private void UpdateClock()
        {
            TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            DateTime vnTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            lbClock.Text = vnTime.ToString("HH:mm:ss"); // Hiển thị giờ:phút:giây
        }

        // Khởi tạo các biến và trạng thái cho công cụ dán
        private void InitializePasteTool()
        {
            lines = new List<string>(); // Khởi tạo danh sách dòng
            currentLineIndex = 0; // Đặt chỉ số dòng về 0
            isPasting = false; // Không dán ban đầu
            isCompleted = false; // Chưa hoàn thành
            UpdateLineCountLabel(); // Cập nhật nhãn số dòng
        }

        // Sự kiện khi nhấn nút START
        private void btnSTART_Click(object sender, EventArgs e)
        {
            txtTextbox.ReadOnly = true; // Khóa TextBox
            txtTextbox.BackColor = Color.LightYellow; // Đặt màu nền vàng nhạt
            btnSTART.Enabled = false; // Vô hiệu hóa nút START
            btnSTOP.Enabled = true; // Kích hoạt nút STOP
            btnRESET.Enabled = true; // Kích hoạt nút RESET
            txtSpeed.Enabled = true; // Cho phép chỉnh tốc độ
            isPasting = false;

            // Đăng ký phím nóng F1 và ESC
            RegisterHotKey(this.Handle, HOTKEY_F1, 0, (int)Keys.F1);
            RegisterHotKey(this.Handle, HOTKEY_ESC, 0, (int)Keys.Escape);

            // *** Thêm dòng này để cập nhật trạng thái ***
            lbStatus.Text = "Sẵn sàng";
            lbStatus.ForeColor = Color.Green; // Tùy chọn: Đặt màu cho trạng thái
            lbStatus.TextAlign = ContentAlignment.MiddleCenter;
            lbStatus.BackColor = Color.Transparent;
        }

        // Sự kiện khi nhấn nút STOP
        private void btnSTOP_Click(object sender, EventArgs e)
        {
            StopPasting(false); // Dừng dán mà không đánh dấu hoàn thành
            txtTextbox.ReadOnly = false; // Mở khóa TextBox
            txtTextbox.BackColor = defaultTextboxBackColor; // Khôi phục màu nền mặc định
            btnSTART.Enabled = true; // Kích hoạt nút START

            // *** Thêm dòng này để cập nhật trạng thái ***
            lbStatus.Text = "Đã tạm dừng\nvà mở khóa";
            lbStatus.ForeColor = Color.Red; // Tùy chọn: Đặt màu cho trạng thái
            lbStatus.TextAlign = ContentAlignment.MiddleCenter;
            lbStatus.BackColor = Color.Transparent;
        }

        // Sự kiện khi nhấn nút RESET
        private void btnRESET_Click(object sender, EventArgs e)
        {
            ResetForm(); // Đặt lại toàn bộ form
        }

        // Đặt lại form về trạng thái ban đầu
        private void ResetForm()
        {
            txtTextbox.Clear(); // Xóa nội dung TextBox
            txtTextbox.ReadOnly = false; // Mở khóa TextBox
            txtTextbox.BackColor = defaultTextboxBackColor; // Khôi phục màu nền mặc định
            btnSTART.Enabled = true; // Kích hoạt nút START
            btnSTOP.Enabled = false; // Vô hiệu hóa nút STOP
            btnRESET.Enabled = true; // Kích hoạt nút RESET

            // Hủy đăng ký phím nóng
            UnregisterHotKey(this.Handle, HOTKEY_F1);
            UnregisterHotKey(this.Handle, HOTKEY_ESC);
            InitializePasteTool(); // Khởi tạo lại công cụ dán

            // *** Thêm dòng này để cập nhật trạng thái ***
            lbStatus.Text = "RESET";
            lbStatus.ForeColor = Color.Peru; // Tùy chọn: Đặt màu cho trạng thái
            lbStatus.TextAlign = ContentAlignment.MiddleCenter; //Căn lề chữ ở giữa
            lbStatus.BackColor = Color.Transparent;
        }

        // Bắt đầu quá trình dán
        private async Task StartPasting()
        {
            // Kiểm tra nếu TextBox trống
            if (string.IsNullOrEmpty(txtTextbox.Text))
            {
                MessageBox.Show("Vui lòng nhập dữ liệu trước khi bắt đầu dán.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Tách nội dung TextBox thành danh sách các dòng
            lines = txtTextbox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();
            currentLineIndex = 0; // Đặt lại chỉ số dòng
            isPasting = true; // Bắt đầu dán
            isCompleted = false; // Chưa hoàn thành
            await PasteLinesAsync(); // Thay đổi: Thêm 'await' trước lời gọi
        }

        // Hàm dán các dòng văn bản bất đồng bộ
        private async Task PasteLinesAsync()
        {
            int pasteSpeed = DEFAULT_PASTE_SPEED; // Tốc độ dán mặc định
            if (!string.IsNullOrEmpty(txtSpeed.Text))
            {
                // Kiểm tra giá trị tốc độ hợp lệ
                if (!int.TryParse(txtSpeed.Text, out pasteSpeed))
                {
                    pasteSpeed = DEFAULT_PASTE_SPEED;
                    MessageBox.Show("Tốc độ không hợp lệ. Sử dụng tốc độ mặc định " + DEFAULT_PASTE_SPEED + "ms.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtSpeed.Text = "";
                }
            }

            if (!string.IsNullOrEmpty(txtTextbox.Text))
            {
                // Lọc các dòng không rỗng từ TextBox
                lines = txtTextbox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                                .Where(line => !string.IsNullOrEmpty(line))
                                .ToList();

                if (lines != null && lines.Count > 0)
                {
                    try
                    {
                        // Lặp qua từng dòng để dán
                        while (isPasting && currentLineIndex < lines.Count)
                        {
                            Clipboard.SetText(lines[currentLineIndex]); // Đặt dòng vào clipboard
                            await Task.Delay(50); // Độ trễ sau copy
                            
                            SendKeys.SendWait("^{v}"); // Dán (Ctrl + V)
                            await Task.Delay(50); // Độ trễ sau paste
                            
                            SendKeys.SendWait("{ENTER}"); // Nhấn Enter (nếu muốn có thêm delay thì thêm tiếp dòng dưới)
                            await Task.Delay(50); // Độ trễ sau enter (nếu cần)
                            
                            currentLineIndex++; // Tăng chỉ số dòng
                            UpdateLineCountLabel(); // Cập nhật nhãn số dòng
                            await Task.Delay(pasteSpeed); // Chờ theo tốc độ cài đặt
                        }

                        // Kiểm tra nếu đã dán hết các dòng
                        if (isPasting && currentLineIndex == lines.Count)
                        {
                            isCompleted = true;
                        }

                        StopPasting(isCompleted); // Dừng dán
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Đã xảy ra lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Vui lòng nhập dữ liệu trước khi bắt đầu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Dừng quá trình dán
        private void StopPasting(bool completed)
        {
            isPasting = false; // Dừng dán
            txtTextbox.ReadOnly = true; // Khóa TextBox
            btnSTART.Enabled = false; // Vô hiệu hóa nút START
            btnSTOP.Enabled = true; // Kích hoạt nút STOP
            btnRESET.Enabled = true; // Kích hoạt nút RESET

            // Nếu hoàn thành, thông báo
            if (completed)
            {
                SendKeys.SendWait("{ENTER}"); // Nhấn Enter
                Thread.Sleep(500); // Chờ 0.5 giây
                MessageBox.Show($"Đã hoàn thành {currentLineIndex} dòng !", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            // *** Thêm dòng này để cập nhật trạng thái khi hoàn thành ***
            lbStatus.Text = $"Hoàn thành dán\n{currentLineIndex} dòng!";
            lbStatus.ForeColor = Color.Green; // Tùy chọn: Đặt màu cho trạng thái
            lbStatus.TextAlign = ContentAlignment.MiddleCenter; //Căn lề chữ ở giữa
            lbStatus.BackColor = Color.Transparent;
        }

        // Cập nhật nhãn hiển thị số dòng
        private void UpdateLineCountLabel()
        {
            // Đếm số dòng không rỗng
            int lineCount = txtTextbox.Lines.Count(line => !string.IsNullOrWhiteSpace(line));
            lbCount.Text = $"Line: {lineCount}";
            lbCount.BackColor = Color.Transparent;

            // Nếu đang dán, hiển thị tiến độ
            if (isPasting)
            {
                lbCount.Text += $"     (Hoàn thành: {currentLineIndex}/{lines.Count})";
                lbCount.BackColor = Color.Transparent;
            }
            UpdateScrollbarVisibility(); // Cập nhật thanh cuộn
        }

        // Sự kiện khi form được tải
        private void Form1_Load(object sender, EventArgs e)
        {
            btnSTOP.Enabled = false; // Vô hiệu hóa nút STOP
            txtTextbox.TextChanged += txtTextbox_TextChanged; // Gắn sự kiện thay đổi TextBox
            UpdateLineCountLabel(); // Cập nhật nhãn số dòng
            UpdateScrollbarVisibility(); // Cập nhật thanh cuộn
            // Label trạng thái
            lbStatus.Text = "Vui lòng nhập\ndữ liệu và\nbấm START"; // Trạng thái ban đầu
            lbStatus.ForeColor = Color.DimGray; // Tùy chọn: Đặt màu cho trạng thái ban đầu
            lbStatus.TextAlign = ContentAlignment.MiddleCenter; //Căn lề chữ ở giữa
            lbStatus.BackColor = Color.Transparent;
            // *** Thêm dòng này để đặt tiêu đề của Form ***
            DateTime today = DateTime.Now;
            string formattedDate = today.ToString("dd/MM/yyyy");
            this.Text = "FAB " + formattedDate; // 'this' tham chiếu đến Form hiện tại
        }

        // Xử lý phím nóng
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x0312) // Tin nhắn phím nóng
            {
                int id = m.WParam.ToInt32();
                switch (id)
                {
                    case HOTKEY_F1:
                        // Nếu không dán và TextBox bị khóa, bắt đầu dán
                        if (!isPasting && txtTextbox.ReadOnly)
                        {
                            _ = StartPasting();
                        }
                        break;
                    case HOTKEY_ESC:
                        // Nếu đang dán, dừng lại
                        if (isPasting)
                        {
                            StopPasting(false);
                            isCompleted = false;
                        }
                        break;
                }
            }
        }

        // Khi form đóng, hủy đăng ký phím nóng
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, HOTKEY_F1);
            UnregisterHotKey(this.Handle, HOTKEY_ESC);
        }

        // Sự kiện khi nội dung TextBox thay đổi
        private void txtTextbox_TextChanged(object sender, EventArgs e)
        {
            UpdateLineCountLabel(); // Cập nhật nhãn số dòng
            txtTextbox.SelectionStart = txtTextbox.Text.Length; // Đặt con trỏ ở cuối
            txtTextbox.ScrollToCaret(); // Cuộn đến vị trí con trỏ
        }

        // Cập nhật hiển thị thanh cuộn
        private void UpdateScrollbarVisibility()
        {
            // Kiểm tra nếu nội dung vượt quá chiều cao TextBox
            Size textSize = txtTextbox.GetPreferredSize(new Size(txtTextbox.Width, 0));
            if (textSize.Height > txtTextbox.ClientSize.Height)
            {
                txtTextbox.ScrollBars = ScrollBars.Vertical; // Hiển thị thanh cuộn dọc
            }
            else
            {
                txtTextbox.ScrollBars = ScrollBars.None; // Ẩn thanh cuộn
            }

            // Kiểm tra nếu nội dung vượt quá chiều rộng TextBox
            Size maxLineWidth = CalculateMaxLineWidth();
            if (maxLineWidth.Width > txtTextbox.ClientSize.Width)
            {
                txtTextbox.ScrollBars |= ScrollBars.Horizontal; // Hiển thị thanh cuộn ngang
            }
            else
            {
                txtTextbox.ScrollBars &= ~ScrollBars.Horizontal; // Ẩn thanh cuộn ngang
                if (txtTextbox.ScrollBars == ScrollBars.None)
                {
                    txtTextbox.ScrollBars = ScrollBars.None;
                }
                else
                {
                    txtTextbox.ScrollBars = ScrollBars.Vertical;
                }
            }
        }

        // Tính toán chiều rộng lớn nhất của các dòng
        private Size CalculateMaxLineWidth()
        {
            int maxWidth = 0;
            using (Graphics g = txtTextbox.CreateGraphics())
            {
                foreach (string line in txtTextbox.Lines)
                {
                    SizeF lineSize = g.MeasureString(line, txtTextbox.Font);
                    if (lineSize.Width > maxWidth)
                    {
                        maxWidth = (int)lineSize.Width; // Cập nhật chiều rộng lớn nhất
                    }
                }
            }
            return new Size(maxWidth, 0);
        }

        // Chỉ cho phép nhập số vào ô txtSpeed
        private void TxtSpeed_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
            {
                e.Handled = true; // Chặn ký tự không phải số
            }
        }

        // Khi form thay đổi kích thước, cập nhật thanh cuộn
        private void Form1_Resize(object sender, EventArgs e)
        {
            UpdateScrollbarVisibility();
        }
    }
}
