using System;
using System.Collections.Generic;

#nullable disable

namespace DbLib.IntegrixContext.Models
{
    public partial class User
    {
        public User()
        {
            NotReservedItems = new HashSet<NotReservedItem>();
            Orders = new HashSet<Order>();
        }

        public int UserId { get; set; }
        public string Name { get; set; }

        public virtual ICollection<NotReservedItem> NotReservedItems { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
    }
}
