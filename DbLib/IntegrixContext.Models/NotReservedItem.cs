using System;
using System.Collections.Generic;

#nullable disable

namespace DbLib.IntegrixContext.Models
{
    public partial class NotReservedItem
    {
        public int NotReservedItemsId { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; }
        public DateTime ReservationDate { get; set; }

        public virtual Product Product { get; set; }
        public virtual User User { get; set; }
        public virtual Warehouse Warehouse { get; set; }
    }
}
