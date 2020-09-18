using System;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.CrestronThread;

namespace Home_Office
{
    public class ControlSystem : CrestronControlSystem
    {
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
                ErrorLog.Error("*** Error in ControlSystem constructor: {0} ***", e.Message);
            }
        }

        public override void InitializeSystem()
        {
            try
            {
                _room = new RoomType.Office(this, "\\USER\\RoomConfig.ini");
                _room.Initialize();
            }
            catch (Exception e)
            {
                ErrorLog.Error("*** Error in InitializeSystem: {0} ***", e.Message);
                ErrorLog.Error("StackTrace:\n{0}", e.StackTrace);
            }
        }
    }
}