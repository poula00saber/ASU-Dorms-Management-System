using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Application.DTOs.Auth
{
    public class LoginResponseDto
    {
        public string Token { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }
        public int DormLocationId { get; set; }
        public string DormLocationName { get; set; }
        // NEW: For multi-location access
        public List<int> AccessibleDormLocationIds { get; set; }
        public Dictionary<int, string> AccessibleDormLocations { get; set; }
    }
}
