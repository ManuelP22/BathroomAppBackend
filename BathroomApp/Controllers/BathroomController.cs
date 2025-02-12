using BathroomApp.Service;
using BathroomApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BathroomApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BathroomController : ControllerBase
    {
        private readonly IBathroomService _bathroomService;
        private readonly IHubContext<BathroomHub> _hubContext;

        public BathroomController(IBathroomService bathroomService, IHubContext<BathroomHub> hubContext)
        {
            _bathroomService = bathroomService;
            _hubContext = hubContext;
        }

        // GET: api/Bathroom
        [HttpGet]
        public IActionResult GetBathroomStatus()
        {
            return Ok(_bathroomService.Status);
        }

        // PUT: api/Bathroom/occupy
        [HttpPut("occupy")]
        public async Task<IActionResult> OccupyBathroom([FromBody] BathroomStatusDto statusDto)
        {
            string? userId = Request.Headers["X-User-Id"];
            if (string.IsNullOrEmpty(userId))
            {
                await _hubContext.Clients.Group(userId ?? "unknown")
                    .SendAsync("errorNotification", "No se ha proporcionado el identificador del usuario.");
                return Unauthorized("No se ha proporcionado el identificador del usuario.");
            }

            if (_bathroomService.Status.IsOccupied)
            {
                await _hubContext.Clients.Group(userId)
                    .SendAsync("errorNotification", "El baño ya está ocupado.");
                return BadRequest("El baño ya está ocupado.");
            }

            bool result = _bathroomService.Occupy(userId);
            if (!result)
            {
                await _hubContext.Clients.Group(userId)
                    .SendAsync("errorNotification", "No se pudo ocupar el baño.");
                return BadRequest("No se pudo ocupar el baño.");
            }

            // Se establece la actividad inicial, puede venir en el DTO o definirse como "none"
            _bathroomService.Status.Activity = statusDto.Activity;
            await _hubContext.Clients.All.SendAsync("bathroomStatusUpdate", _bathroomService.Status);
            return Ok(_bathroomService.Status);
        }

        // PUT: api/Bathroom/free
        [HttpPut("free")]
        public async Task<IActionResult> FreeBathroom()
        {
            string? userId = Request.Headers["X-User-Id"];
            if (string.IsNullOrEmpty(userId))
            {
                await _hubContext.Clients.Group(userId ?? "unknown")
                    .SendAsync("errorNotification", "No se ha proporcionado el identificador del usuario.");
                return Unauthorized("No se ha proporcionado el identificador del usuario.");
            }

            if (!_bathroomService.Status.IsOccupied)
            {
                await _hubContext.Clients.Group(userId)
                    .SendAsync("errorNotification", "El baño ya está libre.");
                return BadRequest("El baño ya está libre.");
            }
            if (_bathroomService.Status.OccupiedBy != userId)
            {
                await _hubContext.Clients.Group(userId)
                    .SendAsync("errorNotification", "Solo la persona que ocupó el baño puede liberarlo.");
                return Forbid("Solo la persona que ocupó el baño puede liberarlo.");
            }

            bool result = _bathroomService.Free(userId);
            if (!result)
            {
                await _hubContext.Clients.Group(userId)
                    .SendAsync("errorNotification", "No se pudo liberar el baño.");
                return BadRequest("No se pudo liberar el baño.");
            }

            // Al liberar, se resetea la actividad
            _bathroomService.Status.Activity = "none";
            await _hubContext.Clients.All.SendAsync("bathroomStatusUpdate", _bathroomService.Status);
            return Ok(_bathroomService.Status);
        }

        // PUT: api/Bathroom/activity
        [HttpPut("activity")]
        public async Task<IActionResult> UpdateBathroomActivity([FromBody] BathroomActivityDto activityDto)
        {
            string? userId = Request.Headers["X-User-Id"];
            if (string.IsNullOrEmpty(userId))
            {
                await _hubContext.Clients.Group(userId ?? "unknown")
                    .SendAsync("errorNotification", "No se ha proporcionado el identificador del usuario.");
                return Unauthorized("No se ha proporcionado el identificador del usuario.");
            }

            if (!_bathroomService.Status.IsOccupied)
            {
                await _hubContext.Clients.Group(userId)
                    .SendAsync("errorNotification", "El baño no está ocupado.");
                return BadRequest("El baño no está ocupado.");
            }
            if (_bathroomService.Status.OccupiedBy != userId)
            {
                await _hubContext.Clients.Group(userId)
                    .SendAsync("errorNotification", "Solo la persona que ocupó el baño puede cambiar la actividad.");
                return Forbid("Solo la persona que ocupó el baño puede cambiar la actividad.");
            }

            // Actualizamos la actividad sin modificar el estado de ocupación
            _bathroomService.Status.Activity = activityDto.Activity;
            await _hubContext.Clients.All.SendAsync("bathroomStatusUpdate", _bathroomService.Status);
            return Ok(_bathroomService.Status);
        }

        [HttpPut("force-free")]
        public async Task<IActionResult> ForceFreeBathroom()
        {

            bool result = _bathroomService.ForceFree();
            if (!result)
            {
                return BadRequest("No se pudo liberar el baño.");
            }

            // Al liberar, se resetea la actividad
            _bathroomService.Status.Activity = "none";
            await _hubContext.Clients.All.SendAsync("bathroomStatusUpdate", _bathroomService.Status);
            return Ok(_bathroomService.Status);
        }

    }

    // DTO para ocupar el baño
    public class BathroomStatusDto
    {
        public bool Occupied { get; set; }
        // Se puede enviar "none", "peeing" o "pooping"
        public string Activity { get; set; } = "none";
    }

    // DTO para actualizar únicamente la actividad
    public class BathroomActivityDto
    {
        public string Activity { get; set; }
    }
}