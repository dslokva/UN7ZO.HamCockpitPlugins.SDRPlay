using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using System.Windows.Forms;
using CSIntel.Ipp;
using static UN7ZO.HamCockpitPlugins.SDRPlaySource.NativeMethods;
using static UN7ZO.HamCockpitPlugins.SDRPlaySource.SDRplayAPI_Callback;

namespace UN7ZO.HamCockpitPlugins.SDRPlaySource {

    public delegate void SamplesAvailableDelegate<ArgsType>(object sender, ArgsType e);
    public delegate void OverloadDetectedDelegate(bool ovrCorrected);
    public delegate void GainChangeDelegate(uint gRdB, uint lnaGRdB, double systemGain);
    

    public unsafe sealed class ComplexSamplesEventArgs : EventArgs {
        public uint Length { get; set; }
        public float[] iBuffer { get; set; }
        public float[] qBuffer { get; set; }
    }

    unsafe class SDRPlayDevice {
        
        private static sdrplay_api_DeviceT _device;
        public sdrplay_api_DeviceT[] devices { get; set; } = new sdrplay_api_DeviceT[6];
        public lightDeviceInfo[] availableDevicesList { get; set; } = null;

        private sdrplay_api_CallbackFnsT cbFns = new sdrplay_api_CallbackFnsT();
        public event SamplesAvailableDelegate<ComplexSamplesEventArgs> SamplesAvailableEvent;
        public event OverloadDetectedDelegate OverloadDetectedEvent;
        public event GainChangeDelegate GainChangeEvent;

        private bool _isStreaming;
        private bool _isSelected;

        public bool IsStreaming {
            get { return _isStreaming; }
        }

        public bool IsSelected {
            get { return _isSelected; }
        }

        public string SelectedDeviceSN { get; private set; }
        public string SelectedDeviceHwVerStr { get; private set; }

        private static readonly SDRPlayDevice instance = new SDRPlayDevice();

        public static SDRPlayDevice GetInstance() {
            return instance;
        }

        private SDRPlayDevice() {
            Debug.WriteLine("SDRPlayDevice - constructor");

            // Assign callback functions to be passed toSDRplayAPI.sdrplay_api_Init()
            cbFns.StreamACbFn = StreamACallback;
            cbFns.EventCbFn = EventCallback;

            _isSelected = false;

            InitializeDevice();
        }

        public int getDeviceCount() {
            int length = 0;

            if (devices != null)
                foreach (sdrplay_api_DeviceT _dev in devices) {
                    if (_dev.hwVer > 0)
                        length++;
                }

            return length;
        }

        public lightDeviceInfo[] getDeviceList() {
            availableDevicesList = new lightDeviceInfo[getDeviceCount()];
            int i = 0;
            if (devices != null)
                foreach (sdrplay_api_DeviceT _dev in devices) {
                    if (_dev.hwVer == SDRPLAY_RSP2_ID) {
                        availableDevicesList[i].deviceName = hwVerStr(_dev.hwVer);
                        availableDevicesList[i].deviceSN = CStrNullTermToString(_dev.SerNo);
                        i++;
                    }                        
                }
            return availableDevicesList;
        }

        private static string CStrNullTermToString(char[] cStrNullTerm) {
            return (new string(cStrNullTerm)).TrimEnd((char)0);
        }

        private string hwVerStr(byte hwVer) {
            switch (hwVer) {
                case SDRPLAY_RSP1_ID: return "SDRPlay RSP1";
                case SDRPLAY_RSP1A_ID: return "SDRPlay RSP1A";
                case SDRPLAY_RSP2_ID: return "SDRPlay RSP2";
                case SDRPLAY_RSPduo_ID: return "SDRPlay RSPduo";
                case SDRPLAY_RSPdx_ID: return "SDRPlay RSPdx";
                default: return "";
            }
        }

        public void InitializeDevice() {
            sdrplay_api_ErrT err;

            if ((err = sdrplay_api_Open()) != sdrplay_api_ErrT.sdrplay_api_Success) {
                Debug.WriteLine("sdrplay_api_Open failed {0}", Marshal.PtrToStringAnsi(sdrplay_api_GetErrorString(err)));
                MessageBox.Show(string.Format("SDRPlay API service open failed: {0}", Marshal.PtrToStringAnsi(sdrplay_api_GetErrorString(err))), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            } else {
                Debug.WriteLine("API opened successfully");

                // Enable debug logging output
                if ((err = sdrplay_api_DebugEnable(IntPtr.Zero, 1)) != sdrplay_api_ErrT.sdrplay_api_Success) {
                    Debug.WriteLine("sdrplay_api_DebugEnable failed {0}", Marshal.PtrToStringAnsi(sdrplay_api_GetErrorString(err)));
                }

                // Check API versions match
                float ver;
                if ((err = sdrplay_api_ApiVersion(out ver)) != sdrplay_api_ErrT.sdrplay_api_Success) {
                    Debug.WriteLine("sdrplay_api_ApiVersion failed {0}", Marshal.PtrToStringAnsi(sdrplay_api_GetErrorString(err)));
                }

                if (ver == 0) {
                    MessageBox.Show("SDRPlay API service not installed or not accessible.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                if (ver != SDRPLAY_API_VERSION_307 && ver != SDRPLAY_API_VERSION_306) {
                    sdrplay_api_Close();
                    MessageBox.Show(string.Format("API version don't match (expected={0:0.00} dll={0:0.00})", SDRPLAY_API_VERSION_307, ver), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Debug.WriteLine("API version: {0:0.000}", ver);

                // Fetch list of available devices
                if ((err = sdrplay_api_GetDevices(devices, out uint ndev, 6)) != sdrplay_api_ErrT.sdrplay_api_Success) {
                    sdrplay_api_Close();
                    //throw new Exception("sdrplay_api_GetDevices failed: " + Marshal.PtrToStringAnsi(sdrplay_api_GetErrorString(err)));
                }
                Debug.WriteLine("num devs: {0:d}", ndev);
            }
        }

        public sdrplay_api_DeviceParamsT selectDevice(int devidx, bool selectDevice) {
            sdrplay_api_ErrT err;
            _device = devices[devidx];
            sdrplay_api_DeviceParamsT deviceParams;

            string serialNumber = CStrNullTermToString(_device.SerNo);

            if (_device.hwVer != SDRPLAY_RSP2_ID) {
                sdrplay_api_Close();
                throw new ApplicationException(string.Format("Unsupported RSP device: %02X", _device.hwVer));
            }

            if (selectDevice) {
                // Lock API while device selection is performed
                sdrplay_api_LockDeviceApi();

                // Select chosen device
                if ((err = sdrplay_api_SelectDevice(ref _device)) != sdrplay_api_ErrT.sdrplay_api_Success) {
                    sdrplay_api_UnlockDeviceApi();
                    sdrplay_api_Close();
                    throw new ApplicationException("sdrplay_api_SelectDevice failed: " + Marshal.PtrToStringAnsi(sdrplay_api_GetErrorString(err)));
                } else {
                    Debug.WriteLine("Selected RSP device: " + hwVerStr(_device.hwVer) + ", S/N: " + serialNumber);
                    SelectedDeviceSN = serialNumber;
                    SelectedDeviceHwVerStr = hwVerStr(_device.hwVer);
                    devices[devidx] = _device; //store pointer for selected device back to array
                    _isSelected = true;
                    // Unlock API now that device is selected
                    sdrplay_api_UnlockDeviceApi();
                }
            } else {
                //Debug.WriteLine("Picked up previously selected device. Use this with danger :)");
                SelectedDeviceSN = serialNumber;
                SelectedDeviceHwVerStr = hwVerStr(_device.hwVer);
                _isSelected = true;
            }

            // Retrieve device parameters so they can be changed if wanted
            IntPtr deviceParamsPtr = IntPtr.Zero;
            if ((err = sdrplay_api_GetDeviceParams(_device.dev, out deviceParamsPtr)) != sdrplay_api_ErrT.sdrplay_api_Success) {
                sdrplay_api_Close();
                throw new ApplicationException("sdrplay_api_GetDeviceParams failed: " + Marshal.PtrToStringAnsi(sdrplay_api_GetErrorString(err)));
            }
            // Check for NULL pointers before changing settings
            if (deviceParamsPtr == IntPtr.Zero) {
                sdrplay_api_Close();
                throw new ApplicationException("sdrplay_api_GetDeviceParams returned NULL deviceParams pointer");
            }
            deviceParams = Marshal.PtrToStructure<sdrplay_api_DeviceParamsT>(deviceParamsPtr);

            return deviceParams;
        }

        public void ConfigureDevice(sdrplay_api_DeviceParamsT deviceParams, long dialFreq, RSP_SampleRate devicefsHz, RSP_IFBW bwType, int ifType, byte notchFilterEnabled, int lnaLevel, RSP_ANT antennaNum) {
            // Configure dev parameters
            if (deviceParams.devParams != IntPtr.Zero) {
                //this makes a copy of the unmanaged structure allocated in the API
                sdrplay_api_DevParamsT devParams = Marshal.PtrToStructure<sdrplay_api_DevParamsT>(deviceParams.devParams);

                devParams.fsFreq.fsHz = (double)devicefsHz;

                //to apply these changes, overwrite the unmanaged structure memory with the updated managed structure
                Marshal.StructureToPtr<sdrplay_api_DevParamsT>(devParams, deviceParams.devParams, false);
            } else {
                sdrplay_api_Close();
                throw new Exception("NULL devParams structure");
            }

            if (deviceParams.rxChannelA != IntPtr.Zero) {
                sdrplay_api_RxChannelParamsT rxParamsA = Marshal.PtrToStructure<sdrplay_api_RxChannelParamsT>(deviceParams.rxChannelA);

                rxParamsA.tunerParams.rfFreq.rfHz = (float)dialFreq;
                rxParamsA.rsp2TunerParams.rfNotchEnable = notchFilterEnabled;
                rxParamsA.rsp2TunerParams.antennaSel = (SDRplayAPI_RSP2.sdrplay_api_Rsp2_AntennaSelectT)antennaNum;

                if (ifType == (int)sdrplay_api_If_kHzT.sdrplay_api_IF_Zero) {
                    rxParamsA.tunerParams.loMode = SDRplayAPI_Tuner.sdrplay_api_LoModeT.sdrplay_api_LO_Undefined;
                } else {
                    rxParamsA.tunerParams.loMode = SDRplayAPI_Tuner.sdrplay_api_LoModeT.sdrplay_api_LO_Auto;
                }
                rxParamsA.tunerParams.bwType = (SDRplayAPI_Tuner.sdrplay_api_Bw_MHzT)bwType;
                rxParamsA.tunerParams.ifType = (SDRplayAPI_Tuner.sdrplay_api_If_kHzT)ifType;
                rxParamsA.tunerParams.gain.gRdB = 40;
                rxParamsA.tunerParams.gain.LNAstate = (byte)lnaLevel;
                rxParamsA.ctrlParams.agc.enable = sdrplay_api_AgcControlT.sdrplay_api_AGC_CTRL_EN;

                Marshal.StructureToPtr<sdrplay_api_RxChannelParamsT>(rxParamsA, deviceParams.rxChannelA, false);
            } else {
                sdrplay_api_Close();
                throw new Exception("NULL rx channel A structure");
            }
        }

        internal void SetFrequency(long frequency) {
            sdrplay_api_ErrT err;
            sdrplay_api_DeviceParamsT deviceParams = selectDevice(0, false);

            if (deviceParams.rxChannelA != IntPtr.Zero) {
                sdrplay_api_RxChannelParamsT rxParamsA = Marshal.PtrToStructure<sdrplay_api_RxChannelParamsT>(deviceParams.rxChannelA);
                rxParamsA.tunerParams.rfFreq.rfHz = (double)frequency;
                Marshal.StructureToPtr<sdrplay_api_RxChannelParamsT>(rxParamsA, deviceParams.rxChannelA, false);
            }

            if (_isStreaming)
                if ((err = sdrplay_api_Update(_device.dev, _device.tuner, sdrplay_api_ReasonForUpdateT.sdrplay_api_Update_Tuner_Frf, sdrplay_api_ReasonForUpdateExtension1T.sdrplay_api_Update_Ext1_None)) != sdrplay_api_ErrT.sdrplay_api_Success) {
                    Debug.WriteLine("sdrplay_api_Update sdrplay_api_Update_Tuner_Gr failed: " + sdrplay_api_GetErrorString(err));
                } else {
                    Debug.WriteLine("LO changed to: " + frequency);
                }
        }

        internal void SelectAntenna(int antNum) {
            sdrplay_api_ErrT err;
            sdrplay_api_DeviceParamsT deviceParams = selectDevice(0, false);

            if (deviceParams.rxChannelA != IntPtr.Zero) {
                sdrplay_api_RxChannelParamsT rxParamsA = Marshal.PtrToStructure<sdrplay_api_RxChannelParamsT>(deviceParams.rxChannelA);
                rxParamsA.rsp2TunerParams.antennaSel = (SDRplayAPI_RSP2.sdrplay_api_Rsp2_AntennaSelectT)antNum;
                Marshal.StructureToPtr<sdrplay_api_RxChannelParamsT>(rxParamsA, deviceParams.rxChannelA, false);
            }

            if (_isStreaming)
                if ((err = sdrplay_api_Update(_device.dev, _device.tuner, sdrplay_api_ReasonForUpdateT.sdrplay_api_Update_Rsp2_AntennaControl, sdrplay_api_ReasonForUpdateExtension1T.sdrplay_api_Update_Ext1_None)) != sdrplay_api_ErrT.sdrplay_api_Success) {
                    Debug.WriteLine("sdrplay_api_Update AntennaControl failed: " + sdrplay_api_GetErrorString(err));
                } else {
                    Debug.WriteLine("Antenna selected: " + antNum);
                }
        }

        internal void setLNAGrLevel(int value) {
            sdrplay_api_ErrT err;
            sdrplay_api_DeviceParamsT deviceParams = selectDevice(0, false);

            if (deviceParams.rxChannelA != IntPtr.Zero) {
                sdrplay_api_RxChannelParamsT rxParamsA = Marshal.PtrToStructure<sdrplay_api_RxChannelParamsT>(deviceParams.rxChannelA);
                rxParamsA.tunerParams.gain.LNAstate = (byte)value;
                Marshal.StructureToPtr<sdrplay_api_RxChannelParamsT>(rxParamsA, deviceParams.rxChannelA, false);
            }

            if (_isStreaming)
                if ((err = sdrplay_api_Update(_device.dev, _device.tuner, sdrplay_api_ReasonForUpdateT.sdrplay_api_Update_Tuner_Gr, sdrplay_api_ReasonForUpdateExtension1T.sdrplay_api_Update_Ext1_None)) != sdrplay_api_ErrT.sdrplay_api_Success) {
                    Debug.WriteLine("sdrplay_api_Update sdrplay_api_Update_Tuner_Gr failed: " + sdrplay_api_GetErrorString(err));
                } else {
                    Debug.WriteLine("LNAState (gain reduce) changed to: " + value);
                }
        }

        internal void setNotchEnabled(bool value) {
            sdrplay_api_ErrT err;
            sdrplay_api_DeviceParamsT deviceParams = selectDevice(0, false);

            sdrplay_api_RxChannelParamsT rxParamsA = Marshal.PtrToStructure<sdrplay_api_RxChannelParamsT>(deviceParams.rxChannelA);

            byte notchFilterEnabled = 0;
            if (value) notchFilterEnabled = 1;
            rxParamsA.rsp2TunerParams.rfNotchEnable = notchFilterEnabled;

            Marshal.StructureToPtr<sdrplay_api_RxChannelParamsT>(rxParamsA, deviceParams.rxChannelA, false);

            if (_isStreaming)
                if ((err = sdrplay_api_Update(_device.dev, _device.tuner, sdrplay_api_ReasonForUpdateT.sdrplay_api_Update_Rsp2_RfNotchControl, sdrplay_api_ReasonForUpdateExtension1T.sdrplay_api_Update_Ext1_None)) != sdrplay_api_ErrT.sdrplay_api_Success) {
                    Debug.WriteLine("sdrplay_api_Update sdrplay_api_Update_Rsp2_RfNotchControl failed: " + sdrplay_api_GetErrorString(err));
                } else {
                    Debug.WriteLine("FM/MW notches changed to: " + value);
                }
        }

        public void StartSampling() {
            sdrplay_api_ErrT err;

            if (_isStreaming) 
                return;

                if ((err = sdrplay_api_Init(_device.dev, ref cbFns, IntPtr.Zero)) != sdrplay_api_ErrT.sdrplay_api_Success) {
                    Debug.WriteLine("sdrplay_api_Init failed {0}", Marshal.PtrToStringAnsi(sdrplay_api_GetErrorString(err)));

                IntPtr lastErrorPtr = sdrplay_api_GetLastError(IntPtr.Zero);
                if (lastErrorPtr != IntPtr.Zero) {
                    sdrplay_api_ErrorInfoT errInfo = Marshal.PtrToStructure<sdrplay_api_ErrorInfoT>(lastErrorPtr);
                    Debug.WriteLine("Error in {0}: {1}(): line {2:d}: {3}",
                        CStrNullTermToString(errInfo.file),
                        CStrNullTermToString(errInfo.function),
                        errInfo.line,
                        CStrNullTermToString(errInfo.message));
                }

                sdrplay_api_Close();

            } else {
                _isStreaming = true;
            }
        }

        internal void StopSampling() {
            sdrplay_api_ErrT err;
            if ((err = sdrplay_api_Uninit(_device.dev)) != sdrplay_api_ErrT.sdrplay_api_Success) {
                sdrplay_api_Close();
                throw new Exception("sdrplay_api_Uninit failed: " + Marshal.PtrToStringAnsi(sdrplay_api_GetErrorString(err)));
            }
            sdrplay_api_LockDeviceApi();

            // Release device (make it available to other applications)
            sdrplay_api_ReleaseDevice(ref _device);
            _isSelected = false;

            sdrplay_api_UnlockDeviceApi();

            if ((err = sdrplay_api_Close()) != sdrplay_api_ErrT.sdrplay_api_Success) {
                throw new Exception("Error closing sdrplay api: " + Marshal.PtrToStringAnsi(sdrplay_api_GetErrorString(err)));
            }

            _isStreaming = false;
        }

        public object bufferLock;
        protected virtual void OnComplexSamplesAvailable(IntPtr xi, IntPtr xq, sdrplay_api_StreamCbParamsT Params, uint numSamples) {
            var handler = SamplesAvailableEvent;
            if (handler != null) {
                var e = new ComplexSamplesEventArgs();
                var iPrepareBuffer = new short[numSamples];
                var qPrepareBuffer = new short[numSamples];

                e.iBuffer = new float[numSamples];
                e.qBuffer = new float[numSamples];

                bufferLock = new object();
                lock (bufferLock) {
                    //read buffer from memory pointer to short[] structure
                    Marshal.Copy(xi, iPrepareBuffer, 0, (int)numSamples);
                    Marshal.Copy(xq, qPrepareBuffer, 0, (int)numSamples);

                    //convert it
                    fixed (short* pInBuffer = iPrepareBuffer)
                    fixed (float* pOutBuffer = e.iBuffer)
                        sp.ippsConvert_16s32f_Sfs(pInBuffer, pOutBuffer, (int) numSamples, 15);

                    fixed (short* pInBuffer = qPrepareBuffer)
                    fixed (float* pOutBuffer = e.qBuffer)
                        sp.ippsConvert_16s32f_Sfs(pInBuffer, pOutBuffer, (int)numSamples, 15);
                }

                e.Length = numSamples;
                //send event to main class
                handler(this, e);
            }
        }

        #region Callback functions
        private void StreamACallback(IntPtr xi, IntPtr xq, ref sdrplay_api_StreamCbParamsT Params, uint numSamples, uint reset, IntPtr cbContext) {
            OnComplexSamplesAvailable(xi, xq, Params, numSamples);
        }

        private void EventCallback(sdrplay_api_EventT eventId, SDRplayAPI_Tuner.sdrplay_api_TunerSelectT tuner, ref sdrplay_api_EventParamsT Params, IntPtr cbContext) {
            switch (eventId) {
                case sdrplay_api_EventT.sdrplay_api_GainChange:
                //    Debug.WriteLine("sdrplay_api_EventCb: {0}, tuner={1} gRdB={2:d} lnaGRdB={3:d} systemGain={4:0.00}",
                //        "sdrplay_api_GainChange", (tuner == SDRplayAPI_Tuner.sdrplay_api_TunerSelectT.sdrplay_api_Tuner_A) ? "sdrplay_api_Tuner_A" :
                //        "sdrplay_api_Tuner_B", Params.gainParams.gRdB, Params.gainParams.lnaGRdB,
                //        Params.gainParams.currGain);
                    GainChangeEvent(Params.gainParams.gRdB, Params.gainParams.lnaGRdB, Params.gainParams.currGain);
                    break;
                case sdrplay_api_EventT.sdrplay_api_PowerOverloadChange:
                 //   Debug.WriteLine("sdrplay_api_PowerOverloadChange: tuner={0} powerOverloadChangeType={1}",
                 //       (tuner == SDRplayAPI_Tuner.sdrplay_api_TunerSelectT.sdrplay_api_Tuner_A) ? "sdrplay_api_Tuner_A" : "sdrplay_api_Tuner_B",
                 //       (Params.powerOverloadParams.powerOverloadChangeType == sdrplay_api_PowerOverloadCbEventIdT.sdrplay_api_Overload_Detected)
                 //       ? "sdrplay_api_Overload_Detected" : "sdrplay_api_Overload_Corrected");
                    
                    OverloadDetectedEvent(Params.powerOverloadParams.powerOverloadChangeType == sdrplay_api_PowerOverloadCbEventIdT.sdrplay_api_Overload_Detected);
                    
                    // Send update message to acknowledge power overload message received
                    sdrplay_api_Update(_device.dev, tuner, sdrplay_api_ReasonForUpdateT.sdrplay_api_Update_Ctrl_OverloadMsgAck,
                        sdrplay_api_ReasonForUpdateExtension1T.sdrplay_api_Update_Ext1_None);
                    break;

/*                case sdrplay_api_EventT.sdrplay_api_RspDuoModeChange:
                    Debug.WriteLine("sdrplay_api_EventCb: {0}, tuner={1} modeChangeType={2}",
                        "sdrplay_api_RspDuoModeChange", (tuner == SDRplayAPI_Tuner.sdrplay_api_TunerSelectT.sdrplay_api_Tuner_A) ?
                        "sdrplay_api_Tuner_A" : "sdrplay_api_Tuner_B",
                        (Params.rspDuoModeParams.modeChangeType == sdrplay_api_RspDuoModeCbEventIdT.sdrplay_api_MasterInitialised) ?
                        "sdrplay_api_MasterInitialised" :
                        (Params.rspDuoModeParams.modeChangeType == sdrplay_api_RspDuoModeCbEventIdT.sdrplay_api_SlaveAttached) ?
                        "sdrplay_api_SlaveAttached" :
                        (Params.rspDuoModeParams.modeChangeType == sdrplay_api_RspDuoModeCbEventIdT.sdrplay_api_SlaveDetached) ?
                        "sdrplay_api_SlaveDetached" :
                        (Params.rspDuoModeParams.modeChangeType == sdrplay_api_RspDuoModeCbEventIdT.sdrplay_api_SlaveInitialised) ?
                        "sdrplay_api_SlaveInitialised" :
                        (Params.rspDuoModeParams.modeChangeType == sdrplay_api_RspDuoModeCbEventIdT.sdrplay_api_SlaveUninitialised) ?
                        "sdrplay_api_SlaveUninitialised" :
                        (Params.rspDuoModeParams.modeChangeType == sdrplay_api_RspDuoModeCbEventIdT.sdrplay_api_MasterDllDisappeared) ?
                        "sdrplay_api_MasterDllDisappeared" :
                        (Params.rspDuoModeParams.modeChangeType == sdrplay_api_RspDuoModeCbEventIdT.sdrplay_api_SlaveDllDisappeared) ?
                        "sdrplay_api_SlaveDllDisappeared" : "unknown type");
*/                  
                    //if (Params.rspDuoModeParams.modeChangeType == sdrplay_api_MasterInitialised)
                    //	masterInitialised = 1;
                    //if (Params.rspDuoModeParams.modeChangeType == sdrplay_api_SlaveUninitialised)
                    //	slaveUninitialised = 1;
                    //break;
                case sdrplay_api_EventT.sdrplay_api_DeviceRemoved:
                    Debug.WriteLine("sdrplay_api_EventCb: {0}", "sdrplay_api_DeviceRemoved");
                    break;
                default:
                    Debug.WriteLine("sdrplay_api_EventCb: {0}, unknown event", eventId);
                    break;
            }
        }
        #endregion

    }
}
