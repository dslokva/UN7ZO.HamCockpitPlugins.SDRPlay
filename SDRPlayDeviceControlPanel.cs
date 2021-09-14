using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UN7ZO.HamCockpitPlugins.SDRPlaySource {
    public partial class SDRPlayDeviceControlPanel : UserControl {
        SDRPlaySource _mainPluginClass = null;

        public SDRPlayDeviceControlPanel() {
            InitializeComponent();
            cbAntennaSelect.SelectedIndex = 0;
        }

        internal void setControlLink(SDRPlaySource mainPluginClass) {
            _mainPluginClass = mainPluginClass;
        }

        internal void setInitialLNAGRLevel(int value) {
            trackLNAGRValue.Value = value;
            labLNAGRText.Text = Convert.ToString(value);
        }

        internal void setInitialAntenna(int antindex) {
            int ant = 5;

            if (ant > 0) 
                ant = antindex - 5;

            cbAntennaSelect.SelectedIndex = ant;
        }

        internal void setInitialNotchStatus(bool value) {
            checkBoxEnableNotches.Checked = value;
        }
        internal void setLNAGRlabelText(string value) {
            labLNAGRText.Invoke((MethodInvoker)delegate {
                // Running on the UI thread
                labLNAGRText.Text = value;
            });
        }

        internal void setPanelVisible(bool value) {
            contentPanel.Visible = value;
        }

        internal void setOverloadLabelVisible(bool value) {
            labOverloadDetect.Invoke((MethodInvoker)delegate {
                // Running on the UI thread
                labOverloadDetect.Visible = value;
            });
        }

        private void trackLNAGRValue_Scroll(object sender, EventArgs e) {
            _mainPluginClass.setDeviceLNAGrLevel(trackLNAGRValue.Value);
        }

        private void checkBoxEnableNotches_CheckedChanged(object sender, EventArgs e) {
            _mainPluginClass.setDeviceNotchEnabled(checkBoxEnableNotches.Checked);
        }

        private void cbAntennaSelect_SelectedIndexChanged(object sender, EventArgs e) {
            int antNum = cbAntennaSelect.SelectedIndex + 5;
            if (_mainPluginClass != null)
                _mainPluginClass.setDeviceAntenna(antNum);
        }

    }
}
