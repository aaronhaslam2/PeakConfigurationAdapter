using System;

namespace Test
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var peak = new PeakConfigurationAdapter.PeakConfigurationAdapter();
            
            // peak.RemoveAllRoutes("10.10.253.70");
            
            // peak.CreateSendRoute_CanToIp("10.10.253.70", "10.10.100.164", 3, 1, 5010);
            // peak.CreateSendRoute_CanToIp("10.10.253.70", "10.10.100.164", 1, 2, 6010);
            // peak.CreateReceiveRoute_IpToCan("10.10.253.70", 2, 2, 6010);
            // peak.CreateReceiveRoute_IpToCan("10.10.253.70", 4, 1, 5010);
            //
            // peak.SetChannelBaudRate("10.10.253.70", 1, 1);
            // peak.SetChannelBaudRate("10.10.253.70", 2, 250);
            //
            // peak.CreateCanChannel(1, "10.10.253.70", "10.10.100.164", 5010, 250);
            // peak.CreateCanChannel(2, "10.10.253.70", "10.10.100.164", 6010, 250);
        }
    }
}