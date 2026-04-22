using System;
using GOKDOGANIHA.Core.Models.Alerts;

namespace GOKDOGANIHA.Core.Abstractions;

/// <summary>Alert tüketen tarafın gördüğü arayüz. AlertConsole, Toast, Audio.</summary>
public interface IAlertSubscriber
{
    event EventHandler<Alert>? AlertPublished;
}
