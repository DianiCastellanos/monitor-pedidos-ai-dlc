// downloadBlob: invoked from LogsPage.razor via IJSRuntime for CSV/JSON export
window.downloadBlob = function (filename, mimeType, content) {
    const blob = new Blob([content], { type: mimeType });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href     = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

// AlertsHub real-time connection (Blazor Server components use C# events via AlertBroadcaster).
// This hub connection is for non-Blazor consumers (e.g. external dashboards).
// Requires @microsoft/signalr placed at wwwroot/js/lib/signalr.min.js.
(function () {
    if (typeof signalR === 'undefined') return;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/alerts')
        .withAutomaticReconnect()
        .build();

    connection.on('ReceiveAlert', function (alert) {
        console.debug('[MonitorPedidos] Alert received via hub:', alert);
    });

    connection.start().catch(function (err) {
        console.warn('[MonitorPedidos] SignalR hub connection error:', err);
    });
}());
