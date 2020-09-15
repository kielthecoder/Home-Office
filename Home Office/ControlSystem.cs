using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM;
using Crestron.SimplSharpPro.Gateways;
using Crestron.SimplSharpPro.UI;

namespace HomeOffice
{
    public class ControlSystem : CrestronControlSystem
    {
        private bool _forceExternalGw = false;

        private RFExGateway _gw;
        private HdMd4x24kE _sw;
        private Tst902 _tp;
        private XpanelForSmartGraphics _xpanel;

        private List<BasicTriListWithSmartObject> _panels;

        public static readonly uint[] SourceSelectJoins = { 31, 32 };

        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 40;
            }
            catch (Exception e)
            {
                ErrorLog.Error("Whoa!! Error in ControlSystem constructor: {0}", e.Message);
            }
        }

        public override void InitializeSystem()
        {
            try
            {
                InitializeGateways();
                InitializeUI();
                InitializeDM();
            }
            catch (Exception e)
            {
                ErrorLog.Error("Whoa!! Error in InitializeSystem: {0}", e.Message);
            }
        }

        private void Log(string method, string msg)
        {
            CrestronConsole.PrintLine("{0}: {1}", method, msg);
        }

        private void InitializeGateways()
        {
            if (this.SupportsInternalRFGateway && !_forceExternalGw)
            {
                Log("InitializeGateways", "Using the internal RF gateway...");

                _gw = this.ControllerRFGatewayDevice as RFExGateway;
            }
            else
            {
                Log("InitializeGateways", "Creating external RF gateway...");

                _gw = new CenRfgwEx(0x0f, this);
                
                if (_gw.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    Log("InitializeGateways", String.Format("Failed to register {0}: {1}", _gw.Name, _gw.RegistrationFailureReason));
            }
        }

        private void InitializeUI()
        {
            if (_gw.Registered)
            {
                Log("InitializeUI", String.Format("Creating TST-902 on {0}...", _gw.Name));

                _tp = new Tst902(0x03, _gw);
                _tp.SigChange += _tp_SigChange;
                
                _tp.ExtenderRfWiFiReservedSigs.Use();
                _tp.ExtenderRfWiFiReservedSigs.DeviceExtenderSigChange += _tp_RfWifi_SigChange;
            }

            Log("InitializeUI", "Creating XPanel...");

            _xpanel = new XpanelForSmartGraphics(0x04, this);
            _xpanel.SigChange += _tp_SigChange;

            Log("InitializeUI", "Registering devices...");

            _panels = new List<BasicTriListWithSmartObject>();
            _panels.Add(_tp);
            _panels.Add(_xpanel);

            foreach (var tp in _panels)
            {
                if (tp.Register() == eDeviceRegistrationUnRegistrationResponse.Success)
                {
                    Log("InitializeUI", String.Format("Registered {0}: ID {1}", tp.Name, tp.ID));
                }
                else
                {
                    Log("InitializeUI", String.Format("Failed to register {0}: {1}", tp.Name, tp.RegistrationFailureReason));
                }
            }
        }

        private void InitializeDM()
        {
            Log("InitializeDM", "Creating DM switch...");

            _sw = new HdMd4x24kE(0x10, "192.168.1.122", this);
            _sw.DMInputChange += _sw_DMInputChange;

            if (_sw.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                Log("InitializeDM", String.Format("Failed to register {0}: {1}", _sw.Name, _sw.RegistrationFailureReason));
        }

        private int GetLast(uint[] joins, uint match)
        {
            for (int i = 0; i < joins.Length; i++)
            {
                if (joins[i] == match)
                    return i + 1;
            }

            return 0;
        }

        public void SetSource(int src)
        {
            switch (src)
            {
                case 1:
                    _sw.HdmiOutputs[1].VideoOut = _sw.HdmiInputs[1];
                    _sw.HdmiOutputs[2].VideoOut = _sw.HdmiInputs[2];

                    foreach (var tp in _panels)
                    {
                        tp.BooleanInput[32].BoolValue = false;
                        tp.BooleanInput[31].BoolValue = true;
                    }
                    break;
                case 2:
                    _sw.HdmiOutputs[1].VideoOut = _sw.HdmiInputs[3];
                    _sw.HdmiOutputs[2].VideoOut = _sw.HdmiInputs[4];

                    foreach (var tp in _panels)
                    {
                        tp.BooleanInput[31].BoolValue = false;
                        tp.BooleanInput[32].BoolValue = true;
                    }
                    break;
            }
        }

        void _sw_DMInputChange(Switch dev, DMInputEventArgs args)
        {
            switch (args.EventId)
            {
                case DMInputEventIds.InputNameEventId:
                    Log("_sw_DMInputChange", String.Format("Input {0} name: {1}", args.Number, _sw.HdmiInputs[args.Number].NameFeedback.StringValue));
                    break;
                case DMInputEventIds.SourceSyncEventId:
                    if (_sw.HdmiInputs[args.Number].VideoDetectedFeedback.BoolValue)
                        _sw_ReportVideoAttributes(args.Number);
                    else
                        Log("_sw_DMInputChange", String.Format("Input {0} sync: None", args.Number));
                    break;
                default:
                    Log("_sw_DMInputChange", String.Format("EventId={0}, Number={1}, Stream={2}", args.EventId, args.Number, args.Stream));
                    break;
            }
        }

        void _sw_ReportVideoAttributes(uint number)
        {
            Log("_sw_ReportVideoAttributes", String.Format("Input {0} sync: {1}x{2}@{3}", number,
                _sw.HdmiInputs[number].HdmiInputPort.VideoAttributes.HorizontalResolutionFeedback.UShortValue,
                _sw.HdmiInputs[number].HdmiInputPort.VideoAttributes.VerticalResolutionFeedback.UShortValue,
                _sw.HdmiInputs[number].HdmiInputPort.VideoAttributes.FramesPerSecondFeedback.UShortValue));
        }

        void _tp_SigChange(BasicTriList dev, SigEventArgs args)
        {
            if (args.Sig.Type == eSigType.Bool)
            {
                if (args.Sig.BoolValue)
                    _tp_Press(dev, args.Sig.Number);
                else
                    _tp_Release(dev, args.Sig.Number);
            }
        }

        void _tp_Press(BasicTriList dev, uint join)
        {
            if ((join >= SourceSelectJoins[0]) && (join <= SourceSelectJoins[SourceSelectJoins.Length - 1]))
            {
                SetSource(GetLast(SourceSelectJoins, join));
            }
        }

        void _tp_Release(BasicTriList dev, uint join)
        {
        }

        void _tp_RfWifi_SigChange(DeviceExtender extender, SigEventArgs args)
        {
            switch (args.Sig.Type)
            {
                case eSigType.Bool:
                    switch (args.Sig.Number)
                    {
                        case 17547:
                            if (_tp.ExtenderRfWiFiReservedSigs.AcLineStatusFeedback.BoolValue)
                            {
                                _tp.UShortInput[2].UShortValue = 2;
                            }
                            else
                            {
                                if (_tp.ExtenderRfWiFiReservedSigs.BatteryLevelFeedback.UShortValue < 50)
                                    _tp.UShortInput[2].UShortValue = 0;
                                else
                                    _tp.UShortInput[2].UShortValue = 1;
                            }
                            break;
                    }
                    break;
                case eSigType.UShort:
                    switch (args.Sig.Number)
                    {
                        case 17532:
                            var battery = args.Sig.UShortValue;

                            if (_tp.ExtenderRfWiFiReservedSigs.AcLineStatusFeedback.BoolValue)
                            {
                                _tp.UShortInput[2].UShortValue = 2;
                            }
                            else
                            {
                                if (battery < 50)
                                    _tp.UShortInput[2].UShortValue = 0;
                                else
                                    _tp.UShortInput[2].UShortValue = 1;
                            }

                            break;
                        case 17533:
                            var rf_sig = args.Sig.UShortValue;

                            if (rf_sig < 50)
                                _tp.UShortInput[1].UShortValue = 1;
                            else
                                _tp.UShortInput[1].UShortValue = 0;

                            break;
                    }
                    break;
            }
        }
    }
}