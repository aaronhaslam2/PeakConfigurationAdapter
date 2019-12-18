using System;
using System.ComponentModel;
using System.Net;
using RestSharp;

namespace PeakConfigurationAdapter
{
    public class PeakConfigurationAdapter : IPeakConfigurationAdapter
    {
        private string _usernamePeakSystem = "admin";
        private string _passwordPeakSystem = "admin";
        
        // private enum CanBaudRate
        // {
        //     [Description("10KBaud")] CanBaud10K = 10000,
        //     [Description("20KBaud")] CanBaud20K = 20000,
        //     [Description("50KBaud")] CanBaud50K = 50000,
        //     [Description("100KBaud")] CanBaud100K = 100000,
        //     [Description("125KBaud")] CanBaud125K = 125000,
        //     [Description("250KBaud")] CanBaud250K = 250000,
        //     [Description("500KBaud")] CanBaud500K = 500000,
        //     [Description("800KBaud")] CanBaud800K = 800000,
        //     [Description("1MBaud")] CanBaud_1M = 1000000
        // };
        
        private (RestClient, RestRequest, IRestResponse) loginToPeak(string peakAddress, string username, string password)
        {
            var restClient = new RestClient("http://" + peakAddress) {CookieContainer = new CookieContainer()};

            var restRequest = new RestRequest("bouncer.php?PW=" + username + "&UN=" + password, Method.POST);

            var restResponse = restClient.Execute(restRequest);

            restRequest = new RestRequest("/processing/device_user.php?request=mode&mode=expert");

            restResponse = restClient.Execute(restRequest);

            return (restClient, restRequest, restResponse);
        }
        
        
        /// <summary>
        /// Change the login username and password for the peak 
        /// </summary>
        /// <param name="newUsername"></param>
        /// <param name="newPassword"></param>
        public void ChangeLoginCredentials(string newUsername, string newPassword)
        {
            _usernamePeakSystem = newUsername;
            _passwordPeakSystem = newPassword;
        }
        
        /// <summary>
        /// Returns the current username and password for the peak
        /// </summary>
        /// <returns></returns>
        public (string username, string password) GetLoginCredentials()
        {
            return (_usernamePeakSystem, _passwordPeakSystem);
        }

        /// <summary>
        /// Creates a route to send data. (CAN > IP)
        /// </summary>
        /// <param name="peakAddress"></param>
        /// <param name="destinationAddress"></param>
        /// <param name="routeNumber"></param>
        /// <param name="channel"></param>
        /// <param name="port"></param>
        public void CreateSendRoute_CanToIp(string peakAddress, string destinationAddress, int routeNumber, int channel, int port)
        {
            var ( client, request, response) =
                loginToPeak(peakAddress, _usernamePeakSystem, _passwordPeakSystem);

            var ipAddressParts = destinationAddress.Split('.');

            // change can channel to 0 based array
            var adjustedCanChannel = channel - 1;

            request =
                new RestRequest(
                    "/processing/route_add.php?route_index=" + routeNumber +
                    "&route_direction=0&return_page=%2Frouting_add_route.php&route_state=on&handshake_state=on&can_chan=can" +
                    adjustedCanChannel + "&ip1="
                    + ipAddressParts[0] + "&ip2=" + ipAddressParts[1] + "&ip3=" + ipAddressParts[2] + "&ip4=" +
                    ipAddressParts[3] + "&port=" + port + "&proto=17&FPP=1", Method.POST);

            response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Error: Status code: " + response.StatusCode);
            }

            Console.WriteLine("Route Created:\t" + 
                              "Route - " + routeNumber + "\t" +
                              "Source - CAN Channel " + channel + "\t" + 
                              "Destination - " + destinationAddress + ":" + port
            );
        }
        
        /// <summary>
        /// Creates a route to receive data. (IP > CAN)
        /// </summary>
        /// <param name="peakAddress"></param>
        /// <param name="routeNumber"></param>
        /// <param name="channel"></param>
        /// <param name="port"></param>
        public void CreateReceiveRoute_IpToCan(string peakAddress, int routeNumber, int channel, int port)
        {
            var ( client, request, response) =
                loginToPeak(peakAddress, _usernamePeakSystem, _passwordPeakSystem);
            
            var adjustedCanChannel = channel - 1;

            request =
                new RestRequest(
                    "/processing/route_add.php?route_index=" + routeNumber +
                    "&route_direction=1&return_page=%2Frouting_add_route.php&route_state=on&handshake_state=on&can_chan=can" +
                    adjustedCanChannel +
                    "&ip1=0&ip2=0&ip3=0&ip4=0&port=" + port + "&proto=17&FPP=1", Method.POST); //&join_filters=0

            response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Error: Status code: " + response.StatusCode);
            }

            Console.WriteLine("Route Created:\t" + 
                              "Route - " + routeNumber + "\t" +
                              "Source - Local IP - Port:" + port + "\t" +
                              "Destination - CAN Channel " + channel
            );
        }
        
        /// <summary>
        /// Removes all current routes configured on the peak.
        /// </summary>
        /// <param name="peakAddress"></param>
        public void RemoveAllRoutes(string peakAddress)
        {
            var ( client, request, response) =
                loginToPeak(peakAddress, _usernamePeakSystem, _passwordPeakSystem);

            for (var count = 8; count >= 0; count--)
            {
                request = new RestRequest("/processing/route_delete.php?delete=" + count, Method.GET);

                // execute the request
                var result = client.Execute(request);

                if (result.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine("Error: Unable to delete route " + count);
                }
            }

            Console.WriteLine("Successfully removed all routes from PCAN at IP address: " + peakAddress);
        }
        
        /// <summary>
        /// Sets the baud rate for a channel.
        /// </summary>
        /// <param name="peakAddress"></param>
        /// <param name="channel"></param>
        /// <param name="baudRateKbits"></param>
        public void SetChannelBaudRate(string peakAddress, int channel, double baudRateKbits)
        {
            var ( client, request, response) =
                loginToPeak(peakAddress, _usernamePeakSystem, _passwordPeakSystem);
            
            var baudRate = (int) baudRateKbits * 1000;

            string infoString = channel == 1 ? "Actuation+Bus" : "OEM+Bus";

            // change the can rate
            request =
                new RestRequest(
                    "processing/can_edit.php?index=" + (int) channel + "&set_can_status=2&bitrate=" + baudRate +
                    "&info=" + infoString, Method.POST);
            
            // execute the request
            response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Error: Baud rate for Channel " + channel + " not changed to " + baudRateKbits + " kbit/s");
            }
            
            Console.WriteLine("Baud rate changed for Channel " + channel + " to " + baudRateKbits + " kbit/s");
        }
        
        /// <summary>
        /// Creates a send and receive route and sets the baud rate for a channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="peakAddress"></param>
        /// <param name="destinationAddress"></param>
        /// <param name="port"></param>
        /// <param name="baudRateKbits"></param>
        public void CreateCanChannel(int channel, string peakAddress, string destinationAddress, int port, double baudRateKbits)
        {
            // Get route numbers
            int routeNumber = channel * 2;

            // Connect to Peak
            var ( client, request, response) =
                loginToPeak(peakAddress, _usernamePeakSystem, _passwordPeakSystem);
            
            ///////////////////////////////////////
            // Create IP to CAN connection
            ///////////////////////////////////////
            var adjustedCanChannel = channel - 1;

            request =
                new RestRequest(
                    "/processing/route_add.php?route_index=" + routeNumber +
                    "&route_direction=1&return_page=%2Frouting_add_route.php&route_state=on&handshake_state=on&can_chan=can" +
                    adjustedCanChannel +
                    "&ip1=0&ip2=0&ip3=0&ip4=0&port=" + port + "&proto=17&FPP=1", Method.POST); //&join_filters=0

            response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Error: Status code: " + response.StatusCode);
            }

            Console.WriteLine("Route Created:\t" + 
                              "Route - " + routeNumber + "\t" +
                              "Source - Local IP - Port:" + port + "\t" +
                              "Destination - CAN Channel " + channel
            );
            
            
            ///////////////////////////////////////
            // Create CAN to IP connection
            ///////////////////////////////////////
            routeNumber--;
            
            var ipAddressParts = destinationAddress.Split('.');
            
            request =
                new RestRequest(
                    "/processing/route_add.php?route_index=" + routeNumber +
                    "&route_direction=0&return_page=%2Frouting_add_route.php&route_state=on&handshake_state=on&can_chan=can" +
                    adjustedCanChannel + "&ip1="
                    + ipAddressParts[0] + "&ip2=" + ipAddressParts[1] + "&ip3=" + ipAddressParts[2] + "&ip4=" +
                    ipAddressParts[3] + "&port=" + port + "&proto=17&FPP=1", Method.POST);

            response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Error: Status code: " + response.StatusCode);
            }

            Console.WriteLine("Route Created:\t" + 
                              "Route - " + routeNumber + "\t" +
                              "Source - CAN Channel " + channel + "\t" + 
                              "Destination - " + destinationAddress + ":" + port
            );
            
            ///////////////////////////////////////
            // Adjust BaudRate
            ///////////////////////////////////////
            var baudRate = (int) baudRateKbits * 1000;

            string infoString = channel == 1 ? "Actuation+Bus" : "OEM+Bus";

            // change the can rate
            request =
                new RestRequest(
                    "processing/can_edit.php?index=" + (int) channel + "&set_can_status=2&bitrate=" + baudRate +
                    "&info=" + infoString, Method.POST);
            
            // execute the request
            response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Error: Baud rate for Channel " + channel + " not changed to " + baudRateKbits + " kbit/s");
            }
            
            Console.WriteLine("Baud rate changed for Channel " + channel + " to " + baudRateKbits + " kbit/s");
            
           
            
            
        }
    }
}