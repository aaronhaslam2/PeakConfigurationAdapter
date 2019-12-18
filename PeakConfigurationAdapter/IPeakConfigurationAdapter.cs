namespace PeakConfigurationAdapter
{
    public interface IPeakConfigurationAdapter
    {

        void ChangeLoginCredentials(string newUsername, string newPassword);

        (string username, string password) GetLoginCredentials();
        
        void CreateSendRoute_CanToIp(string peakAddress, string destinationAddress, int routeNumber, int channel, int port);

        void CreateReceiveRoute_IpToCan(string peakAddress, int routeNumber, int channel, int port);

        void RemoveAllRoutes(string peakAddress);

        void SetChannelBaudRate(string peakAddress, int channel, double baudRateKbits);

        void CreateCanChannel(int channel, string peakAddress, string destinationAddress, int port, double baudRateKbits);
        
    }
}