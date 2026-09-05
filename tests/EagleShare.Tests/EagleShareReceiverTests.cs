using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Xunit;

namespace EagleShare.Tests;

public sealed class EagleShareReceiverTests
{
    [Fact]
    public async Task Receiver_StartsAnHttpListenerAndStopsIt()
    {
        var receiver = new EagleShareReceiver(GetAvailablePort());

        await receiver.StartAsync();

        using var client = new HttpClient();
        var response = await client.GetAsync($"http://127.0.0.1:{receiver.Port}/");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(receiver.IsRunning);

        await receiver.StopAsync();

        Assert.False(receiver.IsRunning);
    }

    [Fact]
    public void GetLanAddress_ReturnsAnIpv4Address()
    {
        var address = EagleShareReceiver.GetLanAddress();

        Assert.Equal(AddressFamily.InterNetwork, address.AddressFamily);
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}