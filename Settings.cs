using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VE3NEA.HamCockpit.PluginHelpers;
using static UN7ZO.HamCockpitPlugins.SDRPlaySource.NativeMethods;
using static UN7ZO.HamCockpitPlugins.SDRPlaySource.SDRplayAPI_RSP2;

namespace UN7ZO.HamCockpitPlugins.SDRPlaySource {

    enum RSP_SampleRate : int {
        [Description("8 MSps")]
        SR_Full_8 = 8000000,

        [Description("6 MSps")]
        SR_Full_6 = 6000000,

        [Description("2 MSps")]
        SR_Full_2 = 2000000
    }

    enum RSP_IF_Mode : int {
        [Description("Zero-IF")]
        Zero_IF = 0,

        [Description("Low-IF")]
        Low_IF = -1
    }

    enum RSP_AGC : int {
        [Description("Enabled (Automatic)")]
        AGC_Auto = sdrplay_api_AgcControlT.sdrplay_api_AGC_CTRL_EN,

        [Description("Disabled")]
        AGC_Disable = sdrplay_api_AgcControlT.sdrplay_api_AGC_DISABLE
    }
    enum RSP_ANT : int {
        [Description("Ant_A")]
        ANT_A = sdrplay_api_Rsp2_AntennaSelectT.sdrplay_api_Rsp2_ANTENNA_A,

        [Description("Ant_B")]
        ANT_B = sdrplay_api_Rsp2_AntennaSelectT.sdrplay_api_Rsp2_ANTENNA_B
    }

    enum RSP_IFBW : int {
        [Description("1.536 Mhz")]
        IF_BW_1_536 = sdrplay_api_Bw_MHzT.sdrplay_api_BW_1_536,

        [Description("600 Khz")]
        IF_BW_0_600 = sdrplay_api_Bw_MHzT.sdrplay_api_BW_0_600,

        [Description("300 Khz")]
        IF_BW_0_300 = sdrplay_api_Bw_MHzT.sdrplay_api_BW_0_300,

        [Description("200 Khz")]
        IF_BW_0_200 = sdrplay_api_Bw_MHzT.sdrplay_api_BW_0_200
    }

    class Settings {
        private RSP_IFBW device_IFBW = RSP_IFBW.IF_BW_1_536;
        private RSP_SampleRate device_SampleRate = RSP_SampleRate.SR_Full_8;
        private RSP_IF_Mode device_IFMode = RSP_IF_Mode.Low_IF;
        private int deviceLNALevel;

        [DisplayName("0. Available devices")]
        [Description("Please select a device. Maximum one of 6 attached devices can be used.")]
        [TypeConverter(typeof(DeviceListValueConverter))]
        public int SelectedDeviceIndex { get; set; }

        [DisplayName("1. Antenna port")]
        [Description("Please select antenna port for RSP2.")]
        [TypeConverter(typeof(EnumDescriptionConverter))]
        public RSP_ANT DeviceAntenna { get; set; }

        [DisplayName("2. IF mode")]
        [Description("Please select IF mode for receiver device. Low-IF: An intermediate frequency that is less than the carrier frequency. Zero-IF: IF signal represented in its in-phase and quadrature components. This reduces the IF frequency to zero or near zero.")]
        [TypeConverter(typeof(EnumDescriptionConverter))]
        [DefaultValue(RSP_IF_Mode.Low_IF)]
        public RSP_IF_Mode DeviceIF_Mode {
            get => device_IFMode; set {
                device_IFMode = value;
                if (value == RSP_IF_Mode.Low_IF) {
                    DeviceSampleRate = RSP_SampleRate.SR_Full_6;
                } else {
                    DeviceSampleRate = RSP_SampleRate.SR_Full_8;
                    device_IFBW = RSP_IFBW.IF_BW_1_536;
                }
            }
        }

        [DisplayName("3. IF Bandwidth")]
        [Description("Receiver's IF bandwidth")]
        [TypeConverter(typeof(EnumDescriptionConverter))]
        [DefaultValue(RSP_IFBW.IF_BW_1_536)]
        public RSP_IFBW DeviceIF_BW {
            get => device_IFBW; set {
                device_IFBW = value;
                if (value == RSP_IFBW.IF_BW_1_536 && DeviceSampleRate == RSP_SampleRate.SR_Full_2) {
                    DeviceSampleRate = RSP_SampleRate.SR_Full_6;
                }
                if (value != RSP_IFBW.IF_BW_1_536 && (DeviceSampleRate == RSP_SampleRate.SR_Full_6 || DeviceSampleRate == RSP_SampleRate.SR_Full_8)) {
                    DeviceSampleRate = RSP_SampleRate.SR_Full_2;
                }
            }
        }

        [DisplayName("4. Sampling Rate")]
        [Description("Receiver's output sampling rate")]
        [TypeConverter(typeof(EnumDescriptionConverter))]
        [DefaultValue(RSP_SampleRate.SR_Full_6)]
        public RSP_SampleRate DeviceSampleRate {
            get => device_SampleRate; set {
                device_SampleRate = value;
                if (value != RSP_SampleRate.SR_Full_8 && value != RSP_SampleRate.SR_Full_6) {
                    DeviceIF_BW = RSP_IFBW.IF_BW_0_600;
                }
                if (value != RSP_SampleRate.SR_Full_8 && DeviceIF_Mode == RSP_IF_Mode.Zero_IF) {
                    DeviceIF_Mode = RSP_IF_Mode.Low_IF;
                }
                if (value != RSP_SampleRate.SR_Full_2) {
                    DeviceIF_BW = RSP_IFBW.IF_BW_1_536;
                }

                switch (value) {
                    case RSP_SampleRate.SR_Full_8:
                        Device_IF_Value = sdrplay_api_If_kHzT.sdrplay_api_IF_2_048;
                        return;
                    case RSP_SampleRate.SR_Full_6:
                        Device_IF_Value = sdrplay_api_If_kHzT.sdrplay_api_IF_1_620;
                        return;
                    case RSP_SampleRate.SR_Full_2:
                        Device_IF_Value = sdrplay_api_If_kHzT.sdrplay_api_IF_0_450;
                        return;
                }

                if (DeviceIF_Mode == RSP_IF_Mode.Zero_IF)
                    Device_IF_Value = sdrplay_api_If_kHzT.sdrplay_api_IF_Zero;

            }
        }

        [DisplayName("5. LNA Gain reduce Level")]
        [Description("Please select LNA reduce level from 0 to 8, where 0 - maximum gain, 8 - maximum reduce.")]
        [DefaultValue(5)]
        public int DeviceLNALevel {
            get => deviceLNALevel; set {
                //TODO: check range for all frequency range of RSP2
                if (value < 0 || value > 8)
                    value = 6;

                deviceLNALevel = value;
            }
        }

        [DisplayName("Enable notch filter")]
        [Description("Please select FM/MW notch filter status")]
        [DefaultValue(true)]
        public bool DeviceNotchFilter { get; set; }

        [DisplayName("Device AGC")]
        [Description("AGC Enabled / Type")]
        [TypeConverter(typeof(EnumDescriptionConverter))]
        [DefaultValue(RSP_AGC.AGC_Auto)]
        [ReadOnly(true)]
        public RSP_AGC DeviceAGC { get; set; }

        [DisplayName("Device serial number")]
        [Description("S/N from attached device")]
        [DefaultValue("")]
        [ReadOnly(true)]
        public string SelectedDeviceSN { get; set; }

        [DisplayName("Device model")]
        [Description("Full model name of attached device")]
        [DefaultValue("")]
        [ReadOnly(true)]
        public string SelectedDeviceModel { get; set; }

        [Browsable(false)]
        public sdrplay_api_If_kHzT Device_IF_Value { get; set; }

        [Browsable(false)]
        public long[] Frequencies { get; set; } = new long[] { 14021000, 105000000 };
    }
}
