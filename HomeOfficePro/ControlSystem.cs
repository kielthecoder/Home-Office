using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.UI;
using Crestron.SimplSharpPro.DM;

namespace HomeOfficePro
{
    public enum SourceIds
    {
        None,
        PC,
        Laptop
    }

    public class ControlSystem : CrestronControlSystem
    {
        private Tsw552 _tsw;
        private HdMd4x24kE _sw;

        private UserInterface _ui;
        
        public const ushort MAX_BRIGHTNESS = 60000;
        public const ushort MIN_BRIGHTNESS = 20000;

        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in ControlSystem: {0}", e.Message);
            }
        }

        public override void InitializeSystem()
        {
            try
            {
                _tsw = new Tsw552(0x03, this);
                _tsw.ExtenderScreenSaverReservedSigs.Use();
                _tsw.ExtenderSystemReservedSigs.Use();

                _sw = new HdMd4x24kE(0x10, "192.168.1.40", this);
                _sw.DMInputChange += sw_DMInputChange;
                _sw.Register();

                _ui = new UserInterface(_tsw, this);
                _ui.Initialize = InitTsw;
                _ui.SetAwake = SetMaxBrightness;
                _ui.SetAsleep = SetMinBrightness;
                _ui.SelectSource = SelectSource;
                _ui.SetBacklight = SetBacklight;
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }

        void sw_DMInputChange(Switch dev, DMInputEventArgs args)
        {
            if (args.EventId == DMInputEventIds.SourceSyncEventId)
            {
                // Route Laptop 1 if video detected
                if (args.Number == 3)
                {
                    dev.Outputs[1].VideoOut = dev.Inputs[3].VideoDetectedFeedback.BoolValue ?
                        dev.Inputs[3] : dev.Inputs[1];
                }

                // Route Laptop 2 if video detected
                if (args.Number == 4)
                {
                    dev.Outputs[2].VideoOut = dev.Inputs[4].VideoDetectedFeedback.BoolValue ?
                        dev.Inputs[4] : dev.Inputs[2];
                }

                // Either Laptop detected
                _ui.LaptopDetected = dev.Inputs[3].VideoDetectedFeedback.BoolValue |
                    dev.Inputs[4].VideoDetectedFeedback.BoolValue;

                // Which source is active now?
                if (dev.Outputs[1].VideoOut.Number == 1 &&
                    dev.Outputs[2].VideoOut.Number == 2)
                {
                    _ui.ActiveSource = SourceIds.PC;
                }
                
                if (dev.Outputs[1].VideoOut.Number == 3 &&
                    dev.Outputs[2].VideoOut.Number == 4)
                {
                    _ui.ActiveSource = SourceIds.Laptop;
                }
            }
        }

        private void InitTsw()
        {
            if (_tsw != null)
            {
                _tsw.ExtenderScreenSaverReservedSigs.ScreensaverOff.BoolValue = true;
            }
        }

        private void SetMaxBrightness()
        {
            if (_tsw != null)
                _tsw.ExtenderSystemReservedSigs.LcdBrightness.UShortValue = MAX_BRIGHTNESS;
        }

        private void SetMinBrightness()
        {
            if (_tsw != null)
                _tsw.ExtenderSystemReservedSigs.LcdBrightness.UShortValue = MIN_BRIGHTNESS;
        }

        private void SetBacklight(bool enable)
        {
            if (_tsw != null)
            {
                if (enable)
                {
                    _tsw.ExtenderSystemReservedSigs.BacklightOn();
                }
                else
                {
                    _tsw.ExtenderSystemReservedSigs.BacklightOff();
                }
            }
        }

        private void SelectSource(SourceIds source)
        {
            switch (source)
            {
                case SourceIds.PC:
                    _sw.Outputs[1].VideoOut = _sw.Inputs[1];
                    _sw.Outputs[2].VideoOut = _sw.Inputs[2];
                    break;
                case SourceIds.Laptop:
                    _sw.Outputs[1].VideoOut = _sw.Inputs[3];
                    _sw.Outputs[2].VideoOut = _sw.Inputs[4];
                    break;
            }

            _ui.ActiveSource = source;
        }
    }
}