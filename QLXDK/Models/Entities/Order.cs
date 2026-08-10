using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLXDK.Models.Entities
{
    public class Order
    {
        [Key]
        public int ID { get; set; }

        public int UserID { get; set; }

        public string OrderCode { get; set; }
        public DateTime CreatedDate { get; set; }
        public Nullable<System.DateTime> PaymentDate { get; set; }
        public long Amount { get; set; }

        public string Status { get; set; }
        public string Token { get; set; }
        public string QrCode { get; set; }
        public string CustomerCode { get; set; }
        //public string CustomerName { get; set; }
        //public string CompanyName { get; set; }
        public string TransactionRefNo { get; set; }

        //public Nullable<System.DateTime> UpdatedAt { get; set; }
        //public Nullable<System.DateTime> DeletedAt { get; set; }

    }
}