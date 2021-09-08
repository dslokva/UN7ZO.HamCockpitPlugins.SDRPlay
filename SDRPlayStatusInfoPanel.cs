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
    public partial class SDRPlayStatusInfoPanel : UserControl {
        public SDRPlayStatusInfoPanel() {
            InitializeComponent();            
        }

        private void SDRPlayStatusInfoPanel_Load(object sender, EventArgs e) {

        }

        public void setSDRDeviceNameLabelText(string text) {
            labSDRDeviceName.Text = text;
        }

        internal void setSDRDeviceStatusLabelColor(Color color) {
            labSDRDeviceStatus.Invoke((MethodInvoker)delegate {
                // Running on the UI thread
                labSDRDeviceStatus.ForeColor = color;
            });
        }

        internal void setSDRDeviceStatusLabelText(string text) {
            labSDRDeviceStatus.Invoke((MethodInvoker)delegate {
                // Running on the UI thread
                labSDRDeviceStatus.Text = text;
            });
        }


        internal int getSDRDeviceNameLabelWidth() {
            return labSDRDeviceName.Width + 45;
        }

    }
}
