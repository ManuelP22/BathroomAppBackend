namespace BathroomApp.Models
{
    public class BathroomStatus
    {
        public bool IsOccupied { get; set; }
        public string? OccupiedBy { get; set; }
        public string Activity { get; set; } = "none";  // Valores posibles: "none", "peeing", "pooping"
        public string? OccupiedSince { get; set; }
    }

}
