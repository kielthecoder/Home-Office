using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using Crestron.SimplSharpPro.DM;

namespace HomeOfficePro
{
    public class UserInterface
    {
        public delegate void DeviceControl();
        public delegate void DeviceControlBool(bool enable);
        public delegate void SwitcherControl(SourceIds source);

        private ControlSystem _cs;
        private BasicTriList _tp;
        private bool _awake;
        private int _sleepMS;

        private Thread _time;
        private Thread _standby;
        private Thread _deepSleep;

        private SourceIds _activeSource;
        public SourceIds ActiveSource
        {
            get
            {
                return _activeSource;
            }
            set
            {
                if (value != _activeSource)
                {
                    _activeSource = value;
                    UpdateFeedback();
                }
            }
        }

        private bool _laptopDetected;
        public bool LaptopDetected
        {
            get
            {
                return _laptopDetected;
            }
            set
            {
                if (value != _laptopDetected)
                {
                    _laptopDetected = value;
                    UpdateFeedback();
                }
            }
        }

        public DeviceControl Initialize;
        public DeviceControl SetAwake;
        public DeviceControl SetAsleep;
        public DeviceControlBool SetBacklight;
        public SwitcherControl SelectSource;
        
        public UserInterface(BasicTriList tp, ControlSystem cs)
        {
            _tp = tp;
            _cs = cs;
            _awake = true;
            _sleepMS = 60000;

            _tp.OnlineStatusChange += OnlineStatusChange;
            _tp.SigChange += SigChange;

            _tp.BooleanOutput[10].UserObject = new Action<bool>(press =>
                {
                    if (press)
                    {
                        ToggleAwake();
                        UpdateFeedback();
                    }
                });

            _tp.BooleanOutput[11].UserObject = new Action<bool>(press =>
                {
                    if (press)
                    {
                        if (SelectSource != null)
                            SelectSource(SourceIds.PC);

                        UpdateFeedback();
                    }
                });

            _tp.BooleanOutput[12].UserObject = new Action<bool>(press =>
                {
                    if (press)
                    {
                        if (SelectSource != null)
                            SelectSource(SourceIds.Laptop);

                        UpdateFeedback();
                    }
                });

            _tp.Register();
        }

        public void UpdateFeedback()
        {
            _tp.BooleanInput[50].BoolValue = !_awake;
            _tp.BooleanInput[51].BoolValue = _awake;

            _tp.BooleanInput[11].BoolValue = _activeSource == SourceIds.PC;
            _tp.BooleanInput[12].BoolValue = _activeSource == SourceIds.Laptop;

            _tp.BooleanInput[22].BoolValue = _laptopDetected;
        }

        public void ToggleAwake()
        {
            _awake = !_awake;

            if (_awake)
            {
                if (SetAwake != null)
                    SetAwake();

                if (SetBacklight != null)
                    SetBacklight(true);
            }
            else
            {
                if (SetAsleep != null)
                    SetAsleep();
            }
        }

        private void OnlineStatusChange(GenericBase dev, OnlineOfflineEventArgs args)
        {
            if (args.DeviceOnLine)
            {
                try
                {
                    _awake = true;

                    if (Initialize != null)
                        Initialize();

                    if (SetAwake != null)
                        SetAwake();

                    _time = new Thread(UpdateTime, _tp);
                    _standby = new Thread(EnterStandby, _tp);
                }
                catch (Exception e)
                {
                    ErrorLog.Error("Exception in DeviceOnLine: {0}", e.Message);
                }
            }
            else
            {
                try
                {
                    if (_time != null)
                        _time.Join();

                    _time = null;

                    if (_standby != null)
                        _standby.Abort();

                    _standby = null;

                    if (_deepSleep != null)
                        _deepSleep.Abort();

                    _deepSleep = null;
                }
                catch (Exception e)
                {
                    ErrorLog.Error("Exception in DeviceOffLine: {0}", e.Message);
                }
            }
        }

        private void SigChange(BasicTriList dev, SigEventArgs args)
        {
            if (args.Sig.UserObject != null)
            {
                var action = args.Sig.UserObject as Action<bool>;
                action(args.Sig.BoolValue);

                if (_standby != null)
                    _standby.Abort();

                _standby = new Thread(EnterStandby, _tp);
            }
        }

        private object UpdateTime(object obj)
        {
            string oldTime, newTime;
            var tp = obj as BasicTriList;

            oldTime = string.Empty;

            while (tp.IsOnline)
            {
                newTime = DateTime.Now.ToShortTimeString();

                if (!newTime.Equals(oldTime))
                {
                    tp.StringInput[1].StringValue = newTime;
                    tp.StringInput[2].StringValue = DateTime.Now.ToLongDateString();

                    oldTime = newTime;
                }

                Thread.Sleep(1000);
            }

            return null;
        }

        private object EnterStandby(object obj)
        {
            var tp = obj as BasicTriList;

            Thread.Sleep(_sleepMS);

            _awake = false;

            if (SetAsleep != null)
                SetAsleep();

            UpdateFeedback();

            _deepSleep = new Thread(EnterDeepSleep, _tp);

            return null;
        }

        private object EnterDeepSleep(object obj)
        {
            var tp = obj as BasicTriList;

            Thread.Sleep(15 * 60000); // 15 minutes

            if (SetBacklight != null)
                SetBacklight(false);

            return null;
        }
    }
}