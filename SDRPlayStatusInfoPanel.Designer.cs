namespace UN7ZO.HamCockpitPlugins.SDRPlaySource {
    partial class SDRPlayStatusInfoPanel {
        /// <summary> 
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором компонентов

        /// <summary> 
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent() {
            this.panel1 = new System.Windows.Forms.Panel();
            this.labSDRDeviceStatus = new System.Windows.Forms.Label();
            this.labSDRDeviceName = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.AutoSize = true;
            this.panel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.panel1.BackColor = System.Drawing.SystemColors.Control;
            this.panel1.Controls.Add(this.labSDRDeviceStatus);
            this.panel1.Controls.Add(this.labSDRDeviceName);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(198, 0);
            this.panel1.TabIndex = 2;
            // 
            // labSDRDeviceStatus
            // 
            this.labSDRDeviceStatus.AutoSize = true;
            this.labSDRDeviceStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labSDRDeviceStatus.Location = new System.Drawing.Point(3, 19);
            this.labSDRDeviceStatus.Name = "labSDRDeviceStatus";
            this.labSDRDeviceStatus.Size = new System.Drawing.Size(80, 17);
            this.labSDRDeviceStatus.TabIndex = 6;
            this.labSDRDeviceStatus.Text = "status n/a";
            // 
            // labSDRDeviceName
            // 
            this.labSDRDeviceName.AutoSize = true;
            this.labSDRDeviceName.Location = new System.Drawing.Point(3, 2);
            this.labSDRDeviceName.Name = "labSDRDeviceName";
            this.labSDRDeviceName.Size = new System.Drawing.Size(192, 17);
            this.labSDRDeviceName.TabIndex = 2;
            this.labSDRDeviceName.Text = "SDRPlay Device not selected";
            // 
            // SDRPlayStatusInfoPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.panel1);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "SDRPlayStatusInfoPanel";
            this.Size = new System.Drawing.Size(198, 0);
            this.Load += new System.EventHandler(this.SDRPlayStatusInfoPanel_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label labSDRDeviceName;
        private System.Windows.Forms.Label labSDRDeviceStatus;
    }
}
