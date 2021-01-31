using System;
using System.Collections.Generic;

#nullable disable

namespace DbLib.IntegrixContext.Models
{
    public partial class Warehouse
    {
        public Warehouse()
        {
            NotReservedItems = new HashSet<NotReservedItem>();
            Orders = new HashSet<Order>();
            ProductInfoInWarehouses = new HashSet<ProductInfoInWarehouse>();
        }

        public int WarehouseId { get; set; }
        public string Name { get; set; }

        public virtual ICollection<NotReservedItem> NotReservedItems { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<ProductInfoInWarehouse> ProductInfoInWarehouses { get; set; }
    }
}
