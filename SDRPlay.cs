using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using VE3NEA.HamCockpit.DspFun;
using VE3NEA.HamCockpit.PluginAPI;
using static UN7ZO.HamCockpitPlugins.SDRPlaySource.NativeMethods;

namespace UN7ZO.HamCockpitPlugins.SDRPlaySource {
    [Export(typeof(IPlugin))]
    [Export(typeof(ISignalSource))]
    unsafe class SDRPlaySource : IPlugin, ISignalSource, IDisposable {
        public int FINAL_SAMPLING_RATE = 2000000;
        private int bufAccumCount = 0;
        private int BUF_ACCUM_LIMIT = 120;

        /*          Conditions for LIF down-conversion to be enabled for all RSPs in single tuner mode:
*          (fsHz == 8000000) && (bwType == sdrplay_api_BW_1_536) && (ifType == sdrplay_api_IF_2_048) - работает ОК 504 сэмпла,   Final SR = 2000000        
*          (fsHz == 6000000) && (bwType <= sdrplay_api_BW_1_536) && (ifType == sdrplay_api_IF_1_620) - работает ОК 504 сэмпла,   Final SR = 2000000
*          (fsHz == 2000000) && (bwType <= sdrplay_api_BW_0_200) && (ifType == sdrplay_api_IF_0_450) - работает ОК 1008 семплов, Final SR = 500000
*          (fsHz == 2000000) && (bwType <= sdrplay_api_BW_0_300) && (ifType == sdrplay_api_IF_0_450) - работает ОК 1008 семплов, Final SR = 500000
*          (fsHz == 2000000) && (bwType == sdrplay_api_BW_0_600) && (ifType == sdrplay_api_IF_0_450) - работает ОК 1008 семплов, Final SR = 1000000
*/

        private Settings _settings = new Settings();
        private readonly RingBuffer buffer;
        public static short[] Ibuf;
        public static short[] Qbuf;

        private SDRPlayDevice _device = null;
        private sdrplay_api_DeviceParamsT _deviceParams;
        private bool _IsStopping = false;

        //visual panels
        private readonly SDRPlayStatusInfoPanel sdrPlayStatusInfoPanel = new SDRPlayStatusInfoPanel();
        private readonly SDRPlayDeviceControlPanel sdrPlayDeviceControlPanel = new SDRPlayDeviceControlPanel();

        public SDRPlaySource() {
            Debug.WriteLine("SDRPlaySource - constructor");
            buffer = new RingBuffer(FINAL_SAMPLING_RATE);
            Format = new SignalFormat(FINAL_SAMPLING_RATE, true, false, 1, -48000, 48000, 0);
            buffer.SamplesAvailable += (o, e) => SamplesAvailable?.Invoke(this, e);

            _device = SDRPlayDevice.GetInstance();
            _device.SamplesAvailableEvent += StreamCallback;
            _device.OverloadDetectedEvent += OverloadCallback;
            _device.GainChangeEvent += GainChangeCallback;

            sdrPlayDeviceControlPanel.setControlLink(this);
            ToolStrip.AutoSize = false;
            
            ToolStrip.Items.Add(new ToolStripControlHost(sdrPlayStatusInfoPanel));
            ToolStrip.Items.Add(new ToolStripSeparator());
            ToolStrip.Items.Add(new ToolStripControlHost(sdrPlayDeviceControlPanel));

            ToolStrip.Height = 34;
            ToolStrip.Width = 180;
        }

        private void OverloadCallback(bool visible) {
            sdrPlayDeviceControlPanel.setOverloadLabelVisible(visible);

            if (visible) {
                sdrPlayStatusInfoPanel.setSDRDeviceStatusLabelColor(Color.Red);
            } else {
                sdrPlayStatusInfoPanel.setSDRDeviceStatusLabelColor(Color.Green);
            }
        }

        private void GainChangeCallback(uint gRdB, uint lnaGRdB, double systemGain) {
            sdrPlayDeviceControlPanel.setLNAGRlabelText(Convert.ToString(lnaGRdB) + " dB");
        }

        public void Dispose() {
            sdrPlayStatusInfoPanel.Dispose();
            sdrPlayDeviceControlPanel.Dispose();
            ToolStrip.Dispose();
        }

        internal void setDeviceLNAGrLevel(int value) {
            _device.setLNAGrLevel(value);
            _settings.DeviceLNALevel = value;
        }

        internal void setDeviceAntenna(int value) {
            _device.selectAntenna(value);
            _settings.DeviceAntenna = (RSP_ANT)value;
        }

        internal void setDeviceNotchEnabled(bool value) {
            _device.setNotchEnabled(value);
            _settings.DeviceNotchFilter = value;
        }

        #region IPlugin

        public string Name => "SDRPlay RSP Device";
        public string Author => "UN7ZO";
        public bool Enabled { get; set; }

        public object Settings
        {
            get => getSettings();
            set => setSettings(value as Settings);
        }

        private void setSettings(Settings newSettings) {
            _settings = newSettings;
        }

        private object getSettings() {
            return _settings;
        }

        public ToolStrip ToolStrip { get; } = new ToolStrip();
        public ToolStripItem StatusItem => null;

        #endregion

        #region ISampleStream

        public SignalFormat Format { get; private set; }

        public int Read(float[] buffer, int offset, int count)
        {
            return this.buffer.Read(buffer, offset, count);
        }

        public event EventHandler<SamplesAvailableEventArgs> SamplesAvailable;

        #endregion

        #region ISignalSource

        public void Initialize() {
            Debug.WriteLine("ISignalSource - Initialize");

            FINAL_SAMPLING_RATE = correctSoftwareSampleRate();
            BUF_ACCUM_LIMIT = correctBufAccumLimit();
            buffer.Resize(FINAL_SAMPLING_RATE);

            Format = new SignalFormat(FINAL_SAMPLING_RATE, true, false, 1, -48000, 48000, 0);

            //1008 samples              504 samples
            //60480 - 60                30240 - 60
            //120960 - 120              60480 - 120

            Ibuf = new short[60480];
            Qbuf = new short[60480];
        }

        private int correctBufAccumLimit() {
            if (_settings.DeviceSampleRate == RSP_SampleRate.SR_Full_2)
                return 60;
            else
                return 120;
        }

        private int correctSoftwareSampleRate() {
            int sampRate = 2000000;

            if (_settings.DeviceSampleRate == RSP_SampleRate.SR_Full_2)
                if (_settings.DeviceIF_BW == RSP_IFBW.IF_BW_0_600)
                    sampRate = 1000000;
                else
                    sampRate = 500000;

            return sampRate;
        }

        public bool Active {
            get => GetActive();
            set => SetActive(value);
        }

        private bool GetActive() {
            bool result = false;

            if (_device != null)
                result = _device.IsStreaming;

            return result;
        }

        private void SetActive(bool value) {
            if (value == Active) return;

            if (value) {
                _IsStopping = false;
                _device.InitializeDevice();

                if (_device.getDeviceCount() == 0) {
                    throw new ApplicationException("SDRPlay devices not found.");
                }

                _deviceParams = _device.selectDevice(_settings.SelectedDeviceIndex, true);
                _settings.SelectedDeviceSN = _device.SelectedDeviceSN;
                _settings.SelectedDeviceModel = _device.SelectedDeviceHwVerStr;

                if (_device.IsSelected) {                    
                    sdrPlayDeviceControlPanel.setPanelVisible(true);
                    sdrPlayDeviceControlPanel.setInitialLNAGRLevel(_settings.DeviceLNALevel);
                    sdrPlayDeviceControlPanel.setInitialNotchStatus(_settings.DeviceNotchFilter);
                    sdrPlayDeviceControlPanel.setInitialAntenna((int)_settings.DeviceAntenna);

                    sdrPlayStatusInfoPanel.setSDRDeviceNameLabelText(_device.SelectedDeviceHwVerStr);// + " (SN: " + _device.SelectedDeviceSN + ")");
                    sdrPlayStatusInfoPanel.setSDRDeviceStatusLabelText("...starting...");
                    sdrPlayStatusInfoPanel.setSDRDeviceStatusLabelColor(Color.Orange);
                    ToolStrip.Width = sdrPlayStatusInfoPanel.getSDRDeviceNameLabelWidth();
                    Application.DoEvents();

                    byte notchEnabled = _settings.DeviceNotchFilter == true ? (byte)1 : (byte)0;
                    _device.ConfigureDevice(_deviceParams, GetDialFrequency(0), _settings.DeviceSampleRate, _settings.DeviceIF_BW, (int)_settings.Device_IF_Value, notchEnabled, _settings.DeviceLNALevel, _settings.DeviceAntenna);

                    //need to be reseted, if we stop device and select another samplerate - buf accum limit may be changed from 120 to 60.
                    bufAccumCount = 0;

                    _device.StartSampling();
                    SetDialFrequency(GetDialFrequency(0), 0);
                    
                    sdrPlayStatusInfoPanel.setSDRDeviceStatusLabelText("ACTIVE");
                    sdrPlayStatusInfoPanel.setSDRDeviceStatusLabelColor(Color.Green);
                    Application.DoEvents();
                }
            } else {
                _IsStopping = true;

                sdrPlayDeviceControlPanel.setPanelVisible(false);
                sdrPlayStatusInfoPanel.setSDRDeviceStatusLabelText("...stopping...");
                sdrPlayStatusInfoPanel.setSDRDeviceStatusLabelColor(Color.Orange);
                Application.DoEvents();

                _device.StopSampling();
                Application.DoEvents();

                sdrPlayStatusInfoPanel.setSDRDeviceStatusLabelText("STOPPED");
                sdrPlayStatusInfoPanel.setSDRDeviceStatusLabelColor(Color.Maroon);
                _settings.SelectedDeviceSN = "";
                _settings.SelectedDeviceModel = "";
            }
        }

        public event EventHandler<StoppedEventArgs> Stopped;

        #endregion

        private unsafe void StreamCallback(object sender, ComplexSamplesEventArgs e) {
            uint numSamples = e.Length;

            int j = ((int) (bufAccumCount * numSamples));
            int i;

            //Accumulate some number of samples
            for (i = 0; i < numSamples; i++) {
                Ibuf[j] = (e.iBuffer[i]);
                Qbuf[j] = (e.qBuffer[i]);
                j++;
            }

            bufAccumCount++;

            //once accumulated - write it to RingBuffer
            if (bufAccumCount == BUF_ACCUM_LIMIT) {
                short[] receivedBytes = new short[numSamples * bufAccumCount * 2];
                j = 0;
                for (i = 0; i < numSamples * bufAccumCount; i++) {
                    receivedBytes[j++] = Ibuf[i];
                    receivedBytes[j++] = Qbuf[i];
                }

                buffer.WriteShort(receivedBytes, receivedBytes.Length);
                bufAccumCount = 0;
            }
        }

        #region ITuner

        public long GetDialFrequency(int channel = 0)
        {
            Debug.WriteLine(
                "GetDialFrequency called, channel: " + channel + ", freq: " + _settings.Frequencies[channel]);
            return _settings.Frequencies[channel];
        }

        public void SetDialFrequency(long frequency, int channel)
        {
            _settings.Frequencies[channel] = frequency;
            if (Active)
                try
                {
                    //send to radio
                    _device.SetFrequency(frequency);
                    //notify host application                   
                    Tuned?.Invoke(this, new EventArgs());
                }
                catch (Exception e)
                {
                    //device.Dispose();
                    var exception = new Exception($"Command SetDialFrequency failed:\n\n{e.Message}");
                    Stopped?.Invoke(this, new StoppedEventArgs(exception));
                }
        }

        public event EventHandler Tuned;

        #endregion
    }
}