using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodDelivery.Shared.Contracts.Events
{
    public class OrderReadyForDeliveryEvent
    {
        public string OrderId { get; set; } = string.Empty;
        public string RiderNote { get; set; } = string.Empty;
    }
}
