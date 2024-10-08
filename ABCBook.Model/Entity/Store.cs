﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCBook.Model.Entity
{
    [Table(nameof(Store))]
    public class Store
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string City { get; set; }
        public DateTime OpenningDate { get; set; }
        public DateTime ClosedDate { get; set; }
        public bool IsOpened { get; set; }
    }
}
