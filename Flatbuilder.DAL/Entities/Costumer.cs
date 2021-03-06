﻿using System.Collections.Generic;

namespace Flatbuilder.DAL.Entities
{
    public class Costumer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public ICollection<Order> Orders { get; set; }
        public Costumer()
        {
            Orders = new List<Order>();
        }     
    }
}