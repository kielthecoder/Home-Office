using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using C1.Utility;

namespace Home_Office
{
    public class Room
    {
        protected ControlSystem _sys;
        protected ConfigFile _cfg;

        public Room(ControlSystem cs, string cfgFileName)
        {
            _sys = cs;
            _cfg = new ConfigFile(cfgFileName);
        }

        public virtual void Initialize()
        {
            Log("Room::Initialize", "Loading room configuration from {0}", _cfg.FileName);

            _cfg.Load();
        }

        public virtual void Press(BasicTriList dev, uint join)
        {
        }

        public virtual void Release(BasicTriList dev, uint join)
        {
        }

        public virtual void SetSource(int src)
        {
        }

        public void Log(string method, string msg)
        {
            CrestronConsole.PrintLine("{0}: {1}", method, msg);
        }

        public void Log(string method, string fmt, params string[] args)
        {
            CrestronConsole.PrintLine("{0}: {1}", method, String.Format(fmt, args));
        }
    }
}