using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace QLXDK.Models.Views
{
    public class UserVM
    {

        public int ID { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "Tên đăng nhập từ 4-20 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; }

        public int Type { get; set; }
        public int Active { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        public string Password { get; set; }
    }

    public class EditViewModel
    {
        public int ID { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "Tên đăng nhập từ 4-20 ký tự")]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        public int Type { get; set; }
        public int Active { get; set; }
    }
}