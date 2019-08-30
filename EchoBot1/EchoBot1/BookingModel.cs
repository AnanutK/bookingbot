using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1
{
    public class BookingModel
    {
        public string AssetName { get; set; }

        public int AssetId { get; set; }
        public string Date { get; set; }

        public string TimeFrom { get; set; }
        public string TimeTo { get; set; }
    }
}
