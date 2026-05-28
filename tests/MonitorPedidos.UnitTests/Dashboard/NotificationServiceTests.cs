using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Web.Hubs;
using MonitorPedidos.Web.Services;
using Moq;
using Xunit;

namespace MonitorPedidos.UnitTests.Dashboard;

public class NotificationServiceTests
{
    // ADR-U6-01: triple mock para IHubContext
    private static (NotificationService svc, Mock<IClientProxy> clientProxy, AlertBroadcaster broadcaster) BuildSut()
    {
        var broadcaster = new AlertBroadcaster();

        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hubClients = new Mock<IHubClients>();
        hubClients.Setup(c => c.All).Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<AlertsHub>>();
        hubContext.Setup(h => h.Clients).Returns(hubClients.Object);

        var svc = new NotificationService(broadcaster, hubContext.Object, Mock.Of<ILogger<NotificationService>>());
        return (svc, clientProxy, broadcaster);
    }

    [Fact]
    public async Task NotifyIncidentAsync_BroadcastsClose_AndSendsToHub()
    {
        var (svc, clientProxy, broadcaster) = BuildSut();
        Guid? capturedId = null;
        broadcaster.OnIncidentClosed += id => { capturedId = id; return Task.CompletedTask; };

        var incidentId = Guid.NewGuid();
        await svc.NotifyIncidentAsync(incidentId);

        Assert.Equal(incidentId, capturedId);
        clientProxy.Verify(c => c.SendCoreAsync(
            "IncidentUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyStatusChangeAsync_SendsSystemStatusUpdated_ToHub()
    {
        var (svc, clientProxy, _) = BuildSut();

        await svc.NotifyStatusChangeAsync(ModuleId.DbOrderChecker, CheckStatus.Ok);

        clientProxy.Verify(c => c.SendCoreAsync(
            "SystemStatusUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyIncidentAsync_BroadcasterThrows_HubStillCalled()
    {
        // BR-NOTIF-01: error en un canal no bloquea al otro
        var (svc, clientProxy, broadcaster) = BuildSut();
        broadcaster.OnIncidentClosed += _ => throw new InvalidOperationException("broadcaster failure");

        await svc.NotifyIncidentAsync(Guid.NewGuid()); // must not throw

        clientProxy.Verify(c => c.SendCoreAsync(
            "IncidentUpdated", It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
