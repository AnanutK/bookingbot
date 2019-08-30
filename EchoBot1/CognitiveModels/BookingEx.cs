using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1.CognitiveModels
{
    // Extends the partial FlightBooking class with methods and properties that simplify accessing entities in the luis results
    public partial class Booking
    {
        
        public string RoomEntities()
        {
            var roomNumber = Entities?.meeting_room?.FirstOrDefault().FirstOrDefault();
            return roomNumber;
        }

        public string BookTime()
        {
            return Entities?.datetime?.FirstOrDefault()?.Expressions?.FirstOrDefault();
        }
            
    }
}

