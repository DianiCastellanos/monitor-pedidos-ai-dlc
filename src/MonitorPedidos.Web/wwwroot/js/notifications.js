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

// ── Tab title alert ──────────────────────────────────────────────────────────
let _titleBlinkInterval = null;
let _originalTitle      = '';

window.startTitleAlert = function () {
    if (_titleBlinkInterval) return;           // ya está parpadeando
    _originalTitle = document.title;
    let on = true;
    _titleBlinkInterval = setInterval(function () {
        document.title = on ? '⚠ ALERTA — MonitorPedidos' : _originalTitle;
        on = !on;
    }, 900);
};

window.stopTitleAlert = function () {
    if (_titleBlinkInterval) {
        clearInterval(_titleBlinkInterval);
        _titleBlinkInterval = null;
    }
    if (_originalTitle) document.title = _originalTitle;
};

// ── AlertsHub SignalR (dispara startTitleAlert en cualquier página) ──────────
(function () {
    if (typeof signalR === 'undefined') return;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/alerts')
        .withAutomaticReconnect()
        .build();

    connection.on('ReceiveAlert', function (alert) {
        console.debug('[MonitorPedidos] Alert received via hub:', alert);
        window.startTitleAlert();
    });

    connection.start().catch(function (err) {
        console.warn('[MonitorPedidos] SignalR hub connection error:', err);
    });
}());
