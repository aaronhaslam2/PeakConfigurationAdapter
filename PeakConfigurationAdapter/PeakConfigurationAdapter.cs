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

        private (RestClient, RestRequest, IRestResponse) loginToPeak(string peakIpAddress, string username, string password)
        {
            var restClient = new RestClient("http://" + peakIpAddress) {CookieContainer = new CookieContainer()};

            var restRequest = new RestRequest("bouncer.php?PW=" + username + "&UN=" + password, Method.POST);

            var restResponse = restClient.Execute(restRequest);

            restRequest = new RestRequest("/processing/device_user.php?request=mode&mode=expert");

            restResponse = restClient.Execute(restRequest);

            return (restClient, restRequest, restResponse);
        }
        
        
        /// <summary>
        /// Change the username and password used to login to the peak.
        /// </summary>
        /// <param name="newUsername">The new username.</param>
        /// <param name="newPassword">The new password.</param>
        public void ChangeLoginCredentials(string newUsername, string newPassword)
        {
            _usernamePeakSystem = newUsername;
            _passwordPeakSystem = newPassword;
        }
        
        /// <summary>
        /// Returns the current username and password  used to login to the peak.
        /// </summary>
        /// <returns>(string username, string password)</returns>
        public (string username, string password) GetLoginCredentials()
        {
            return (_usernamePeakSystem, _passwordPeakSystem);
        }

        /// <summary>
        /// Creates a route to send data. <c>(CAN 🡪 IP)</c>
        /// </summary>
        /// <param name="peakIpAddress">IP address to the peak device.</param>
        /// <param name="targetIpAddress">IP address to the target device.</param>
        /// <param name="routeNumber">Route number to configure.</param>
        /// <param name="channel">Channel number to configure.</param>
        /// <param name="port">Port number.</param>
        public void CreateSendRoute_CanToIp(string peakIpAddress, string targetIpAddress, int routeNumber, int channel, int port)
        {
            var ( client, request, response) =
                loginToPeak(peakIpAddress, _usernamePeakSystem, _passwordPeakSystem);

            var ipAddressParts = targetIpAddress.Split('.');

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
                              "Destination - " + targetIpAddress + ":" + port
            );
        }
        
        /// <summary>
        /// Creates a route to receive data. <c>(IP 🡪 CAN)</c>
        /// </summary>
        /// <param name="peakIpAddress">IP address to the peak device.</param>
        /// <param name="routeNumber">Route number to configure.</param>
        /// <param name="channel">Channel number to configure.</param>
        /// <param name="port">Port number.</param>
        public void CreateReceiveRoute_IpToCan(string peakIpAddress, int routeNumber, int channel, int port)
        {
            var ( client, request, response) =
                loginToPeak(peakIpAddress, _usernamePeakSystem, _passwordPeakSystem);
            
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
        /// Removes all routes and channels configured on the peak.
        /// </summary>
        /// <param name="peakIpAddress">IP address to the peak device.</param>
        public void RemoveAllRoutes(string peakIpAddress)
        {
            var ( client, request, response) =
                loginToPeak(peakIpAddress, _usernamePeakSystem, _passwordPeakSystem);

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

            Console.WriteLine("Successfully removed all routes from PCAN at IP address: " + peakIpAddress);
        }
        
        /// <summary>
        /// Set the baud rate for a specific channel.
        /// </summary>
        /// <param name="peakIpAddress">IP address to the peak device.</param>
        /// <param name="channel">Channel number to configure.</param>
        /// <param name="baudRateKbits">Desired baud rate. (kbit/s)</param>
        public void SetChannelBaudRate(string peakIpAddress, int channel, double baudRateKbits)
        {
            var ( client, request, response) =
                loginToPeak(peakIpAddress, _usernamePeakSystem, _passwordPeakSystem);
            
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
        /// Creates:
        /// <list type="bullet">
        ///    <item><description>Send route on specified channel</description></item>
        ///    <item><description>Receive route on specified channel</description></item>
        /// </list>
        /// Configures:
        /// <list type="bullet">
        ///    <item><description>Baud rate for specified channel</description></item>
        /// </list>
        /// </summary>
        /// <param name="channel">Channel number to configure.</param>
        /// <param name="peakIpAddress">IP address to the peak device.</param>
        /// <param name="targetIpAddress">IP address to the target device.</param>
        /// <param name="port">Port number.</param>
        /// <param name="baudRateKbits">Desired baud rate. (kbit/s)</param>
        public void CreateCanChannel(int channel, string peakIpAddress, string targetIpAddress, int port, double baudRateKbits)
        {
            // Get route number
            int routeNumber = channel * 2;

            // Connect to Peak
            var ( client, request, response) =
                loginToPeak(peakIpAddress, _usernamePeakSystem, _passwordPeakSystem);
            
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
            
            var ipAddressParts = targetIpAddress.Split('.');
            
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
                              "Destination - " + targetIpAddress + ":" + port
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