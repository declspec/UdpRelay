using System;
using System.Net;
using System.Text.RegularExpressions;

namespace ConsoleApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var local = new IPEndPoint(IPAddress.Any, 53);
            var target = new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53);

            var record = new ResourceRecord(IPAddress.Parse("192.168.56.104"), 10000, new Regex("\\.dev\\.io$"));
            var interceptor = new DnsInterceptor(new[] { record });

            using (var relay = new UdpRelay(local, target, interceptor)) {
                var buffer = new byte[512];
                relay.Bind();

                while (true) {
                    try {
                        relay.RelayPacket(buffer);
                        Console.WriteLine("info: forwarded packet");
                    }
                    catch(Exception e) {
                        Console.WriteLine("warning: {0}", e.Message);
                    }
                }
            }
        }
    }
}
