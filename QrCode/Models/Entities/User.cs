using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLXDK.Models.Entities
{
    public class User
    {
        [Key]
        public int ID { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Type { get; set; }
        public int Active { get; set; }
        public System.DateTime? CreatedAt { get; set; }
        public System.DateTime? UpdatedAt { get; set; }
        public Nullable<System.DateTime> DeletedAt { get; set; }

    }  
}