using System;
using System.Collections.Generic;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;

namespace Home_Office
{
    public class UI
    {
        public static readonly uint[] SourceSelectJoins = { 31, 32, 33 };

        private ControlSystem _cs;
        private List<BasicTriListWithSmartObject> _panels;

        public UI(ControlSystem cs)
        {
            _cs = cs;
            _panels = new List<BasicTriListWithSmartObject>();
        }

        public void Add(BasicTriListWithSmartObject dev)
        {
            dev.SigChange += SigChange;

            _panels.Add(dev);
        }

        public void Add_902(Tst902 dev)
        {
            dev.SigChange += SigChange;
            dev.ExtenderRfWiFiReservedSigs.Use();
            dev.ExtenderRfWiFiReservedSigs.DeviceExtenderSigChange += RfWifi_SigChange;

            _panels.Add(dev);
        }

        public void Register()
        {
            foreach (var tp in _panels)
            {
                if (tp.Register() == eDeviceRegistrationUnRegistrationResponse.Success)
                {
                    _cs.Log("UI::Register", String.Format("Registered {0}: ID {1}", tp.Name, tp.ID));
                }
                else
                {
                    _cs.Log("UI::Register", String.Format("Failed to register {0}: {1}", tp.Name, tp.RegistrationFailureReason));
                } 
            }
        }

        public void SetBool(uint join, bool state)
        {
            foreach (var tp in _panels)
                tp.BooleanInput[join].BoolValue = state;
        }

        public void Interlock(uint[] group, uint join)
        {
            foreach (var j in group)
                SetBool(j, j == join);
        }

        public static int GetLast(uint[] joins, uint match)
        {
            for (int i = 0; i < joins.Length; i++)
            {
                if (joins[i] == match)
                    return i + 1;
            }

            return 0;
        }

        private void SigChange(BasicTriList dev, SigEventArgs args)
        {
            if (args.Sig.Type == eSigType.Bool)
            {
                if (args.Sig.BoolValue)
                    Press(dev, args.Sig.Number);
                else
                    Release(dev, args.Sig.Number);
            }
        }

        private void Press(BasicTriList dev, uint join)
        {
            if ((join >= SourceSelectJoins[0]) && (join <= SourceSelectJoins[SourceSelectJoins.Length - 1]))
            {
                _cs.SetSource(GetLast(SourceSelectJoins, join));
            }
        }

        private void Release(BasicTriList dev, uint join)
        {
        }

        void RfWifi_SigChange(DeviceExtender extender, SigEventArgs args)
        {
            var ext902 = extender as Tst902RfWiFiReservedSigs;
            var tst902 = args.Sig.Owner as BasicTriListWithSmartObject;

            switch (args.Sig.Type)
            {
                case eSigType.Bool:
                    switch (args.Sig.Number)
                    {
                        case 17547:
                            if (ext902.AcLineStatusFeedback.BoolValue)
                            {
                                tst902.UShortInput[2].UShortValue = 2;
                            }
                            else
                            {
                                if (ext902.BatteryLevelFeedback.UShortValue < 50)
                                    tst902.UShortInput[2].UShortValue = 0;
                                else
                                    tst902.UShortInput[2].UShortValue = 1;
                            }
                            break;
                    }
                    break;
                case eSigType.UShort:
                    switch (args.Sig.Number)
                    {
                        case 17532:
                            var battery = args.Sig.UShortValue;

                            if (ext902.AcLineStatusFeedback.BoolValue)
                            {
                                tst902.UShortInput[2].UShortValue = 2;
                            }
                            else
                            {
                                if (battery < 50)
                                    tst902.UShortInput[2].UShortValue = 0;
                                else
                                    tst902.UShortInput[2].UShortValue = 1;
                            }

                            break;
                        case 17533:
                            var rf_sig = args.Sig.UShortValue;

                            if (rf_sig < 50)
                                tst902.UShortInput[1].UShortValue = 1;
                            else
                                tst902.UShortInput[1].UShortValue = 0;

                            break;
                    }
                    break;
            }
        }
    }
}