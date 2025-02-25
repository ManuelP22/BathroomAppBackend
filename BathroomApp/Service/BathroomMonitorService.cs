using BathroomApp.Service;
using Microsoft.AspNetCore.SignalR;

public class BathroomMonitorService : BackgroundService
{
    private readonly IBathroomService _bathroomService;
    private readonly IHubContext<BathroomHub> _hubContext;
    private readonly IConfigurationService _configurationService;
    private int? _lastNotifiedThreshold = null;
    private readonly int _firstAlert;
    private readonly int _secondAlert;
    private readonly int _thirdAlert;
    private readonly int _finalAlert;

    public BathroomMonitorService(
        IBathroomService bathroomService,
        IHubContext<BathroomHub> hubContext,
        IConfigurationService configurationService)
    {
        _bathroomService = bathroomService;
        _hubContext = hubContext;
        _configurationService = configurationService;
        // Se obtienen los valores configurados y, si son 0, se asignan los valores por defecto.
        _firstAlert = _configurationService.GetFirstAlert() > 0 ? _configurationService.GetFirstAlert() : 10;
        _secondAlert = _configurationService.GetSecondAlert() > 0 ? _configurationService.GetSecondAlert() : 20;
        _thirdAlert = _configurationService.GetThirdAlert() > 0 ? _configurationService.GetThirdAlert() : 25;
        _finalAlert = _configurationService.GetFinalAlert() > 0 ? _configurationService.GetFinalAlert() : 30;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_bathroomService.Status.IsOccupied && !string.IsNullOrEmpty(_bathroomService.Status.OccupiedSince))
            {
                DateTime occupiedTime = DateTime.Parse(_bathroomService.Status.OccupiedSince);
                var elapsed = DateTime.UtcNow - occupiedTime;
                var elapsedMinutes = (int)elapsed.Minutes;

                // Alerta final: liberar el baño automáticamente
                if (elapsedMinutes >= _finalAlert)
                {
                    _bathroomService.ForceFree();
                    await _hubContext.Clients.All.SendAsync("bathroomStatusUpdate", _bathroomService.Status, stoppingToken);
                    if (!string.IsNullOrEmpty(_bathroomService.Status.OccupiedBy))
                    {
                        await _hubContext.Clients.Group(_bathroomService.Status.OccupiedBy)
                            .SendAsync("bathroomReminder", new
                            {
                                message = $"El baño ha sido liberado automáticamente tras {_finalAlert} minutos.",
                                vibration = "very-strong",
                                occupantToken = _bathroomService.Status.OccupiedBy
                            }, stoppingToken);
                    }
                    _lastNotifiedThreshold = null;
                }
                else
                {
                    // Recordatorio para la tercera alerta (por ejemplo, 25 minutos)
                    if (elapsedMinutes >= _thirdAlert && (_lastNotifiedThreshold == null || _lastNotifiedThreshold < _thirdAlert))
                    {
                        var minutesLeft = (_finalAlert - _thirdAlert);
                        var extraMessage = minutesLeft > 0 ? $" En {minutesLeft} minutos será liberado automáticamente." : "";
                        await _hubContext.Clients.Group(_bathroomService.Status.OccupiedBy)
                            .SendAsync("bathroomReminder", new
                            {
                                message = $"Llevas {_thirdAlert} minutos ocupando el baño.{extraMessage}",
                                vibration = "very-strong",
                                occupantToken = _bathroomService.Status.OccupiedBy
                            }, stoppingToken);
                        _lastNotifiedThreshold = _thirdAlert;
                    }
                    // Recordatorio para la segunda alerta (por ejemplo, 20 minutos)
                    else if (elapsedMinutes >= _secondAlert && (_lastNotifiedThreshold == null || _lastNotifiedThreshold < _secondAlert))
                    {
                        await _hubContext.Clients.Group(_bathroomService.Status.OccupiedBy)
                            .SendAsync("bathroomReminder", new
                            {
                                message = $"Llevas {_secondAlert} minutos ocupando el baño.",
                                vibration = "strong",
                                occupantToken = _bathroomService.Status.OccupiedBy
                            }, stoppingToken);
                        _lastNotifiedThreshold = _secondAlert;
                    }
                    // Recordatorio para la primera alerta (por ejemplo, 10 minutos)
                    else if (elapsedMinutes >= _firstAlert && (_lastNotifiedThreshold == null || _lastNotifiedThreshold < _firstAlert))
                    {
                        await _hubContext.Clients.Group(_bathroomService.Status.OccupiedBy)
                            .SendAsync("bathroomReminder", new
                            {
                                message = $"Llevas {_firstAlert} minutos ocupando el baño.",
                                vibration = "normal",
                                occupantToken = _bathroomService.Status.OccupiedBy
                            }, stoppingToken);
                        _lastNotifiedThreshold = _firstAlert;
                    }
                }
            }
            else
            {
                // Reiniciamos el seguimiento de notificaciones si el baño está libre
                _lastNotifiedThreshold = null;
            }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
