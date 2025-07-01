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
        private List<string> lines; // Danh sách các dòng văn bản sẽ được dán
        // Chỉ số dòng hiện tại đang được xử lý
        private int currentLineIndex; // Chỉ số dòng hiện tại đang được xử lý
        // Trạng thái đang thực hiện dán hay không
        private bool isPasting; // Trạng thái đang thực hiện dán hay không
        // Trạng thái hoàn thành quá trình dán
        private bool isCompleted; // Trạng thái hoàn thành quá trình dán
        // Tốc độ dán mặc định (250ms)
        private const int DEFAULT_PASTE_SPEED = 250; // Tốc độ dán mặc định (250ms)

        // Nhãn hiển thị đồng hồ
        private Label lbClock; // Nhãn hiển thị đồng hồ
        // Timer để cập nhật đồng hồ
        private System.Windows.Forms.Timer timer; // Timer để cập nhật đồng hồ

        // Hiệu ứng cầu vồng tên tác giả
        // Timer để tạo hiệu ứng cầu vồng cho nhãn tên tác giả
        private System.Windows.Forms.Timer rainbowTimer; // Timer để tạo hiệu ứng cầu vồng cho nhãn tên tác giả
        // Trạng thái hiệu ứng cầu vồng có đang bật hay không
        private bool isRainbowActive; // Trạng thái hiệu ứng cầu vồng có đang bật hay không
        // Màu gốc của nhãn tên tác giả
        private Color originalAuthorColor; // Màu gốc của nhãn tên tác giả
        // Giai đoạn để tính toán màu sắc cầu vồng
        private double rainbowPhase; // Giai đoạn để tính toán màu sắc cầu vồng

        // Màu nền mặc định của TextBox
        private Color defaultTextboxBackColor; // Màu nền mặc định của TextBox

        // Sử dụng thư viện user32.dll để đăng ký và hủy phím nóng
        [DllImport("user32.dll")] // Import thư viện user32.dll
        [return: MarshalAs(UnmanagedType.Bool)] // Định nghĩa kiểu trả về
        static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk); // Khai báo hàm RegisterHotKey

        [DllImport("user32.dll")] // Import thư viện user32.dll
        [return: MarshalAs(UnmanagedType.Bool)] // Định nghĩa kiểu trả về
        static extern bool UnregisterHotKey(IntPtr hWnd, int id); // Khai báo hàm UnregisterHotKey

        // Định nghĩa ID cho phím nóng F1 và ESC
        private const int HOTKEY_F1 = 1; // Định nghĩa ID cho phím nóng F1
        private const int HOTKEY_ESC = 2; // Định nghĩa ID cho phím nóng ESC
        //Timer cho việc reset tiêu đề
        private System.Windows.Forms.Timer titleResetTimer; // Timer cho việc reset tiêu đề

        // Hàm khởi tạo form
        public nongvanphan() // Hàm khởi tạo form
        {
            InitializeComponent(); // Khởi tạo các thành phần của form
            // Khởi tạo công cụ dán
            InitializePasteTool(); // Khởi tạo công cụ dán
            // Khởi tạo đồng hồ
            InitializeClock(); // Khởi tạo đồng hồ
            //this.TopMost = true;//Luôn hiện trên các ứng dụng khác // Luôn hiện trên các ứng dụng khác
            // Khởi tạo hiệu ứng cầu vồng cho tên tác giả
            rainbowTimer = new System.Windows.Forms.Timer(); // Khởi tạo timer cầu vồng
            rainbowTimer.Interval = 100; // Cập nhật màu mỗi 100ms
            rainbowTimer.Tick += RainbowTimer_Tick; // Gắn sự kiện Tick
            isRainbowActive = false; // Đặt trạng thái hiệu ứng cầu vồng là không hoạt động
            originalAuthorColor = lbAuthor.ForeColor; // Lưu màu gốc của nhãn
            rainbowPhase = 0; // Đặt giai đoạn cầu vồng về 0
            lbAuthor.MouseEnter += LbAuthor_MouseEnter; // Sự kiện khi chuột di vào nhãn
            lbAuthor.MouseLeave += LbAuthor_MouseLeave; // Sự kiện khi chuột rời nhãn

            // Gắn sự kiện chỉ cho phép nhập số vào ô txtSpeed
            txtSpeed.KeyPress += TxtSpeed_KeyPress; // Gắn sự kiện KeyPress cho txtSpeed

            // Lưu màu nền mặc định của TextBox
            defaultTextboxBackColor = txtTextbox.BackColor; // Lưu màu nền mặc định của TextBox
        }

        // Sự kiện Tick của timer cầu vồng, cập nhật màu sắc
        private void RainbowTimer_Tick(object sender, EventArgs e) // Sự kiện Tick của timer cầu vồng, cập nhật màu sắc
        {
            rainbowPhase += 0.1; // Tăng giai đoạn để thay đổi màu

            // Tính toán màu cầu vồng dựa trên giai đoạn
            Color newColor = CalculateRainbowColor(rainbowPhase); // Tính toán màu cầu vồng dựa trên giai đoạn
            lbAuthor.ForeColor = newColor; // Áp dụng màu mới cho nhãn
        }

        // Tính toán màu sắc cầu vồng dựa trên giai đoạn
        private Color CalculateRainbowColor(double phase) // Tính toán màu sắc cầu vồng dựa trên giai đoạn
        {
            // Sử dụng hàm sin để tạo màu đỏ, xanh lá, xanh dương
            double red = Math.Sin(phase) * 127 + 128; // Tính toán giá trị màu đỏ
            double green = Math.Sin(phase + 2 * Math.PI / 3) * 127 + 128; // Tính toán giá trị màu xanh lá
            double blue = Math.Sin(phase + 4 * Math.PI / 3) * 127 + 128; // Tính toán giá trị màu xanh dương

            // Đảm bảo giá trị màu nằm trong khoảng 0-255
            red = Math.Max(0, Math.Min(255, red)); // Đảm bảo giá trị màu đỏ nằm trong khoảng 0-255
            green = Math.Max(0, Math.Min(255, green)); // Đảm bảo giá trị màu xanh lá nằm trong khoảng 0-255
            blue = Math.Max(0, Math.Min(255, blue)); // Đảm bảo giá trị màu xanh dương nằm trong khoảng 0-255

            return Color.FromArgb((int)red, (int)green, (int)blue); // Trả về màu sắc từ các giá trị RGB
        }

        // Khi chuột di vào nhãn tên tác giả, kích hoạt hiệu ứng cầu vồng
        private void LbAuthor_MouseEnter(object sender, EventArgs e) // Khi chuột di vào nhãn tên tác giả, kích hoạt hiệu ứng cầu vồng
        {
            if (!isRainbowActive) // Kiểm tra nếu hiệu ứng cầu vồng chưa hoạt động
            {
                isRainbowActive = true; // Kích hoạt hiệu ứng cầu vồng
                originalAuthorColor = lbAuthor.ForeColor; // Lưu màu gốc
                rainbowTimer.Start(); // Bắt đầu timer
            }
        }

        // Khi chuột rời nhãn, tắt hiệu ứng và khôi phục màu gốc
        private void LbAuthor_MouseLeave(object sender, EventArgs e) // Khi chuột rời nhãn, tắt hiệu ứng và khôi phục màu gốc
        {
            if (isRainbowActive) // Kiểm tra nếu hiệu ứng cầu vồng đang hoạt động
            {
                isRainbowActive = false; // Tắt hiệu ứng cầu vồng
                rainbowTimer.Stop(); // Dừng timer
                lbAuthor.ForeColor = originalAuthorColor; // Khôi phục màu gốc
            }
        }

        // Khởi tạo đồng hồ hiển thị thời gian
        private void InitializeClock() // Khởi tạo đồng hồ hiển thị thời gian
        {
            lbClock = new Label(); // Khởi tạo một đối tượng Label
            lbClock.AutoSize = true; // Tự động điều chỉnh kích thước
            lbClock.Font = new Font("Arial", 11, FontStyle.Bold); // Đặt font chữ
            lbClock.ForeColor = Color.FromArgb(255, 69, 0); // Đặt màu chữ
            lbClock.BackColor = Color.Transparent; // Nền trong suốt
            lbClock.TextAlign = ContentAlignment.MiddleCenter; // Căn giữa chữ
            // Đặt vị trí đồng hồ phía trên nút START
            lbClock.Location = new Point(btnSTART.Left + 5, 25); // Đặt vị trí
            lbClock.Size = new Size(btnRESET.Right - btnSTART.Left, 25); // Đặt kích thước
            this.Controls.Add(lbClock); // Thêm đồng hồ vào form

            timer = new System.Windows.Forms.Timer(); // Khởi tạo một đối tượng Timer
            timer.Interval = 500; // Cập nhật mỗi 0.5 giây
            timer.Tick += Timer_Tick; // Gắn sự kiện Tick
            timer.Start(); // Bắt đầu timer

            UpdateClock(); // Cập nhật thời gian ngay khi khởi tạo
        }

        // Sự kiện Tick của timer đồng hồ
        private void Timer_Tick(object sender, EventArgs e) // Sự kiện Tick của timer đồng hồ
        {
            UpdateClock(); // Cập nhật thời gian
        }

        // Cập nhật thời gian theo múi giờ GMT+7
        private void UpdateClock() // Cập nhật thời gian theo múi giờ GMT+7
        {
            TimeZoneInfo vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Tìm múi giờ Việt Nam
            DateTime vnTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone); // Chuyển đổi thời gian UTC sang múi giờ Việt Nam
            lbClock.Text = vnTime.ToString("HH:mm:ss"); // Hiển thị giờ:phút:giây
        }

        // Khởi tạo các biến và trạng thái cho công cụ dán
        private void InitializePasteTool() // Khởi tạo các biến và trạng thái cho công cụ dán
        {
            lines = new List<string>(); // Khởi tạo danh sách dòng
            currentLineIndex = 0; // Đặt chỉ số dòng về 0
            isPasting = false; // Không dán ban đầu
            isCompleted = false; // Chưa hoàn thành
            UpdateLineCountLabel(); // Cập nhật nhãn số dòng
        }

        // Sự kiện khi nhấn nút START
        private void btnSTART_Click(object sender, EventArgs e) // Sự kiện khi nhấn nút START
        {
            txtTextbox.ReadOnly = true; // Khóa TextBox
            txtTextbox.BackColor = Color.LightYellow; // Đặt màu nền vàng nhạt
            btnSTART.Enabled = false; // Vô hiệu hóa nút START
            btnSTOP.Enabled = true; // Kích hoạt nút STOP
            btnRESET.Enabled = true; // Kích hoạt nút RESET
            txtSpeed.Enabled = true; // Cho phép chỉnh tốc độ
            isPasting = false; // Đặt trạng thái không dán

            
            // *** Thêm dòng này để cập nhật trạng thái ***
            lbStatus.Text = "    SẴN SÀNG"; // Cập nhật trạng thái
            lbStatus.ForeColor = Color.DarkGreen; // Tùy chọn: Đặt màu cho trạng thái
            lbStatus.TextAlign = ContentAlignment.MiddleCenter; // Căn giữa chữ
            lbStatus.BackColor = Color.Transparent; // Nền trong suốt
        }

        // Sự kiện khi nhấn nút STOP
        private void btnSTOP_Click(object sender, EventArgs e) // Sự kiện khi nhấn nút STOP
        {
            StopPasting(false); // Dừng dán mà không đánh dấu hoàn thành
            if (titleResetTimer != null) titleResetTimer.Stop(); // Dừng timer reset tiêu đề nếu đang chạy
            isCompleted = false; // Đặt trạng thái chưa hoàn thành
            UpdateFormTitle(false); // Cập nhật tiêu đề form
            txtTextbox.ReadOnly = false; // Mở khóa TextBox
            txtTextbox.BackColor = defaultTextboxBackColor; // Khôi phục màu nền mặc định
            btnSTART.Enabled = true; // Kích hoạt nút START

            // *** Thêm dòng này để cập nhật trạng thái ***
            lbStatus.Text = "Đã tạm dừng\nvà mở khóa"; // Cập nhật trạng thái
            lbStatus.ForeColor = Color.Red; // Tùy chọn: Đặt màu cho trạng thái
            lbStatus.TextAlign = ContentAlignment.MiddleCenter; // Căn giữa chữ
            lbStatus.BackColor = Color.Transparent; // Nền trong suốt
        }

        // Sự kiện khi nhấn nút RESET
        private void btnRESET_Click(object sender, EventArgs e) // Sự kiện khi nhấn nút RESET
        {
            ResetForm(); // Đặt lại toàn bộ form
        }

        // Đặt lại form về trạng thái ban đầu
        private void ResetForm() // Đặt lại form về trạng thái ban đầu
        {
            txtTextbox.Clear(); // Xóa nội dung TextBox
            txtTextbox.ReadOnly = false; // Mở khóa TextBox
            txtTextbox.BackColor = defaultTextboxBackColor; // Khôi phục màu nền mặc định
            btnSTART.Enabled = true; // Kích hoạt nút START
            btnSTOP.Enabled = false; // Vô hiệu hóa nút STOP
            btnRESET.Enabled = true; // Kích hoạt nút RESET

            // Hủy đăng ký phím nóng
            UnregisterHotKey(this.Handle, HOTKEY_F1); // Hủy đăng ký phím nóng F1
            UnregisterHotKey(this.Handle, HOTKEY_ESC); // Hủy đăng ký phím nóng ESC
            InitializePasteTool(); // Khởi tạo lại công cụ dán

            // *** Thêm dòng này để cập nhật trạng thái ***
            lbStatus.Text = "    RESET"; // Cập nhật trạng thái
            lbStatus.ForeColor = Color.OrangeRed; // Tùy chọn: Đặt màu cho trạng thái
            lbStatus.TextAlign = ContentAlignment.MiddleCenter; // Căn lề chữ ở giữa
            lbStatus.BackColor = Color.Transparent; // Nền trong suốt

            if (titleResetTimer != null) titleResetTimer.Stop(); // Dừng timer reset tiêu đề nếu đang chạy
            isCompleted = false; // Đặt trạng thái chưa hoàn thành
            UpdateFormTitle(false); // Cập nhật tiêu đề form
        }

        // Bắt đầu quá trình dán
        private async Task StartPasting() // Bắt đầu quá trình dán bất đồng bộ
        {
            // Kiểm tra nếu TextBox trống
            if (string.IsNullOrEmpty(txtTextbox.Text)) // Kiểm tra nếu TextBox trống
            {
                MessageBox.Show("Vui lòng nhập dữ liệu trước khi bắt đầu dán.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Hiển thị thông báo
                return; // Thoát khỏi hàm
            }

            // Tách nội dung TextBox thành danh sách các dòng
            lines = txtTextbox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList(); // Tách nội dung TextBox thành danh sách các dòng
            currentLineIndex = 0; // Đặt lại chỉ số dòng
            isPasting = true; // Bắt đầu dán
            isCompleted = false; // Chưa hoàn thành
            if (titleResetTimer != null) titleResetTimer.Stop(); // Dừng timer reset tiêu đề nếu đang chạy
            UpdateFormTitle(); // Cập nhật tiêu đề form
            await PasteLinesAsync(); // Thay đổi: Thêm 'await' trước lời gọi
        }
        // Dừng quá trình dán
        private void StopPasting(bool completed) // Dừng quá trình dán
        {
            isPasting = false; // Dừng dán

            if (completed) // Nếu đã hoàn thành
            {
                isCompleted = true; // Đặt trạng thái hoàn thành
                UpdateFormTitle(true); // Hiện tiến độ cuối cùng
                StartTitleResetTimer(); // Bắt đầu timer 5 phút để đổi về tên gốc
                SendKeys.SendWait("{ENTER}"); // Gửi phím ENTER
                Thread.Sleep(500); // Tạm dừng 500ms
                var notify = new NotifyForm($"Đã hoàn thành {currentLineIndex} dòng !"); // Tạo form thông báo
                notify.ShowDialog(); // Hiển thị form thông báo TopMost

                // *** Thêm dòng này để cập nhật trạng thái khi hoàn thành ***
                lbStatus.Text = $"Hoàn thành dán\n{currentLineIndex} dòng!"; // Cập nhật trạng thái
                lbStatus.ForeColor = Color.Green; // Tùy chọn: Đặt màu cho trạng thái
            }
            else // Nếu không hoàn thành
            {
                isCompleted = false; // Đặt trạng thái chưa hoàn thành
                UpdateFormTitle(false); // Quay về tiêu đề mặc định NGAY LẬP TỨC
                if (titleResetTimer != null) titleResetTimer.Stop(); // Đảm bảo không còn timer chạy

                // *** Thêm dòng này để cập nhật trạng thái ***
                lbStatus.Text = "Đã tạm dừng\nvà mở khóa"; // Cập nhật trạng thái
                lbStatus.ForeColor = Color.Red; // Tùy chọn: Đặt màu cho trạng thái
            }

            // Luôn mở khóa txtTextbox khi quá trình dán dừng, bất kể hoàn thành hay tạm dừng
            txtTextbox.ReadOnly = false; // Mở khóa TextBox
            txtTextbox.BackColor = defaultTextboxBackColor; // Khôi phục màu nền mặc định
            btnSTART.Enabled = true; // Kích hoạt nút START
            btnSTOP.Enabled = false; // Vô hiệu hóa nút STOP
            btnRESET.Enabled = true; // Kích hoạt nút RESET

            lbStatus.TextAlign = ContentAlignment.MiddleCenter; // Căn giữa chữ
            lbStatus.BackColor = Color.Transparent; // Nền trong suốt
        }

        private void UpdateFormTitle(bool showProgress = true) // Cập nhật tiêu đề form
        {
            DateTime today = DateTime.Now; // Lấy ngày hiện tại
            string formattedDate = today.ToString("dd/MM/yyyy"); // Định dạng ngày
            if (showProgress && isPasting && lines != null && lines.Count > 0) // Nếu đang dán và có tiến độ
            {
                this.Text = $"Tiến độ: {currentLineIndex}/{lines.Count}"; // Cập nhật tiêu đề với tiến độ
            }
            else if (showProgress && isCompleted && lines != null && lines.Count > 0) // Nếu đã hoàn thành
            {
                this.Text = $"Đã dán {currentLineIndex}/{lines.Count} dòng"; // Cập nhật tiêu đề với số dòng đã dán
            }
            else // Trường hợp khác
            {
                this.Text = $"FAB {formattedDate}"; // Đặt tiêu đề mặc định
            }
        }
        // Hàm dán các dòng văn bản bất đồng bộ
        private async Task PasteLinesAsync() // Hàm dán các dòng văn bản bất đồng bộ
        {
            int pasteSpeed = DEFAULT_PASTE_SPEED; // Tốc độ dán mặc định
            if (!string.IsNullOrEmpty(txtSpeed.Text)) // Kiểm tra nếu txtSpeed không rỗng
            {
                // Kiểm tra giá trị tốc độ hợp lệ
                if (!int.TryParse(txtSpeed.Text, out pasteSpeed)) // Nếu giá trị tốc độ không hợp lệ
                {
                    pasteSpeed = DEFAULT_PASTE_SPEED; // Đặt tốc độ mặc định
                    MessageBox.Show("Tốc độ không hợp lệ. Sử dụng tốc độ mặc định " + DEFAULT_PASTE_SPEED + "ms.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); // Hiển thị thông báo lỗi
                    txtSpeed.Text = ""; // Xóa nội dung txtSpeed
                }
            }

            if (!string.IsNullOrEmpty(txtTextbox.Text)) // Nếu TextBox không trống
            {
                // Lọc các dòng không rỗng từ TextBox
                lines = txtTextbox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None) // Tách các dòng
                                .Where(line => !string.IsNullOrEmpty(line)) // Lọc các dòng không rỗng
                                .ToList(); // Chuyển sang List

                if (lines != null && lines.Count > 0) // Nếu có dòng để dán
                {
                    try // Xử lý ngoại lệ
                    {
                        // Lặp qua từng dòng để dán
                        while (isPasting && currentLineIndex < lines.Count) // Lặp khi đang dán và chưa hết dòng
                        {
                            Clipboard.SetText(lines[currentLineIndex]); // Đặt dòng vào clipboard
                            await Task.Delay(50); // Độ trễ sau copy

                            SendKeys.SendWait("^{v}"); // Dán (Ctrl + V)
                            await Task.Delay(50); // Độ trễ sau paste

                            SendKeys.SendWait("{ENTER}"); // Nhấn Enter (nếu muốn có thêm delay thì thêm tiếp dòng dưới)
                            await Task.Delay(50); // Độ trễ sau enter (nếu cần)

                            currentLineIndex++; // Tăng chỉ số dòng
                            UpdateLineCountLabel(); // Cập nhật nhãn số dòng
                            UpdateFormTitle(); // Cập nhật tiến độ trên tiêu đề
                            await Task.Delay(pasteSpeed); // Chờ theo tốc độ cài đặt
                        }

                        // Kiểm tra nếu đã dán hết các dòng
                        if (isPasting && currentLineIndex == lines.Count) // Nếu đã dán hết các dòng
                        {
                            isCompleted = true; // Đặt trạng thái hoàn thành
                        }

                        StopPasting(isCompleted); // Dừng dán
                    }
                    catch (Exception ex) // Bắt ngoại lệ
                    {
                        MessageBox.Show("Đã xảy ra lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); // Hiển thị thông báo lỗi
                    }
                }
            }
            else // Nếu TextBox trống
            {
                MessageBox.Show("Vui lòng nhập dữ liệu trước khi bắt đầu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); // Hiển thị thông báo
            }
        }

        private void StartTitleResetTimer() // Bắt đầu timer reset tiêu đề
        {
            if (titleResetTimer == null) // Nếu timer chưa được khởi tạo
            {
                titleResetTimer = new System.Windows.Forms.Timer(); // Khởi tạo timer
                titleResetTimer.Interval = 5 * 60 * 1000; // 5 phút = 300000 ms
                titleResetTimer.Tick += TitleResetTimer_Tick; // Gắn sự kiện Tick
            }
            titleResetTimer.Start(); // Bắt đầu timer
        }

        private void TitleResetTimer_Tick(object sender, EventArgs e) // Sự kiện Tick của timer reset tiêu đề
        {
            titleResetTimer.Stop(); // Dừng timer
            isCompleted = false; // Đặt trạng thái chưa hoàn thành
            UpdateFormTitle(false); // Reset về tiêu đề gốc
        }

        // Cập nhật nhãn hiển thị số dòng
        private void UpdateLineCountLabel() // Cập nhật nhãn hiển thị số dòng
        {
            // Đếm số dòng không rỗng
            int lineCount = txtTextbox.Lines.Count(line => !string.IsNullOrWhiteSpace(line)); // Đếm số dòng không rỗng
            lbCount.Text = $"Line: {lineCount}"; // Cập nhật nhãn số dòng
            lbCount.BackColor = Color.Transparent; // Nền trong suốt

            // Nếu đang dán, hiển thị tiến độ
            if (isPasting) // Nếu đang dán
            {
                lbCount.Text += $"     (Hoàn thành: {currentLineIndex}/{lines.Count})"; // Thêm tiến độ vào nhãn
                lbCount.BackColor = Color.Transparent; // Nền trong suốt
            }
            UpdateScrollbarVisibility(); // Cập nhật thanh cuộn
        }

        // Sự kiện khi form được tải
        private void Form1_Load(object sender, EventArgs e) // Sự kiện khi form được tải
        {
            btnSTOP.Enabled = false; // Vô hiệu hóa nút STOP
            txtTextbox.TextChanged += txtTextbox_TextChanged; // Gắn sự kiện thay đổi TextBox
            UpdateLineCountLabel(); // Cập nhật nhãn số dòng
            UpdateScrollbarVisibility(); // Cập nhật thanh cuộn
            // Label trạng thái
            lbStatus.Text = "Vui lòng nhập\ndữ liệu và\nbấm START"; // Trạng thái ban đầu
            lbStatus.ForeColor = Color.DimGray; // Tùy chọn: Đặt màu cho trạng thái ban đầu
            lbStatus.TextAlign = ContentAlignment.MiddleCenter; // Căn lề chữ ở giữa
            lbStatus.BackColor = Color.Transparent; // Nền trong suốt
            // *** Thêm dòng này để đặt tiêu đề của Form ***
            DateTime today = DateTime.Now; // Lấy ngày hiện tại
            UpdateFormTitle(); // Cập nhật tiêu đề form

            // Đăng ký phím nóng F1 và ESC
            RegisterHotKey(this.Handle, HOTKEY_F1, 0, (int)Keys.F1); // Đăng ký phím nóng F1
            RegisterHotKey(this.Handle, HOTKEY_ESC, 0, (int)Keys.Escape); // Đăng ký phím nóng ESC
        }

        protected override void WndProc(ref Message m) // Xử lý thông điệp cửa sổ (bao gồm phím nóng)
        {
            base.WndProc(ref m); // Gọi phương thức cơ sở

            if (m.Msg == 0x0312) // Tin nhắn phím nóng
            {
                int id = m.WParam.ToInt32(); // Lấy ID của phím nóng
                switch (id) // Kiểm tra ID
                {
                    case HOTKEY_F1: // Nếu là F1
                        // Nếu không dán và có dữ liệu trong textbox, bắt đầu dán
                        if (!isPasting && !string.IsNullOrEmpty(txtTextbox.Text)) // Kiểm tra điều kiện: không đang dán và textbox không rỗng
                        {
                            _ = StartPasting(); // Bắt đầu dán (không chờ)
                        }
                        break; // Thoát khỏi switch
                    case HOTKEY_ESC: // Nếu là ESC
                        // Nếu đang dán, dừng lại
                        if (isPasting) // Kiểm tra điều kiện
                        {
                            StopPasting(false); // Dừng dán
                            isCompleted = false; // Đặt trạng thái chưa hoàn thành
                        }
                        break; // Thoát khỏi switch
                }
            }
        }

        // Khi form đóng, hủy đăng ký phím nóng
        private void Form1_FormClosing(object sender, FormClosingEventArgs e) // Khi form đóng
        {
            UnregisterHotKey(this.Handle, HOTKEY_F1); // Hủy đăng ký phím nóng F1
            UnregisterHotKey(this.Handle, HOTKEY_ESC); // Hủy đăng ký phím nóng ESC
        }

        // Sự kiện khi nội dung TextBox thay đổi
        private void txtTextbox_TextChanged(object sender, EventArgs e) // Sự kiện khi nội dung TextBox thay đổi
        {
            UpdateLineCountLabel(); // Cập nhật nhãn số dòng
            txtTextbox.SelectionStart = txtTextbox.Text.Length; // Đặt con trỏ ở cuối
            txtTextbox.ScrollToCaret(); // Cuộn đến vị trí con trỏ
        }

        // Cập nhật hiển thị thanh cuộn
        private void UpdateScrollbarVisibility() // Cập nhật hiển thị thanh cuộn
        {
            // Kiểm tra nếu nội dung vượt quá chiều cao TextBox
            Size textSize = txtTextbox.GetPreferredSize(new Size(txtTextbox.Width, 0)); // Lấy kích thước ưu tiên của văn bản
            if (textSize.Height > txtTextbox.ClientSize.Height) // Nếu chiều cao văn bản lớn hơn chiều cao hiển thị của TextBox
            {
                txtTextbox.ScrollBars = ScrollBars.Vertical; // Hiển thị thanh cuộn dọc
            }
            else // Ngược lại
            {
                txtTextbox.ScrollBars = ScrollBars.None; // Ẩn thanh cuộn
            }

            // Kiểm tra nếu nội dung vượt quá chiều rộng TextBox
            Size maxLineWidth = CalculateMaxLineWidth(); // Tính toán chiều rộng lớn nhất của các dòng
            if (maxLineWidth.Width > txtTextbox.ClientSize.Width) // Nếu chiều rộng lớn nhất của dòng lớn hơn chiều rộng hiển thị của TextBox
            {
                txtTextbox.ScrollBars |= ScrollBars.Horizontal; // Hiển thị thanh cuộn ngang
            }
            else // Ngược lại
            {
                txtTextbox.ScrollBars &= ~ScrollBars.Horizontal; // Ẩn thanh cuộn ngang
                if (txtTextbox.ScrollBars == ScrollBars.None) // Nếu không có thanh cuộn nào
                {
                    txtTextbox.ScrollBars = ScrollBars.None; // Đặt lại là không có thanh cuộn
                }
                else // Nếu có thanh cuộn dọc
                {
                    txtTextbox.ScrollBars = ScrollBars.Vertical; // Chỉ hiển thị thanh cuộn dọc
                }
            }
        }

        // Tính toán chiều rộng lớn nhất của các dòng
        private Size CalculateMaxLineWidth() // Tính toán chiều rộng lớn nhất của các dòng
        {
            int maxWidth = 0; // Khởi tạo chiều rộng lớn nhất
            using (Graphics g = txtTextbox.CreateGraphics()) // Tạo đối tượng Graphics từ TextBox
            {
                foreach (string line in txtTextbox.Lines) // Lặp qua từng dòng
                {
                    SizeF lineSize = g.MeasureString(line, txtTextbox.Font); // Đo kích thước của dòng
                    if (lineSize.Width > maxWidth) // Nếu chiều rộng của dòng hiện tại lớn hơn chiều rộng lớn nhất
                    {
                        maxWidth = (int)lineSize.Width; // Cập nhật chiều rộng lớn nhất
                    }
                }
            }
            return new Size(maxWidth, 0); // Trả về kích thước với chiều rộng lớn nhất
        }

        // Chỉ cho phép nhập số vào ô txtSpeed
        private void TxtSpeed_KeyPress(object sender, KeyPressEventArgs e) // Chỉ cho phép nhập số vào ô txtSpeed
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar)) // Nếu ký tự không phải số và không phải ký tự điều khiển
            {
                e.Handled = true; // Chặn ký tự
            }
        }

        // Khi form thay đổi kích thước, cập nhật thanh cuộn
        private void Form1_Resize(object sender, EventArgs e) // Khi form thay đổi kích thước
        {
            UpdateScrollbarVisibility(); // Cập nhật thanh cuộn
        }
    }
}
