using System;
using System.Collections.Generic;

#nullable disable

namespace DbLib.IntegrixContext.Models
{
    public partial class Product
    {
        public Product()
        {
            OrderItems = new HashSet<OrderItem>();
            ProductInfoInWarehouses = new HashSet<ProductInfoInWarehouse>();
        }

        public int ProductId { get; set; }
        public string FullName { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual ICollection<ProductInfoInWarehouse> ProductInfoInWarehouses { get; set; }
    }
}
