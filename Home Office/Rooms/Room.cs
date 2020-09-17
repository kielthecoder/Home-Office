using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using C1.Utility;

namespace Home_Office.Rooms
{
    public class Room
    {
        protected ControlSystem _cs;
        protected ConfigFile _cfg;

        public string Name { get; set; }

        public Room(ControlSystem cs, ConfigFile cfg)
        {
            this._cs = cs;

            Name = "Room";
        }

        public void Initialize()
        {
        }

        public void Log(string msg)
        {
            this._cs.Log(Name, msg);
        }
    }
}