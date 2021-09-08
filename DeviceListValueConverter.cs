using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UN7ZO.HamCockpitPlugins.SDRPlaySource.NativeMethods;

namespace UN7ZO.HamCockpitPlugins.SDRPlaySource {
    public struct DeviceValueEntry {
        public int Id;
        public string Name;
        public DeviceValueEntry(int id, string name) { Id = id; Name = name; }       
    }

    /// <exclude />
    public class DeviceListValueConverter : StringConverter {
        internal DeviceValueEntry[] valuesTable = null;
        private SDRPlayDevice device = SDRPlayDevice.GetInstance();
        /// <exclude />
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            return true;
        }

        /// <exclude />
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            if (valuesTable == null || valuesTable.Length == 0)
                ListAvailableDevices();
            return new StandardValuesCollection(valuesTable.Select(s => s.Id).ToArray());
        }

        /// <exclude />
        protected void ListAvailableDevices() {
            valuesTable = new DeviceValueEntry[0];

            if (device != null) {
                int count = device.getDeviceCount();
                if (count > 0) {
                    valuesTable = new DeviceValueEntry[count];
                
                    lightDeviceInfo[] devices = device.getDeviceList();
                    for (int i = 0; i < count; i++) {
                        lightDeviceInfo _dev = devices[i]; 
                        valuesTable[0] = new DeviceValueEntry(i, _dev.deviceName + " (S/N: " + _dev.deviceSN + ")");
                    }
                }                                         
            }
        }

        /// <exclude />
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (valuesTable == null || valuesTable.Length == 0) ListAvailableDevices();
            return valuesTable.Where(s => s.Name == value as string)?.Select(s => s.Id)?.First();
        }

        /// <exclude />
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (valuesTable == null || valuesTable.Length == 0) ListAvailableDevices();
       
            try {
                return valuesTable.Where(s => s.Id == (int)value).Select(s => s.Name).First();
            }
            catch {
                return "<please select>";
            }
        }
    }


}
