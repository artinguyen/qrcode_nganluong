using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using QLXDK.Models;
using System.Linq;
using System.Web;
using QLXDK.Models.Views;
using System.Security.Cryptography;

namespace QLXDK.Controllers
{
    [RoutePrefix("api/VCBPayment")]
    public class QrCodeApiController : ApiController
    {
        private qlslContext _db = new qlslContext();
        [HttpGet]
        [Route("Inquiry")]
        public HttpResponseMessage InquiryGetFallback()
        {
            var errorResponse = new
            {
                code = "405",
                message = "Sai phương thức kết nối"
            };

            return Request.CreateResponse(System.Net.HttpStatusCode.MethodNotAllowed, errorResponse);
        }

        [HttpPost]
        [Route("Inquiry")]
        public HttpResponseMessage Inquiry([FromBody] dynamic request)
        {
            // =========================================================================
            // BƯỚC 1: XÁC THỰC BASIC AUTHENTICATION (VIẾT THẲNG TRONG API)
            // =========================================================================
            var authHeader = Request.Headers.Authorization;

            // Kiểm tra xem có gửi kèm Header Basic đúng chuẩn không
            if (authHeader == null || !authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, new { error_code = "401", error_description = "Thiếu hoặc sai định dạng Header Authorization." });
            }

            try
            {
                // Giải mã chuỗi Base64 từ đối tác gửi lên
                string credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter));
                string[] parts = credentials.Split(':');

                if (parts.Length != 2)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, new { error_code = "401", error_description = "Chuỗi mã hóa Authorization không đúng cấu trúc." });
                }

                string username = parts[0];
                string password = parts[1];

                // Đọc thông tin tài khoản cấu hình trong Web.config
                //string validUsername = ConfigurationManager.AppSettings["BasicAuth_Username"];
                //string validPassword = ConfigurationManager.AppSettings["BasicAuth_Password"];
                string validUsername = "vcb_client";
                string validPassword = "NrapTC52phfYTHIx@";
                // So sánh tài khoản mật khẩu
                if (username != validUsername || password != validPassword)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, new { error_code = "401", error_description = "Tài khoản hoặc mật khẩu không đúng." });
                }
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, new { error_code = "401", error_description = "Lỗi giải mã chuỗi mã hóa bí mật." });
            }


            // =========================================================================
            // BƯỚC 2: XỬ LÝ DỮ LIỆU ĐẦU VÀO (DYNAMIC - KHÔNG MODEL) VÀ LOGIC CRUD
            // =========================================================================
            //if (request == null)
            //{
            //    return Request.CreateResponse(HttpStatusCode.BadRequest, new { code = "01", message = "Dữ liệu body request trống." });
            //}

            try
            {
                // Bóc tách dữ liệu động trực tiếp từ chuỗi JSON trong ảnh của bạn
                string channelId = request.context?.channelId;
                string channelRefNumber = request.context?.channelRefNumber;
                string requestDateTime = request.context?.requestDateTime;

                string customerCode = request.payload?.customerCode;
                //string providerId = request.payload?.providerId;
                string reqSignature = request.signature;

                //string signature = request.signature;
                string secretKey = "viETCOMBAnkCangSaiGon@159";
                string responseMsgId = new Random().Next(1000000, 9999999).ToString();
                string msgReq = channelId + "|" + channelRefNumber;
                string msgRes = channelId + "|" + channelRefNumber + "|" + responseMsgId;

                //string msgPartInput = $"{customerCode}";
                if (!VerifyMessageSHA256(secretKey, msgReq, reqSignature))
                {
                    //return Request.CreateResponse(HttpStatusCode.OK, CreateErrorResponse(channelId, channelRefNumber, 2, "Chữ ký điện tử SHA-256 không hợp lệ."));
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { error_code = "2", error_description = "Chữ ký không hợp lệ" });
                }



                var order = _db.Orders
                    .FirstOrDefault(o => o.CustomerCode == customerCode);

                if (order == null)
                {
                    // Trả về lỗi nếu không tìm thấy thông tin đơn hàng của khách hàng này
                    return Request.CreateResponse(HttpStatusCode.OK, new
                    {
                        context = new
                        {
                            channelId = channelId,
                            channelRefNumber = channelRefNumber,
                            errorCode = 1,
                            errorMessage = "Không tìm thấy thông tin hóa đơn.",
                            requestDateTime = requestDateTime,
                            responseMsgId = responseMsgId,
                            status = "FAILURE"
                        },
                        signature = CreateMessageSHA256(secretKey, msgRes)
                    });
                }

                var apiResponse = new
                {
                    context = new
                    {
                        channelId = channelId, // Lấy từ request hoặc cứng "VAH"
                        channelRefNumber = channelRefNumber,
                        errorCode = 0,
                        errorMessage = "",
                        requestDateTime = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
                        responseMsgId = new Random().Next(1000000, 9999999).ToString(), // Sinh ID ngẫu nhiên hoặc theo chuỗi hệ thống
                        status = "SUCCESS"
                    },
                    payload = new
                    {
                        customerCode = order.CustomerCode, // Lấy từ bảng Orders
                        paymentSequence = "1",
                        bills = new[] // Khởi tạo mảng Json array []
                        {
                            new
                            {
                                amount = order.Amount, // Số tiền hóa đơn lấy từ DB (Ép kiểu về số chuỗi nếu ngân hàng cần chuỗi)
                                billId = order.OrderCode ?? order.CustomerCode, // Mã hóa đơn lấy từ DB
                                addnlFields = new[] // Mảng lồng chứa tên khách hàng
                                {
                                    new
                                    {
                                        fieldId = "CustName",
                                        fieldValue = order.CustomerName
                                    }
                                }
                            }
                        }
                    },
                    signature = "20f5b41b12c349783475ab35429bb1bf" // Chuỗi signature hệ thống tự sinh (tạm thời để cứng theo mẫu)
                };

                // Trả dữ liệu JSON chuẩn về cho ngân hàng
                return Request.CreateResponse(HttpStatusCode.OK, apiResponse);


                // --- KHU VỰC VIẾT LOGIC CRUD / TRUY VẤN CƠ SỞ DỮ LIỆU ---
                // Bạn viết trực tiếp lệnh gọi DB tại đây để lấy thông tin QR
                // Ví dụ: var qrData = db.QrCodes.FirstOrDefault(x => x.Code == customerCode);
                // -------------------------------------------------------

                // Trả về kết quả JSON thành công cho đối tác
                //var responseResult = new
                //{
                //    code = "00",
                //    message = "Xác thực và truy vấn thông tin thành công",
                //    data = new
                //    {
                //        processedCustomer = customerCode,
                //        serviceType = serviceId,
                //        systemTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                //    }
                //};

                //return Request.CreateResponse(HttpStatusCode.OK, responseResult);
            }
            catch (Exception ex)
            {
                // Trả về lỗi 500 nếu hệ thống bị crash trong quá trình chạy
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error_code = "500", error_description = ex.Message });
            }
        }

        public String CreateSignature(string msgPart)
        {
            string mySignature = "";
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(msgPart);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2")); // "x2" đảm bảo chữ thường, giống chuỗi mã mẫu
                }
                mySignature = sb.ToString();
            }
            return mySignature;
        }

        [HttpPost]
        [Route("Payment")]
        public HttpResponseMessage Payment([FromBody] dynamic request)
        {
            // =========================================================================
            // BƯỚC 1: XÁC THỰC BASIC AUTHENTICATION (VIẾT THẲNG TRONG API)
            // =========================================================================
            var authHeader = Request.Headers.Authorization;

            // Kiểm tra xem có gửi kèm Header Basic đúng chuẩn không
            if (authHeader == null || !authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, new { error_code = "401", error_description = "Thiếu hoặc sai định dạng Header Authorization." });
            }

            try
            {
                // Giải mã chuỗi Base64 từ đối tác gửi lên
                string credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter));
                string[] parts = credentials.Split(':');

                if (parts.Length != 2)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, new { error_code = "401", error_description = "Chuỗi mã hóa Authorization không đúng cấu trúc." });
                }

                string username = parts[0];
                string password = parts[1];

                // Đọc thông tin tài khoản cấu hình trong Web.config
                //string validUsername = ConfigurationManager.AppSettings["BasicAuth_Username"];
                //string validPassword = ConfigurationManager.AppSettings["BasicAuth_Password"];
                string validUsername = "vcb_client";
                string validPassword = "NrapTC52phfYTHIx@";
                // So sánh tài khoản mật khẩu
                if (username != validUsername || password != validPassword)
                {
                    return Request.CreateResponse(HttpStatusCode.Unauthorized, new { error_code = "401", error_description = "Tài khoản hoặc mật khẩu không đúng." });
                }
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, new { error_code = "401", error_description = "Lỗi giải mã chuỗi mã hóa bí mật." });
            }


            // =========================================================================
            // BƯỚC 2: XỬ LÝ DỮ LIỆU ĐẦU VÀO (DYNAMIC - KHÔNG MODEL) VÀ LOGIC CRUD
            // =========================================================================
            //if (request == null)
            //{
            //    return Request.CreateResponse(HttpStatusCode.BadRequest, new { code = "01", message = "Dữ liệu body request trống." });
            //}

            try
            {
                // Bóc tách dữ liệu động trực tiếp từ chuỗi JSON trong ảnh của bạn
                string channelId = request.context?.channelId;
                string channelRefNumber = request.context?.channelRefNumber;
                string requestDateTime = request.context?.requestDateTime;

                string customerCode = request.payload?.customerCode;
                //string providerId = request.payload?.providerId;
                //string serviceId = request.payload?.serviceId;
                string billId = request.context?.bills.billId;
                string amount = request.context?.bills.amount;
                //string signature = request.signature;
                string secretKey = "viETCOMBAnkCangSaiGon@159";
                string responseMsgId = new Random().Next(1000000, 9999999).ToString();
                string reqSignature = request.signature;


                string msgReq = channelId + "|" + channelRefNumber;
                string msgRes = channelId + "|" + channelRefNumber + "|" + responseMsgId;
                //var order = _db.Orders
                //    .FirstOrDefault(o => o.CustomerCode == customerCode);

                //if (order == null)
                //{
                //    // Trả về lỗi nếu không tìm thấy thông tin đơn hàng của khách hàng này
                //    return Request.CreateResponse(HttpStatusCode.OK, new
                //    {
                //        context = new
                //        {
                //            channelId = channelId,
                //            channelRefNumber = channelRefNumber,
                //            errorCode = 1,
                //            errorMessage = "Không tìm thấy thông tin hóa đơn.",
                //            requestDateTime = requestDateTime,
                //            responseMsgId = responseMsgId,
                //            status = "FAILURE"
                //        }
                //    });
                //}

                if (!VerifyMessageSHA256(secretKey, msgReq, reqSignature))
                {
                    //return Request.CreateResponse(HttpStatusCode.OK, CreateErrorResponse(channelId, channelRefNumber, 2, "Chữ ký điện tử SHA-256 không hợp lệ."));
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { error_code = "2", error_description = "Chữ ký không hợp lệ" });
                }

                var apiResponse = new
                {
                    context = new
                    {
                        channelId = channelId, // Lấy từ request hoặc cứng "VAH"
                        channelRefNumber = channelRefNumber,
                        errorCode = 0,
                        errorMessage = "",
                        requestDateTime = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
                        responseMsgId = new Random().Next(1000000, 9999999).ToString(), // Sinh ID ngẫu nhiên hoặc theo chuỗi hệ thống
                        status = "SUCCESS"
                    },
                    payload = new
                    {
                        //customerCode = order.CustomerCode, // Lấy từ bảng Orders
                        //paymentSequence = "1",
                        bills = new[] // Khởi tạo mảng Json array []
                        {
                            new
                            {
                                providerId = "SGL",
                                serviceId = "PAYMENTINFO",
                                //amount = order.Amount, // Số tiền hóa đơn lấy từ DB (Ép kiểu về số chuỗi nếu ngân hàng cần chuỗi)
                                //billId = order.OrderCode ?? order.CustomerCode, // Mã hóa đơn lấy từ DB
                                bills = new[] // Mảng lồng chứa tên khách hàng
                                {
                                    new
                                    {
                                        amount = amount,
                                        billErrCode = "0",
                                        billErrDesc = "",
                                        billId = billId 
                                    }
                                }
                            }
                        }
                    },
                    signature = CreateMessageSHA256(secretKey, msgRes) // Chuỗi signature hệ thống tự sinh (tạm thời để cứng theo mẫu)
                };

                // Trả dữ liệu JSON chuẩn về cho ngân hàng
                return Request.CreateResponse(HttpStatusCode.OK, apiResponse);


                // --- KHU VỰC VIẾT LOGIC CRUD / TRUY VẤN CƠ SỞ DỮ LIỆU ---
                // Bạn viết trực tiếp lệnh gọi DB tại đây để lấy thông tin QR
                // Ví dụ: var qrData = db.QrCodes.FirstOrDefault(x => x.Code == customerCode);
                // -------------------------------------------------------

                // Trả về kết quả JSON thành công cho đối tác
                //var responseResult = new
                //{
                //    code = "00",
                //    message = "Xác thực và truy vấn thông tin thành công",
                //    data = new
                //    {
                //        processedCustomer = customerCode,
                //        serviceType = serviceId,
                //        systemTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                //    }
                //};

                //return Request.CreateResponse(HttpStatusCode.OK, responseResult);
            }
            catch (Exception ex)
            {
                // Trả về lỗi 500 nếu hệ thống bị crash trong quá trình chạy
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error_code = "500", error_description = ex.Message });
            }
        }

        private static bool VerifyMessageSHA256(string secretKey, string msgPart, string signature)
        {
            if (string.IsNullOrEmpty(signature)) return false;

            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                // Ghép chuỗi theo đúng cấu trúc tài liệu của ngân hàng: msgPart + "|" + secretKey
                string input = msgPart + "|" + secretKey;

                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                // Chuyển đổi chuỗi byte array sang định dạng Hex viết thường (%02x)
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                // Tiến hành so khớp chuỗi chữ ký (Không phân biệt hoa thường)
                return sb.ToString().Equals(signature, StringComparison.OrdinalIgnoreCase);
            }
        }

        // Hàm tự sinh chuỗi Signature SHA-256 để gửi trả hoặc test dữ liệu
        private static string CreateMessageSHA256(string secretKey, string msgPart)
        {
            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                // Ghép chuỗi theo đúng quy tắc tài liệu: msgPart + "|" + secretKey
                string input = msgPart + "|" + secretKey;

                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                // Chuyển đổi byte array sang chuỗi Hex viết thường (%02x)
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }


    }
}
