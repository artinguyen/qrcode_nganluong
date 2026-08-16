using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLXDK.Models.Views
{
    public class OrderVM
    {
        [Key]
        public int ID { get; set; }

        public int UserID { get; set; }

        public string OrderCode { get; set; }
        public DateTime CreatedDate { get; set; }

        public Nullable<System.DateTime> PaymentDate { get; set; }
        public long Amount { get; set; }

        public string Status { get; set; }
        public string QrCode { get; set; }
    }
}