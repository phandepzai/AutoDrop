using System.Drawing;

namespace PasteTool
{
    partial class nongvanphan
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(nongvanphan));
            this.btnSTART = new System.Windows.Forms.Button();
            this.btnSTOP = new System.Windows.Forms.Button();
            this.btnRESET = new System.Windows.Forms.Button();
            this.txtTextbox = new System.Windows.Forms.TextBox();
            this.lbCount = new System.Windows.Forms.Label();
            this.txtSpeed = new System.Windows.Forms.TextBox();
            this.labelSpeed = new System.Windows.Forms.Label();
            this.lbAuthor = new System.Windows.Forms.Label();
            this.lbStatus = new System.Windows.Forms.Label();
            this.lbInput = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnSTART
            // 
            this.btnSTART.Location = new System.Drawing.Point(224, 66);
            this.btnSTART.Name = "btnSTART";
            this.btnSTART.Size = new System.Drawing.Size(75, 40);
            this.btnSTART.TabIndex = 0;
            this.btnSTART.Text = "START";
            this.btnSTART.UseVisualStyleBackColor = true;
            this.btnSTART.Click += new System.EventHandler(this.btnSTART_Click);
            // 
            // btnSTOP
            // 
            this.btnSTOP.Location = new System.Drawing.Point(224, 124);
            this.btnSTOP.Name = "btnSTOP";
            this.btnSTOP.Size = new System.Drawing.Size(75, 40);
            this.btnSTOP.TabIndex = 1;
            this.btnSTOP.Text = "STOP";
            this.btnSTOP.UseVisualStyleBackColor = true;
            this.btnSTOP.Click += new System.EventHandler(this.btnSTOP_Click);
            // 
            // btnRESET
            // 
            this.btnRESET.Location = new System.Drawing.Point(224, 181);
            this.btnRESET.Name = "btnRESET";
            this.btnRESET.Size = new System.Drawing.Size(75, 40);
            this.btnRESET.TabIndex = 2;
            this.btnRESET.Text = "RESET";
            this.btnRESET.UseVisualStyleBackColor = true;
            this.btnRESET.Click += new System.EventHandler(this.btnRESET_Click);
            // 
            // txtTextbox
            // 
            this.txtTextbox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtTextbox.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtTextbox.Location = new System.Drawing.Point(9, 24);
            this.txtTextbox.Multiline = true;
            this.txtTextbox.Name = "txtTextbox";
            this.txtTextbox.Size = new System.Drawing.Size(200, 451);
            this.txtTextbox.TabIndex = 3;
            this.txtTextbox.WordWrap = false;
            // 
            // lbCount
            // 
            this.lbCount.AutoSize = true;
            this.lbCount.BackColor = System.Drawing.Color.Transparent;
            this.lbCount.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbCount.Location = new System.Drawing.Point(8, 479);
            this.lbCount.Name = "lbCount";
            this.lbCount.Size = new System.Drawing.Size(34, 15);
            this.lbCount.TabIndex = 4;
            this.lbCount.Text = "Line:";
            // 
            // txtSpeed
            // 
            this.txtSpeed.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSpeed.ForeColor = System.Drawing.Color.MediumBlue;
            this.txtSpeed.Location = new System.Drawing.Point(236, 262);
            this.txtSpeed.Multiline = true;
            this.txtSpeed.Name = "txtSpeed";
            this.txtSpeed.Size = new System.Drawing.Size(49, 24);
            this.txtSpeed.TabIndex = 5;
            this.txtSpeed.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // labelSpeed
            // 
            this.labelSpeed.AutoSize = true;
            this.labelSpeed.BackColor = System.Drawing.Color.Transparent;
            this.labelSpeed.Location = new System.Drawing.Point(230, 242);
            this.labelSpeed.Name = "labelSpeed";
            this.labelSpeed.Size = new System.Drawing.Size(64, 13);
            this.labelSpeed.TabIndex = 6;
            this.labelSpeed.Text = "Tốc độ (ms)";
            // 
            // lbAuthor
            // 
            this.lbAuthor.AutoSize = true;
            this.lbAuthor.BackColor = System.Drawing.Color.Transparent;
            this.lbAuthor.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbAuthor.ForeColor = System.Drawing.Color.Silver;
            this.lbAuthor.Location = new System.Drawing.Point(219, 482);
            this.lbAuthor.Name = "lbAuthor";
            this.lbAuthor.Size = new System.Drawing.Size(90, 13);
            this.lbAuthor.TabIndex = 7;
            this.lbAuthor.Text = "©Nông Văn Phấn";
            // 
            // lbStatus
            // 
            this.lbStatus.AutoSize = true;
            this.lbStatus.BackColor = System.Drawing.Color.Transparent;
            this.lbStatus.Location = new System.Drawing.Point(231, 328);
            this.lbStatus.Name = "lbStatus";
            this.lbStatus.Size = new System.Drawing.Size(58, 13);
            this.lbStatus.TabIndex = 8;
            this.lbStatus.Text = "Trạng thái:";
            // 
            // lbInput
            // 
            this.lbInput.AutoSize = true;
            this.lbInput.BackColor = System.Drawing.Color.Transparent;
            this.lbInput.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lbInput.ForeColor = System.Drawing.Color.Indigo;
            this.lbInput.Location = new System.Drawing.Point(8, 4);
            this.lbInput.Name = "lbInput";
            this.lbInput.Size = new System.Drawing.Size(67, 13);
            this.lbInput.TabIndex = 9;
            this.lbInput.Text = "Nhập dữ liệu";
            // 
            // nongvanphan
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::PasteTool.Properties.Resources.bgapp;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(314, 499);
            this.Controls.Add(this.lbInput);
            this.Controls.Add(this.lbStatus);
            this.Controls.Add(this.lbAuthor);
            this.Controls.Add(this.labelSpeed);
            this.Controls.Add(this.txtSpeed);
            this.Controls.Add(this.lbCount);
            this.Controls.Add(this.txtTextbox);
            this.Controls.Add(this.btnRESET);
            this.Controls.Add(this.btnSTOP);
            this.Controls.Add(this.btnSTART);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "nongvanphan";
            this.Text = "FAB";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSTART;
        private System.Windows.Forms.Button btnSTOP;
        private System.Windows.Forms.Button btnRESET;
        private System.Windows.Forms.TextBox txtTextbox;
        private System.Windows.Forms.Label lbCount;
        private System.Windows.Forms.TextBox txtSpeed;
        private System.Windows.Forms.Label labelSpeed;
        private System.Windows.Forms.Label lbAuthor;
        private System.Windows.Forms.Label lbStatus;
        private System.Windows.Forms.Label lbInput;
    }
}