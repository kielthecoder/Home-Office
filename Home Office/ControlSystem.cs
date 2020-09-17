using System;
using System.Collections.Generic;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;
using C1.Utility;

using Home_Office.Rooms;

namespace Home_Office
{
    public class ControlSystem : CrestronControlSystem
    {
        private ConfigFile _cfg;
        private Room _room;

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
                var fileName = "\\USER\\RoomConfig.ini";

                Log("InitializeSystem", "Loading configuration from {0}", fileName);

                _cfg = new ConfigFile(fileName);
                _cfg.Load();

                _room = new Office(this, _cfg);
                _room.Initialize();
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

        public void Log(string method, string msg, params string[] args)
        {
            CrestronConsole.PrintLine("{0}: {1}", method, String.Format(msg, args));
        }
    }
}