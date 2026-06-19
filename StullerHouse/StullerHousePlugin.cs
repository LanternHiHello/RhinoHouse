using Rhino;
using Rhino.PlugIns;
using System;

namespace StullerHouse
{

    public class StullerHousePlugin : Rhino.PlugIns.PlugIn
    {
        public StullerHousePlugin()
        {
            Instance = this;
        }


        public static StullerHousePlugin Instance { get; private set; }
        public HouseDisplayConduit Conduit { get; private set; }


        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            Conduit = new HouseDisplayConduit();
            return base.OnLoad(ref errorMessage);

        }

        protected override void OnShutdown()
        {
            Conduit.Enabled = false;
            Conduit = null;
            base.OnShutdown();
        }

    }
}