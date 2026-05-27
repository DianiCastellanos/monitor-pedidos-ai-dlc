using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Alerts;

public sealed record AlertMessage(
    string WhatHappened,
    DateTimeOffset WhenOccurred,
    string WhereOccurred,
    Severity Severity,
    string ProbableCause,
    string SuggestedAction
);
