using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThinkFun
{
    public class Configuration
    {
        public HashSet<string> FavoriteElements { get; set; } = new HashSet<string>();

        public string? Destination { get; set; } = null;

        public int FilterType { get; set; } = 0;
        public int FilterPark { get; set; } = 0;
        public int FilterStatus { get; set; } = 0;

        public void ToggleFavorite(string ParkElementId)
        {
            if(FavoriteElements.Contains(ParkElementId))
                FavoriteElements.Remove(ParkElementId);
            else
                FavoriteElements.Add(ParkElementId);
        }
    }
}
