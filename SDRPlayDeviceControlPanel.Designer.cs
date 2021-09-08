using System;
using System.Windows.Forms;

namespace UN7ZO.HamCockpitPlugins.SDRPlaySource {
    partial class SDRPlayDeviceControlPanel : UserControl {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.contentPanel = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.cbAntennaSelect = new System.Windows.Forms.ComboBox();
            this.labOverloadDetect = new System.Windows.Forms.Label();
            this.checkBoxEnableNotches = new System.Windows.Forms.CheckBox();
            this.labLNAGRText = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.trackLNAGRValue = new System.Windows.Forms.TrackBar();
            this.contentPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackLNAGRValue)).BeginInit();
            this.SuspendLayout();
            // 
            // contentPanel
            // 
            this.contentPanel.AutoSize = true;
            this.contentPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.contentPanel.Controls.Add(this.label2);
            this.contentPanel.Controls.Add(this.cbAntennaSelect);
            this.contentPanel.Controls.Add(this.labOverloadDetect);
            this.contentPanel.Controls.Add(this.checkBoxEnableNotches);
            this.contentPanel.Controls.Add(this.labLNAGRText);
            this.contentPanel.Controls.Add(this.label1);
            this.contentPanel.Controls.Add(this.trackLNAGRValue);
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentPanel.Location = new System.Drawing.Point(0, 0);
            this.contentPanel.Margin = new System.Windows.Forms.Padding(0);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Size = new System.Drawing.Size(372, 80);
            this.contentPanel.TabIndex = 0;
            this.contentPanel.Visible = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 52);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 17);
            this.label2.TabIndex = 9;
            this.label2.Text = "Antenna:";
            // 
            // cbAntennaSelect
            // 
            this.cbAntennaSelect.FormattingEnabled = true;
            this.cbAntennaSelect.Items.AddRange(new object[] {
            "Ant_A",
            "Ant_B"});
            this.cbAntennaSelect.Location = new System.Drawing.Point(77, 49);
            this.cbAntennaSelect.Name = "cbAntennaSelect";
            this.cbAntennaSelect.Size = new System.Drawing.Size(74, 24);
            this.cbAntennaSelect.TabIndex = 8;
            this.cbAntennaSelect.SelectedIndexChanged += new System.EventHandler(this.cbAntennaSelect_SelectedIndexChanged);
            // 
            // labOverloadDetect
            // 
            this.labOverloadDetect.AutoSize = true;
            this.labOverloadDetect.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labOverloadDetect.ForeColor = System.Drawing.Color.Maroon;
            this.labOverloadDetect.Location = new System.Drawing.Point(40, 27);
            this.labOverloadDetect.Name = "labOverloadDetect";
            this.labOverloadDetect.Size = new System.Drawing.Size(93, 17);
            this.labOverloadDetect.TabIndex = 7;
            this.labOverloadDetect.Text = "OVERLOAD";
            this.labOverloadDetect.Visible = false;
            // 
            // checkBoxEnableNotches
            // 
            this.checkBoxEnableNotches.AutoSize = true;
            this.checkBoxEnableNotches.Location = new System.Drawing.Point(179, 51);
            this.checkBoxEnableNotches.Name = "checkBoxEnableNotches";
            this.checkBoxEnableNotches.Size = new System.Drawing.Size(179, 21);
            this.checkBoxEnableNotches.TabIndex = 6;
            this.checkBoxEnableNotches.Text = "Enable FM/MW notches";
            this.checkBoxEnableNotches.UseVisualStyleBackColor = true;
            this.checkBoxEnableNotches.CheckedChanged += new System.EventHandler(this.checkBoxEnableNotches_CheckedChanged);
            // 
            // labLNAGRText
            // 
            this.labLNAGRText.AutoSize = true;
            this.labLNAGRText.Location = new System.Drawing.Point(130, 5);
            this.labLNAGRText.Name = "labLNAGRText";
            this.labLNAGRText.Size = new System.Drawing.Size(36, 17);
            this.labLNAGRText.TabIndex = 5;
            this.labLNAGRText.Text = "0 db";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 5);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(121, 17);
            this.label1.TabIndex = 4;
            this.label1.Text = "LNA Gain reduce:";
            // 
            // trackLNAGRValue
            // 
            this.trackLNAGRValue.AutoSize = false;
            this.trackLNAGRValue.Location = new System.Drawing.Point(179, 3);
            this.trackLNAGRValue.Maximum = 8;
            this.trackLNAGRValue.Name = "trackLNAGRValue";
            this.trackLNAGRValue.Size = new System.Drawing.Size(190, 34);
            this.trackLNAGRValue.TabIndex = 3;
            this.trackLNAGRValue.Scroll += new System.EventHandler(this.trackLNAGRValue_Scroll);
            // 
            // SDRPlayDeviceControlPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.contentPanel);
            this.Name = "SDRPlayDeviceControlPanel";
            this.Size = new System.Drawing.Size(372, 80);
            this.Load += new System.EventHandler(this.SDRPlayDeviceControlPanel_Load);
            this.contentPanel.ResumeLayout(false);
            this.contentPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackLNAGRValue)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void SDRPlayDeviceControlPanel_Load(object sender, EventArgs e) {

        }

        #endregion

        private Panel contentPanel;
        private Label labLNAGRText;
        private Label label1;
        private TrackBar trackLNAGRValue;
        private CheckBox checkBoxEnableNotches;
        private Label labOverloadDetect;
        private ComboBox cbAntennaSelect;
        private Label label2;
    }
}
