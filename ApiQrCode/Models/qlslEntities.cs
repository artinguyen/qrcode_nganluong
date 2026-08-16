using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using QLXDK.Models;

namespace ApiQrCode.Models
{
    public class qlslContext : DbContext
    {
        // 1. Khởi tạo và trỏ đến tên Connection String trong Web.config
        public qlslContext() : base("name=qlslConnect")
        {
        }

        public DbSet<Entities.User> Users { get; set; }
        public DbSet<Entities.Order> Orders { get; set; }
    }
}