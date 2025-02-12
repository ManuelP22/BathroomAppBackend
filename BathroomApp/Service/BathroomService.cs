using BathroomApp.Models;

namespace BathroomApp.Service
{
    public interface IBathroomService
    {
        BathroomStatus Status { get; }
        bool Occupy(string userId);  // Retorna true si se ocupa correctamente, false si ya está ocupado
        bool Free(string userId);    // Retorna true si se libera, false si el usuario no es el que lo ocupó o ya está libre

        bool ForceFree();
    }

    public class BathroomService : IBathroomService
    {
        // Estado inicial: libre y sin usuario asignado
        public BathroomStatus Status { get; private set; } = new BathroomStatus { IsOccupied = false, OccupiedBy = null };

        public bool Occupy(string userId)
        {
            if (Status.IsOccupied)
                return false;
            Status.IsOccupied = true;
            Status.OccupiedBy = userId;
            Status.Activity = "none";
            // Al ocupar el baño se registra el instante actual en formato ISO (UTC)
            Status.OccupiedSince = DateTime.UtcNow.ToString("o");
            return true;
        }

        public bool Free(string userId)
        {
            if (!Status.IsOccupied)
                return false;
            if (Status.OccupiedBy != userId)
                return false;
            Status.IsOccupied = false;
            Status.OccupiedBy = null;
            Status.Activity = "none";
            // Al liberar el baño se reinicia el tiempo de ocupación
            Status.OccupiedSince = null;
            return true;
        }

        public bool ForceFree()
        {
            Status.IsOccupied = false;
            Status.OccupiedBy = null;
            Status.Activity = "none";
            Status.OccupiedSince = null;
            return true;
        }
    }
}
