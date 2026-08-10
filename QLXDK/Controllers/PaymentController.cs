using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using QLXDK.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;
using System.Web;
using QLXDK.Models.Views;
using Microsoft.AspNet.SignalR;
using System.Security.Cryptography;
using System.Net.Http.Headers; // Thêm thư viện này để cấu hình Header

namespace QLXDK.Controllers
{
    [System.Web.Mvc.Authorize]
    public class PaymentController : Controller
    {
        private qlslContext _db = new qlslContext();
        private static readonly HttpClient client = new HttpClient();
        // GET: Payment
        public ActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public ActionResult Callback()
        {
            string orderCode = Request["order_code"];
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<PaymentHub>();
            //hubContext.Clients.Group(orderCode).onPaymentSuccess();

            string messageError = Request["message_error"];
            string status = Request["status"];
            int userId = Convert.ToInt32(Session["UserId"]);
            
            if (status == "3")
            {
                // Update
                var item = _db.Orders.SingleOrDefault(p => p.OrderCode == orderCode);
                if (item == null) return HttpNotFound();

                item.PaymentDate = DateTime.Now;
                item.Status = status;
                item.QrCode = null;
                _db.SaveChanges();
                // Get userId from Order
                userId = item.UserID;
                List<OrderVM> orders = GetRecentOrdersByUserId(userId);

                var successResult = new
                {
                    IsSuccess = true,
                    OrderCode = orderCode,
                    Data = orders
                };
                hubContext.Clients.Group(orderCode).onPaymentSuccess(successResult);

            } else {
                var failResult = new
                {
                    IsSuccess = false,
                    OrderCode = orderCode,
                    Message = "Giao dịch thất bại"
                };
                hubContext.Clients.Group(orderCode).onPaymentSuccess(failResult);
            }
            
            var response = new
            {
                result_code = "0000",
                result_message = "OK"
            };
            //return Json(response, JsonRequestBehavior.AllowGet);
            return Json(response);
        }

        public ActionResult Return()
        {
            return View();
        }

        //public ActionResult Cancel()
        //{
        //    return View();
        //}

        public List<QLXDK.Models.Views.OrderVM> GetRecentOrdersByUserId(int userId)
        {
            DateTime aDayAgo = DateTime.Now.AddDays(-1);

            return _db.Orders
                .Where(p => p.UserID == userId && p.CreatedDate >= aDayAgo)
                .Select(p => new OrderVM
                {
                    ID = p.ID,
                    OrderCode = p.OrderCode,
                    CreatedDate = p.CreatedDate,
                    Status = p.Status,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    QrCode = p.QrCode
                })
                .OrderByDescending(p => p.CreatedDate)
                .ToList();
        }
        // POST: Payment/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Payment/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Payment/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Payment/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Payment/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public static string CalculateNganLuongChecksum(
            string merchantSiteCode, string orderCode, string orderDescription,
            string amount, string currency, string buyerFullname, string buyerEmail,
            string buyerMobile, string buyerAddress, string returnUrl, string cancelUrl,
            string notifyUrl, string language, string merchantPasscode)
        {
            string rawString = $"{merchantSiteCode}|{orderCode}|{orderDescription}|{amount}|{currency}|{buyerFullname}|{buyerEmail}|{buyerMobile}|{buyerAddress}|{returnUrl}|{cancelUrl}|{notifyUrl}|{language}|{merchantPasscode}";

            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(rawString);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        [HttpPost]
        public async Task<JsonResult> CreateQrCode(string orderCode, string amount)
        {

            string apiUrl = ConfigurationManager.AppSettings["apiUrl"];
            string merchantSiteCode = ConfigurationManager.AppSettings["merchantSiteCode"];
            string orderDescription = "Thanh toan";

            string currency = ConfigurationManager.AppSettings["currency"];
            string buyerFullname = ConfigurationManager.AppSettings["buyerFullname"];
            string buyerEmail = ConfigurationManager.AppSettings["buyerEmail"];
            string buyerMobile = ConfigurationManager.AppSettings["buyerMobile"];
            string buyerAddress = ConfigurationManager.AppSettings["buyerAddress"];
            string returnUrl = ConfigurationManager.AppSettings["returnUrl"];
            string cancelUrl = ConfigurationManager.AppSettings["cancelUrl"];
            string notifyUrl = ConfigurationManager.AppSettings["notifyUrl"];
            string language = ConfigurationManager.AppSettings["language"];
            string version = ConfigurationManager.AppSettings["version"];
            string paymentMethodCode = ConfigurationManager.AppSettings["paymentMethodCode"];
            string bankCode = ConfigurationManager.AppSettings["bankCode"];
            string merchantPasscode = ConfigurationManager.AppSettings["merchantPasscode"];

            string checksum = CalculateNganLuongChecksum(
                merchantSiteCode, orderCode, orderDescription, amount, currency,
                buyerFullname, buyerEmail, buyerMobile, buyerAddress,
                returnUrl, cancelUrl, notifyUrl, language, merchantPasscode
            );

            var payload = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("function", "CreateOrder"),
                    new KeyValuePair<string, string>("merchant_site_code", merchantSiteCode),
                    new KeyValuePair<string, string>("order_code", orderCode),
                    new KeyValuePair<string, string>("order_description", orderDescription),
                    new KeyValuePair<string, string>("amount", amount),
                    new KeyValuePair<string, string>("currency", currency),
                    new KeyValuePair<string, string>("buyer_fullname", buyerFullname),
                    new KeyValuePair<string, string>("buyer_email", buyerEmail),
                    new KeyValuePair<string, string>("buyer_mobile", buyerMobile),
                    new KeyValuePair<string, string>("buyer_address", buyerAddress),
                    new KeyValuePair<string, string>("return_url", returnUrl),
                    new KeyValuePair<string, string>("cancel_url", cancelUrl),
                    new KeyValuePair<string, string>("notify_url", notifyUrl),
                    new KeyValuePair<string, string>("language", language),
                    new KeyValuePair<string, string>("version", version),
                    new KeyValuePair<string, string>("payment_method_code", paymentMethodCode),
                    new KeyValuePair<string, string>("bank_code", bankCode),
                    new KeyValuePair<string, string>("checksum", checksum)
                };
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpContent content = new FormUrlEncodedContent(payload);
                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                    string checkoutUrl = "";
                    string token = "";
                    int userId = Convert.ToInt32(Session["UserId"]);

                    // 
                    if (response.IsSuccessStatusCode)
                    {
                        string responseResult = await response.Content.ReadAsStringAsync();
                        JObject data = JObject.Parse(responseResult);
                        // Check error
                        if (data["result_code"]?.ToString() != "0000")
                        {
                            return new JsonResult
                            {
                                Data = new { success = false, message = messageError(data["result_code"]?.ToString()) }
                            };
                        }

                        if (data["result_code"]?.ToString() == "0000")
                        {
                            checkoutUrl = data["result_data"]?["checkout_url"]?.ToString();
                            token = data["result_data"]?["token_code"]?.ToString();

                            var newItem = new Models.Entities.Order
                            {
                                UserID = userId,
                                OrderCode = orderCode,
                                CreatedDate = DateTime.Now,
                                Amount = Convert.ToInt32(amount),
                                Status = "1",
                                Token = token,
                                //CreatedDate = DateTime.Now,

                            };

                            _db.Orders.Add(newItem);
                            _db.SaveChanges();

                        }
                    }

                    if (!string.IsNullOrWhiteSpace(checkoutUrl))
                    {
                        HttpResponseMessage res = await client.GetAsync(checkoutUrl);
                        if (res.IsSuccessStatusCode)
                        {
                            string qrResult = await res.Content.ReadAsStringAsync();
                            JObject qrParsed = JObject.Parse(qrResult);
                            if (qrParsed["result_code"]?.ToString() == "0000")
                            {
                                string dataQr = qrParsed["result_data"]?["data_qr"]?.ToString();
                                // Update QrCode
                                var item = _db.Orders.SingleOrDefault(p => p.OrderCode == orderCode);
                                item.QrCode = dataQr;
                                _db.SaveChanges();

                                List<OrderVM> orders = GetRecentOrdersByUserId(userId);
                                return new JsonResult
                                {
                                    Data = new { success = true, qrdata = "data:image/png;base64," + dataQr, data = orders }
                                };
                            }
                        }
                    }
                } // ./ Using

                return new JsonResult
                {
                    Data = new { success = false, message = "Đã có lỗi xảy ra" }
                    //JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            catch (Exception ex)
            {
                return new JsonResult
                {
                    Data = new { success = false, message = "Đã có lỗi xảy ra" }
                };
            }
        }

        public string messageError(string errorCode)
        {
            string message = "";
            switch (errorCode)
            {
                case "0001":
                    message = "Lỗi không xác định";
                    break;
                case "0102":
                    message = "Hủy giao dịch thất bại. Trạng thái đơn hàng không hợp lệ!";
                    break;
                case "0101":
                    message = "Dữ liệu truyền lên đúng, nhưng không thể tạo đơn hàng cho merchant này";
                    break;
                default:
                    message = "Thông tin không hợp lệ";
                    break;
            }
            return message;
        }

        [HttpPost]
        public async Task<JsonResult> Cancel(string orderCode)
        {

            string apiUrl = ConfigurationManager.AppSettings["cancelApiUrl"];
            string merchantSiteCode = ConfigurationManager.AppSettings["merchantSiteCode"];
           
            string merchantPasscode = ConfigurationManager.AppSettings["merchantPasscode"];
            var item = _db.Orders.SingleOrDefault(p => p.OrderCode == orderCode);
            long amount = item.Amount;
            string tokenCode = item.Token;
            string checksum = checksumCancel(
                merchantSiteCode, tokenCode, orderCode,  merchantPasscode
            );

            var payload = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("function", "CancelPayment"),
                    new KeyValuePair<string, string>("merchant_site_code", merchantSiteCode),
                    new KeyValuePair<string, string>("token_code", tokenCode),
                    new KeyValuePair<string, string>("order_code", orderCode),
                    new KeyValuePair<string, string>("checksum", checksum)
                };
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpContent content = new FormUrlEncodedContent(payload);
                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                    int userId = Convert.ToInt32(Session["UserId"]);

                    // 
                    if (response.IsSuccessStatusCode)
                    {
                        string responseResult = await response.Content.ReadAsStringAsync();
                        JObject data = JObject.Parse(responseResult);
                        // Check error
                        if (data["result_code"]?.ToString() != "0000")
                        {
                            return new JsonResult
                            {
                                Data = new { success = false, message = messageError(data["result_code"]?.ToString()) }
                            };
                        }

                        if (data["result_code"]?.ToString() == "0000")
                        {
                            item.QrCode = null;
                            item.Status = "4";
                            _db.SaveChanges();

                            List<OrderVM> orders = GetRecentOrdersByUserId(userId);

                            return new JsonResult
                            {
                                Data = new { success = true, message = "Huỷ thành công", data = orders, orderCode = orderCode }
                            };

                        }
                    }

                } // ./ Using

                return new JsonResult
                {
                    Data = new { success = false, message = "Đã có lỗi xảy ra" }
                    //JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            catch (Exception ex)
            {
                return new JsonResult
                {
                    Data = new { success = false, message = "Đã có lỗi xảy ra" }
                };
            }
        }

        public static string checksumCancel(string merchantSiteCode, string tokenCode, string orderCode, string merchantPasscode)
        {
            string rawData = $"{merchantSiteCode} {tokenCode} {orderCode} {merchantPasscode}";
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        [HttpPost]
        public async Task<JsonResult> GenQrCode(string orderCode, string amount)
        {
            // 1. Ép hệ thống dùng TLS 1.2
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                delegate (object sender,
                          System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                          System.Security.Cryptography.X509Certificates.X509Chain chain,
                          System.Net.Security.SslPolicyErrors sslPolicyErrors)
                {
                    return true;
                };


            int userId = Convert.ToInt32(Session["UserId"]);
            string apiUrl = "https://7b21ec42-4016-43ab-b5c0-8b56802c953e.mock.pstmn.io";
            string appId = "SGLGT"; 
            string secretKey = "viETCOMBAnkCangSaiGon@159";

            // 1. Tạo nhanh dữ liệu động
            string messageId = Guid.NewGuid().ToString("N").Substring(0, 18).ToUpper();
            string messageTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            //string refMessageId = Guid.NewGuid().ToString();
            string refMessageId = "";
            string accessToken = "";
            // Access Token 
            string cacheKey = $"UserToken_{userId}";

            // 1. Kiểm tra xem trong Cache của User này đã có token chưa
            string cachedToken = HttpRuntime.Cache[cacheKey] as string;

            if (!string.IsNullOrEmpty(cachedToken))
            {
                // Nếu còn token và chưa hết hạn -> Trả về sử dụng luôn
                accessToken = cachedToken;
            } else
            {
                accessToken = await GetAccessToken();
            }


            // Giả lập chuỗi ký (bạn tự thay đổi theo quy tắc ghép của ngân hàng)
            string msgPart = appId + messageId + refMessageId + messageTime;
            string mySignature = CreateSignatureSHA256(secretKey, msgPart);
            //using (MD5 md5 = MD5.Create())
            //{
            //    byte[] inputBytes = Encoding.UTF8.GetBytes(msgPart);
            //    byte[] hashBytes = md5.ComputeHash(inputBytes);
            //    StringBuilder sb = new StringBuilder();
            //    for (int i = 0; i < hashBytes.Length; i++)
            //    {
            //        sb.Append(hashBytes[i].ToString("x2")); // "x2" đảm bảo chữ thường, giống chuỗi mã mẫu
            //    }
            //    mySignature = sb.ToString();
            //}

            // 2. Định nghĩa cấu trúc JSON trực tiếp bằng Anonymous Type
            var requestBody = new
            {
                context = new
                {
                    appId = appId,
                    messageId = messageId,
                    //refMessageId = refMessageId,
                    messageTime = messageTime,
                    routing = new { serviceCode = "CANGSAIGON_GENQR" }
                },
                payload = new
                {
                    MID = "P03HN2CHA024E10",
                    qrType = "06",
                    country = "VN",
                    billNumber = orderCode,
                    customerId = orderCode,
                    amount = new { amount = amount, currency = "704" },
                    masterCode = "970436",
                    purpose = "Topup TEST"
                },
                signature = mySignature
            };

            try
            {
                // 3. Serialize trực tiếp ra chuỗi JSON và gửi đi
                string jsonString = JsonConvert.SerializeObject(requestBody);
                var httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Authorization = null;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);


                HttpResponseMessage response = await client.PostAsync(apiUrl, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    //string jsonResponse = await response.Content.ReadAsStringAsync();

                    // 4. Đọc dữ liệu Response bằng kiểu dynamic (không cần tạo Class hứng)
                    //dynamic result = JsonConvert.DeserializeObject(jsonResponse);
                    string qrResult = await response.Content.ReadAsStringAsync();
                    JObject qrParsed = JObject.Parse(qrResult);
               
                        string dataQr = qrParsed["payload"]?["data"]?.ToString();

                        List<OrderVM> orders = GetRecentOrdersByUserId(userId);
                    //string dataQr = result?.data;

                    var newItem = new Models.Entities.Order
                    {
                        UserID = userId,
                        OrderCode = orderCode,
                        CreatedDate = DateTime.Now,
                        Amount = Convert.ToInt32(amount),
                        Status = "1",
                        CustomerCode = orderCode,
                        QrCode = dataQr
                        //CustomerName = null,
                        //CompanyName = null
                        //Token = token,

                    };

                    _db.Orders.Add(newItem);
                    _db.SaveChanges();


                    return new JsonResult
                    {
                        Data = new { success = true, qrdata = "data:image/png;base64," + dataQr, data = orders }
                    };

                    // Ví dụ lấy trực tiếp thông tin từ JSON phản hồi
                    //ViewBag.ResponseCode = result?.payload?.responseCode ?? "Không có mã lỗi";
                    //ViewBag.ServerSignature = result?.signature;
                }

                return new JsonResult
                {
                    Data = new { success = false, message = "Đã có lỗi xảy ra" }
                    //JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            catch (Exception ex)
            {
                return new JsonResult
                {
                    Data = new { success = false, message = "Đã có lỗi xảy ra" }
                };
            }


        } // ./ GenQrCode

        [HttpPost]
        public async Task<String> GetAccessToken()
        {
            // 1. Ép hệ thống dùng TLS 1.2
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            // 2. BỎ QUA KIỂM TRA CHỨNG CHỈ SSL (Thêm dòng này để xử lý triệt để lỗi Secure Channel)
            // Lệnh này ép mọi chứng chỉ SSL dù lỗi hay không hợp lệ đều được bỏ qua để kết nối thành công
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                delegate (object sender,
                          System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                          System.Security.Cryptography.X509Certificates.X509Chain chain,
                          System.Net.Security.SslPolicyErrors sslPolicyErrors)
                {
                    return true; // Luôn luôn trả về true để cho phép kết nối
                };



            string apiUrl = "https://7b21ec42-4016-43ab-b5c0-8b56802c953e.mock.pstmn.io";
            string clientId = "";
            string clientSecret = "";
            string grantType = "";

            

            // 2. Định nghĩa cấu trúc JSON trực tiếp bằng Anonymous Type
            var requestBody = new
            {

                payload = new
                {
                    client_id = clientId,
                    cilent_secret = clientSecret,
                    grant_type = grantType
                }
            };

            try
            {
                // 3. Serialize trực tiếp ra chuỗi JSON và gửi đi
                string jsonString = JsonConvert.SerializeObject(requestBody);
                var httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(apiUrl, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    //string jsonResponse = await response.Content.ReadAsStringAsync();

                    // 4. Đọc dữ liệu Response bằng kiểu dynamic (không cần tạo Class hứng)
                    //dynamic result = JsonConvert.DeserializeObject(jsonResponse);

                    //List<OrderVM> orders = GetRecentOrdersByUserId(18);
                    //string accessToken = result?.signature;
                    string tokenResult = await response.Content.ReadAsStringAsync();
                    JObject tokenParsed = JObject.Parse(tokenResult);

                    
                    int userId = Convert.ToInt32(Session["UserId"]);
                    string cacheKey = $"UserToken_{userId}";
                    int expiresIn = (int)tokenParsed["expires_in"];
                    string accessToken = tokenParsed["access_token"]?.ToString();
                    DateTime cacheExpiryTime = DateTime.Now.AddSeconds(expiresIn - 60);

                    // Nạp vào bộ nhớ RAM của Server
                    System.Web.HttpRuntime.Cache.Insert(
                        cacheKey,
                        accessToken,
                        null,
                        cacheExpiryTime,
                        System.Web.Caching.Cache.NoSlidingExpiration
                    );

                    return accessToken;

                    // Ví dụ lấy trực tiếp thông tin từ JSON phản hồi
                    //ViewBag.ResponseCode = result?.payload?.responseCode ?? "Không có mã lỗi";
                    //ViewBag.ServerSignature = result?.signature;
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }


        } // ./ GenQrCode

        [HttpPost]
        public async Task<JsonResult> InquiryTransaction(string orderCode, string amount)
        {
            // 1. Ép hệ thống dùng TLS 1.2
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                delegate (object sender,
                          System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                          System.Security.Cryptography.X509Certificates.X509Chain chain,
                          System.Net.Security.SslPolicyErrors sslPolicyErrors)
                {
                    return true;
                };


            int userId = Convert.ToInt32(Session["UserId"]);
            string apiUrl = "https://7b21ec42-4016-43ab-b5c0-8b56802c953e.mock.pstmn.io";
            string appId = "SGLGT";
            string secretKey = "viETCOMBAnkCangSaiGon@159";

            // 1. Tạo nhanh dữ liệu động
            string messageId = Guid.NewGuid().ToString("N").Substring(0, 18).ToUpper();
            string messageTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            //string refMessageId = Guid.NewGuid().ToString();
            string refMessageId = "";
            string accessToken = "";
            // Access Token 
            string cacheKey = $"UserToken_{userId}";

            // 1. Kiểm tra xem trong Cache của User này đã có token chưa
            string cachedToken = HttpRuntime.Cache[cacheKey] as string;

            if (!string.IsNullOrEmpty(cachedToken))
            {
                // Nếu còn token và chưa hết hạn -> Trả về sử dụng luôn
                accessToken = cachedToken;
            }
            else
            {
                accessToken = await GetAccessToken();
            }


            // Giả lập chuỗi ký (bạn tự thay đổi theo quy tắc ghép của ngân hàng)
            string msgPart = appId + messageId + refMessageId + messageTime;
            string mySignature = CreateSignatureSHA256(secretKey, msgPart);
            //using (MD5 md5 = MD5.Create())
            //{
            //    byte[] inputBytes = Encoding.UTF8.GetBytes(msgPart);
            //    byte[] hashBytes = md5.ComputeHash(inputBytes);
            //    StringBuilder sb = new StringBuilder();
            //    for (int i = 0; i < hashBytes.Length; i++)
            //    {
            //        sb.Append(hashBytes[i].ToString("x2")); // "x2" đảm bảo chữ thường, giống chuỗi mã mẫu
            //    }
            //    mySignature = sb.ToString();
            //}

            // 2. Định nghĩa cấu trúc JSON trực tiếp bằng Anonymous Type
            var requestBody = new
            {
                context = new
                {
                    appId = appId,
                    messageId = messageId,
                    //refMessageId = refMessageId,
                    messageTime = messageTime,
                    routing = new { serviceCode = "CANGSAIGON_GENQR" }
                },
                payload = new
                {
                   payerCode = "", // Mã khách hàng thanh toán
                   datedateFrom = "2023-07-31",
                   dateTo = "2023-07-31"
                },
                signature = mySignature
            };

            try
            {
                // 3. Serialize trực tiếp ra chuỗi JSON và gửi đi
                string jsonString = JsonConvert.SerializeObject(requestBody);
                var httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Authorization = null;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);


                HttpResponseMessage response = await client.PostAsync(apiUrl, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    //string jsonResponse = await response.Content.ReadAsStringAsync();

                    // 4. Đọc dữ liệu Response bằng kiểu dynamic (không cần tạo Class hứng)
                    //dynamic result = JsonConvert.DeserializeObject(jsonResponse);
                    string qrResult = await response.Content.ReadAsStringAsync();
                    JObject qrParsed = JObject.Parse(qrResult);

                    string dataQr = qrParsed["payload"]?["data"]?.ToString();

                    List<OrderVM> orders = GetRecentOrdersByUserId(userId);
                    //string dataQr = result?.data;

                    var newItem = new Models.Entities.Order
                    {
                        UserID = userId,
                        OrderCode = orderCode,
                        CreatedDate = DateTime.Now,
                        Amount = Convert.ToInt32(amount),
                        Status = "1",
                        CustomerCode = orderCode,
                        QrCode = dataQr,
                        //CustomerName = null,
                        //CompanyName = null
                        //Token = token,
                        //CreatedDate = DateTime.Now,

                    };

                    _db.Orders.Add(newItem);
                    _db.SaveChanges();


                    return new JsonResult
                    {
                        Data = new { success = true, qrdata = "data:image/png;base64," + dataQr, data = orders }
                    };

                    // Ví dụ lấy trực tiếp thông tin từ JSON phản hồi
                    //ViewBag.ResponseCode = result?.payload?.responseCode ?? "Không có mã lỗi";
                    //ViewBag.ServerSignature = result?.signature;
                }

                return new JsonResult
                {
                    Data = new { success = false, message = "Đã có lỗi xảy ra" }
                    //JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
            catch (Exception ex)
            {
                return new JsonResult
                {
                    Data = new { success = false, message = "Đã có lỗi xảy ra" }
                };
            }


        } // ./ GenQrCode

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

        private static string CreateSignatureSHA256(string secretKey, string msgPart)
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
