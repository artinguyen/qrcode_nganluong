using System;
using System.Linq;
using System.Web.Mvc;
using QLXDK.Models;
using QLXDK.Models.Views;
using ClosedXML.Excel;
using System.IO;
using System.Collections.Generic;

namespace QLXDK.Controllers
{
    [Authorize(Users = "admin,phct")]
    public class ReportController : Controller
    {
        private qlslContext _db = new qlslContext();
        int userAdmin = 18;
        // GET: Report
        public ActionResult Index()
        {
            var UserList = _db.Users
                        .Where(v => v.ID != userAdmin)
                        .Select(v => new
                        {
                            Id = v.ID,
                            Name = v.FullName
                        })
                        .ToList();
            ViewBag.UserList = new SelectList(UserList, "Id", "Name");
            return View();
        }

        [HttpGet]
        public ActionResult Preview(string UserId, string Type, string FromDate, string ToDate)
        {
            try
            {
                DateTime FromDateParsed = DateTime.Parse(FromDate);
                DateTime ToDateParsed = DateTime.Parse(ToDate);
                DateTime maxAllowedDate = FromDateParsed.AddMonths(3);
                // Valid date
                if (ToDateParsed > maxAllowedDate)
                {
                    return Json(new { success = false, message = "Khoảng thời gian tìm kiếm không được vượt quá 3 tháng." }, JsonRequestBehavior.AllowGet);
                }

                List<OrderVM> orderList = getOrderList(UserId, Type, FromDateParsed, ToDateParsed);
                return PartialView("_Preview", orderList);
            }
            catch (Exception e)
            {
                return Json(new { success = false, message = "Đã có lỗi xảy ra" }, JsonRequestBehavior.AllowGet);
            }
        }

        public List<OrderVM> getOrderList(string UserId, string Type, DateTime FromDateParsed, DateTime ToDateParsed)
        {
            var query = from od in _db.Orders
                        join us in _db.Users on od.UserID equals us.ID
                        where od.CreatedDate >= FromDateParsed && od.CreatedDate <= ToDateParsed && od.UserID != userAdmin
                        select od;

            if (!string.IsNullOrEmpty(UserId))
            {
                int id = int.Parse(UserId);
                query = query.Where(x => x.UserID == id);
            }

            if (!string.IsNullOrEmpty(Type))
            {
                query = query.Where(x => x.Status == Type);
            }

            var orderList = query.Select(x => new OrderVM
            {
                OrderCode = x.OrderCode,
                CreatedDate = x.CreatedDate,
                PaymentDate = x.PaymentDate,
                Amount = x.Amount,
                Status = x.Status,
                TransactionRefNo = x.TransactionRefNo
            }).ToList();
            return orderList;
        }

        [HttpPost]
        public ActionResult ExportExcel(string User, string Type, string FromDate, string ToDate)
        {
            try
            {
                DateTime FromDateParsed = DateTime.Parse(FromDate);
                DateTime ToDateParsed = DateTime.Parse(ToDate);
                DateTime maxAllowedDate = FromDateParsed.AddMonths(3);
                // Valid date
                if (ToDateParsed > maxAllowedDate)
                {
                    TempData["Message"] = "Khoảng thời gian tìm kiếm không được vượt quá 3 tháng.";
                    return RedirectToAction("Index");
                }

                // Create a workbook
                var wb = new XLWorkbook();
                wb.Style.Font.FontName = "Times New Roman";
                wb.Style.Font.FontSize = 12;
                // Add Line sheet
                var wl = wb.AddWorksheet("Sheet1");
                // Add Header
                wl.Cell("A1").Value = "Đơn hàng";
                wl.Cell("B1").Value = "Ngày tạo";
                wl.Cell("C1").Value = "Ngày thanh toán";
                wl.Cell("D1").Value = "Số tiền";
                wl.Cell("E1").Value = "Trạng thái";
                wl.Cell("F1").Value = "Transaction No";
                // Format Header
                var headerRange = wl.Range(1, 1, 1, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                int rowList = 2;
                List<OrderVM> orderList = getOrderList(User, Type, FromDateParsed, ToDateParsed);

                foreach (var item in orderList)
                {
                    string status = "Chờ thanh toán";
                    wl.Cell(rowList, 1).Value = item.OrderCode;
                    wl.Cell(rowList, 2).Value = item.CreatedDate != null ? item.CreatedDate.ToString("dd/MM/yyyy H:m:s") : "";
                    wl.Cell(rowList, 3).Value = item.PaymentDate != null ? item.PaymentDate.Value.ToString("dd/MM/yyyy H:m:s") : "";
                    wl.Cell(rowList, 4).Value = item.Amount;

                    if (item.Status == "3")
                    {
                        status = "Đã thanh toán";
                    }

                    wl.Cell(rowList, 5).Value = status;
                    if (item.Status == "1")
                    {
                        wl.Range(rowList, 1, rowList, 6).Style.Fill.BackgroundColor = XLColor.Pink;
                    }

                    wl.Cell(rowList, 6).Value = item.TransactionRefNo;
                    rowList++;
                }
                // Add border
                var listRange = wl.Range("A1:" + "F" + (rowList - 1));
                listRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                listRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                listRange.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                listRange.Style.Border.RightBorder = XLBorderStyleValues.Thin;

                wl.Columns().AdjustToContents();
                var ms = new MemoryStream();
                wb.SaveAs(ms);

                var bytes = ms.ToArray();
                string filename = "Report_" + DateTime.Now.ToString("ddMMyyyyHHmmss");
                return File(bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    filename + ".xlsx");
            }
            catch (Exception e)
            {
                TempData["Message"] = "Đã có lỗi xảy ra";
                return RedirectToAction("Index");
            }
        }
    }
}