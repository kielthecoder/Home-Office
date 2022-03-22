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
            if (_sys.SupportsInternalRFGateway && !_cfg.GetBool("panel", "external_gw", false))
            {
                Log("Office::InitializeGateways", "Using internal RF gateway");

                _gw = _sys.ControllerRFGatewayDevice as RFExGateway;
            }
            else
            {
                Log("Office::InitializeGateways", "Creating external RF gateway");

                _gw = new CenRfgwEx(_cfg.GetInteger("gateway", "ip_id", 0x0f), _sys);

                if (_gw.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    Log("Office::InitializeGateways", "Failed to register {0}: {1}", _gw.Name, _gw.RegistrationFailureReason.ToString());
            }
        }

        private void InitializeUI()
        {
            _panels = new UI(this);

            if (_gw.Registered)
            {
                Log("Office::InitializeUI", "Creating TST-902 on {0}", _gw.Name);
                _tp = new Tst902(_cfg.GetInteger("panel", "rf_id", 0x03), _gw);
                _panels.Add_902(_tp);
            }

            Log("Office::InitializeUI", "Creating XPanel");
            _xpanel = new XpanelForSmartGraphics(_cfg.GetInteger("xpanel", "ip_id", 0x04), _sys);
            _panels.Add(_xpanel);

            Log("Office::InitializeUI", "Registering devices");
            _panels.Register();

        }

        private void InitializeDM()
        {
            Log("Office::InitializeDM", "Creating DM switch");

            _sw = new HdMd4x24kE(_cfg.GetInteger("switcher", "ip_id", 0x10),
                                 _cfg.GetString("switcher", "ip", "192.168.1.10"),
                                 _sys);

            if (_sw.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                Log("Office::InitializeDM", "Failed to register {0}: {1}", _sw.Name, _sw.RegistrationFailureReason.ToString());
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
    }
}