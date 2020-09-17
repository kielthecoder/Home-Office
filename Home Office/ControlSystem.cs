using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM;
using Crestron.SimplSharpPro.Gateways;
using Crestron.SimplSharpPro.UI;
using C1.Utility;

namespace Home_Office
{
    public class ControlSystem : CrestronControlSystem
    {
        private ConfigFile _cfg;
        private UI _panels;

        private RFExGateway _gw;
        private HdMd4x24kE _sw;
        private Tst902 _tp;
        private XpanelForSmartGraphics _xpanel;

        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 40;
            }
            catch (Exception e)
            {
                ErrorLog.Error("WHOA! Error in ControlSystem constructor: {0}", e.Message);
            }
        }

        public override void InitializeSystem()
        {
            try
            {
                var configFileName = "\\USER\\RoomConfig.ini";

                Log("InitializeSystem", String.Format("Loading configuration from {0}", configFileName));

                _cfg = new ConfigFile(configFileName);
                _cfg.Load();

                InitializeGateways();
                InitializeUI();
                InitializeDM();
            }
            catch (Exception e)
            {
                ErrorLog.Error("WHOA! Error in InitializeSystem: {0}", e.Message);
            }
        }

        public void Log(string method, string msg)
        {
            CrestronConsole.PrintLine("{0}: {1}", method, msg);
        }

        private void InitializeGateways()
        {
            if (this.SupportsInternalRFGateway && !_cfg.GetBool("panel", "forceExternalGateway"))
            {
                Log("InitializeGateways", "Using internal RF gateway");

                _gw = this.ControllerRFGatewayDevice as RFExGateway;
            }
            else
            {
                Log("InitializeGateways", "Creating external RF gateway");

                _gw = new CenRfgwEx(_cfg.GetInteger("gateway", "ip_id", 0x0f), this);
                
                if (_gw.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    Log("InitializeGateways", String.Format("Failed to register {0}: {1}", _gw.Name, _gw.RegistrationFailureReason));
            }
        }

        private void InitializeUI()
        {
            _panels = new UI(this);

            if (_gw.Registered)
            {
                Log("InitializeUI", String.Format("Creating TST-902 on {0}", _gw.Name));
                _tp = new Tst902(_cfg.GetInteger("panel", "rf_id", 0x03), _gw);
                _panels.Add_902(_tp);
            }

            Log("InitializeUI", "Creating XPanel");
            _xpanel = new XpanelForSmartGraphics(_cfg.GetInteger("xpanel", "ip_id", 0x04), this);
            _panels.Add(_xpanel);

            Log("InitializeUI", "Registering devices");
            _panels.Register();

        }

        private void InitializeDM()
        {
            Log("InitializeDM", "Creating DM switch");

            _sw = new HdMd4x24kE(_cfg.GetInteger("switcher", "ip_id", 0x10), _cfg.GetString("switcher", "ip", "192.168.1.10"), this);
            _sw.DMInputChange += _sw_DMInputChange;

            if (_sw.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                Log("InitializeDM", String.Format("Failed to register {0}: {1}", _sw.Name, _sw.RegistrationFailureReason));
        }

        public void SetSource(int src)
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
    }
}