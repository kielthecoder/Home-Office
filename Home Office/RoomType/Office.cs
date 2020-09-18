using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM;
using Crestron.SimplSharpPro.Gateways;
using Crestron.SimplSharpPro.UI;

namespace Home_Office.RoomType
{
    public class Office : Room
    {
        private bool _forceExternalGw = false;

        private RFExGateway _gw;
        private HdMd4x24kE _sw;
        private Tst902 _tp;
        private XpanelForSmartGraphics _xpanel;

        private UI _panels;

        public Office(ControlSystem cs, string cfgFileName)
            : base(cs, cfgFileName)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            InitializeGateways();
            InitializeUI();
            InitializeDM();
        }

        private void InitializeGateways()
        {
            if (_sys.SupportsInternalRFGateway && !_forceExternalGw)
            {
                Log("InitializeGateways", "Using internal RF gateway");

                _gw = _sys.ControllerRFGatewayDevice as RFExGateway;
            }
            else
            {
                Log("InitializeGateways", "Creating external RF gateway");

                _gw = new CenRfgwEx(0x0f, _sys);

                if (_gw.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    Log("InitializeGateways", "Failed to register {0}: {1}", _gw.Name, _gw.RegistrationFailureReason.ToString());
            }
        }

        private void InitializeUI()
        {
            _panels = new UI(this);

            if (_gw.Registered)
            {
                Log("InitializeUI", "Creating TST-902 on {0}", _gw.Name);
                _tp = new Tst902(0x03, _gw);
                _panels.Add_902(_tp);
            }

            Log("InitializeUI", "Creating XPanel");
            _xpanel = new XpanelForSmartGraphics(0x04, _sys);
            _panels.Add(_xpanel);

            Log("InitializeUI", "Registering devices");
            _panels.Register();

        }

        private void InitializeDM()
        {
            Log("InitializeDM", "Creating DM switch");

            _sw = new HdMd4x24kE(0x10, "192.168.1.122", _sys);
            _sw.DMInputChange += _sw_DMInputChange;

            if (_sw.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                Log("InitializeDM", "Failed to register {0}: {1}", _sw.Name, _sw.RegistrationFailureReason.ToString());
        }

        public override void Press(BasicTriList dev, uint join)
        {
        }

        public override void Release(BasicTriList dev, uint join)
        {
        }

        public override void SetSource(int src)
        {
            switch (src)
            {
                case 1:
                    _sw.HdmiOutputs[1].VideoOut = _sw.HdmiInputs[1];
                    _sw.HdmiOutputs[2].VideoOut = _sw.HdmiInputs[2];

                    _panels.Interlock(UI.SourceSelectJoins, UI.SourceSelectJoins[0]);

                    break;
                case 2:
                    _sw.HdmiOutputs[1].VideoOut = _sw.HdmiInputs[3];
                    _sw.HdmiOutputs[2].VideoOut = _sw.HdmiInputs[4];

                    _panels.Interlock(UI.SourceSelectJoins, UI.SourceSelectJoins[1]);

                    break;
                case 3:
                    _sw.HdmiOutputs[1].VideoOut = _sw.HdmiInputs[1];
                    _sw.HdmiOutputs[2].VideoOut = _sw.HdmiInputs[3];

                    _panels.Interlock(UI.SourceSelectJoins, UI.SourceSelectJoins[2]);

                    break;
            }
        }

        void _sw_DMInputChange(Switch dev, DMInputEventArgs args)
        {
            switch (args.EventId)
            {
                case DMInputEventIds.InputNameEventId:
                    Log("_sw_DMInputChange", "Input {0} name: {1}", args.Number.ToString(), _sw.HdmiInputs[args.Number].NameFeedback.StringValue);
                    break;
                case DMInputEventIds.SourceSyncEventId:
                    if (_sw.HdmiInputs[args.Number].VideoDetectedFeedback.BoolValue)
                        _sw_ReportVideoAttributes(args.Number);
                    else
                        Log("_sw_DMInputChange", "Input {0} sync: None", args.Number.ToString());
                    break;
                default:
                    Log("_sw_DMInputChange", "EventId={0}, Number={1}, Stream={2}", args.EventId.ToString(), args.Number.ToString(), args.Stream.ToString());
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
    }
}