namespace PeakConfigurationAdapter
{
    public interface IPeakConfigurationAdapter
    {

        void ChangeLoginCredentials(string newUsername, string newPassword);

        (string username, string password) GetLoginCredentials();
        
        void CreateSendRoute_CanToIp(string peakIpAddress, string targetIpAddress, int routeNumber, int channel, int port);

        void CreateReceiveRoute_IpToCan(string peakIpAddress, int routeNumber, int channel, int port);

        void RemoveAllRoutes(string peakIpAddress);

        void SetChannelBaudRate(string peakIpAddress, int channel, double baudRateKbits);

        void CreateCanChannel(int channel, string peakIpAddress, string targetIpAddress, int port, double baudRateKbits);
        
    }
}