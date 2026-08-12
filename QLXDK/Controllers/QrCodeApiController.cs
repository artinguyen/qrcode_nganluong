using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using QLXDK.Models;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace QLXDK.Controllers
{
    [RoutePrefix("api/VCBPayment")]
    public class QrCodeApiController : ApiController
    {
        private qlslContext _db = new qlslContext();
        private static readonly HttpClient _httpClient = new HttpClient();
        [HttpGet]
        [Route("Inquiry")]
        public HttpResponseMessage InquiryGetFallback()
        {
            var errorResponse = new
            {
                code = "405",
                message = "Invalid method"
            };

            return Request.CreateResponse(System.Net.HttpStatusCode.MethodNotAllowed, errorResponse);
        }

        [HttpGet]
        [Route("Payment")]
        public HttpResponseMessage Payment()
        {
            var errorResponse = new
            {
                code = "405",
                message = "Invalid method"
            };

            return Request.CreateResponse(System.Net.HttpStatusCode.MethodNotAllowed, errorResponse);
        }

        [HttpGet]
        [Route("Health")]
        public IHttpActionResult GetStatus()
        {
            try
            {
                using (var context = new qlslContext())
                {
                    context.Database.ExecuteSqlCommand("SELECT 1");
                }
            }
            catch { }

            return Ok("Ok");
        }

        [HttpPost]
        [Route("Inquiry")]
        //HttpResponseMessage, async Task<HttpResponseMessage>
        public HttpResponseMessage Inquiry([FromBody] dynamic request)
        {
            try
            {
                var authResult = checkAuthen(Request);

                if (authResult != null)
                {
                    return authResult;
                }
                string channelId = request.context?.channelId;
                string channelRefNumber = request.context?.channelRefNumber;
                string requestDateTime = request.context?.requestDateTime;
                string customerCode = request.payload?.customerCode;
                string providerId = request.payload?.providerId;
                string serviceId = request.payload?.serviceId;
                string reqSignature = request.signature;

                string secretKey = ConfigurationManager.AppSettings["secretKey"];
                string custName = ConfigurationManager.AppSettings["custName"];

                string responseMsgId = Guid.NewGuid().ToString("N").Substring(0, 18);
                string msgReq = channelId + "|" + channelRefNumber;
                string msgRes = channelId + "|" + channelRefNumber + "|" + responseMsgId;
                string errorMessage = "";
                int errorCode = 0;

                // Check signature
                if (!VerifyMessageSHA256(secretKey, msgReq, reqSignature))
                {
                    errorCode = 18;
                    errorMessage = "Invalid signature";
                    return CreateFailureResponseInquiry(Request, channelId, channelRefNumber, errorCode, errorMessage, requestDateTime, responseMsgId, secretKey, msgRes, customerCode);
                }

                var order = _db.Orders.FirstOrDefault(o => o.CustomerCode == customerCode);

                if (order == null)
                {
                    errorCode = 17;
                    errorMessage = "Invalid user";
                    return CreateFailureResponseInquiry(Request, channelId, channelRefNumber, errorCode, errorMessage, requestDateTime, responseMsgId, secretKey, msgRes, customerCode);
                }

                if (order.Status == "3")
                {
                    errorCode = 1;
                    errorMessage = "Paid";
                    return CreateFailureResponseInquiry(Request, channelId, channelRefNumber, errorCode, errorMessage, requestDateTime, responseMsgId, secretKey, msgRes, customerCode);
                }

                var apiResponse = new
                {
                    context = new
                    {
                        channelId = channelId,
                        channelRefNumber = channelRefNumber,
                        errorCode = 0,
                        errorMessage = "",
                        requestDateTime = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"),
                        responseMsgId = new Random().Next(1000000, 9999999).ToString(),
                        status = "SUCCESS"
                    },
                    payload = new
                    {
                        customerCode = order.CustomerCode,
                        paymentSequence = "1",
                        bills = new[]
                        {
                            new
                            {
                                amount = order.Amount.ToString(),
                                billId = order.OrderCode ?? order.CustomerCode,
                                addnlFields = new[]
                                {
                                    new
                                    {
                                        fieldId = "CustName",
                                        fieldValue = custName
                                    }
                                }
                            }
                        }
                    },
                    signature = CreateMessageSHA256(secretKey, msgRes)
                };

                return Request.CreateResponse(HttpStatusCode.OK, apiResponse);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = "500", error_description = ex.Message });
            }
        }

        public String CreateSignatureMD5(string msgPart)
        {
            string mySignature = "";
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(msgPart);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                mySignature = sb.ToString();
            }
            return mySignature;
        }

        [HttpPost]
        [Route("Payment")]
        public HttpResponseMessage Payment([FromBody] dynamic request)
        {
            try
            {
                // 
                var authResult = checkAuthen(Request);

                if (authResult != null)
                {
                    return authResult;
                }

                string channelId = request.context?.channelId;
                string channelRefNumber = request.context?.channelRefNumber;
                string requestDateTime = request.context?.requestDateTime;

                string customerCode = request.payload?.customerCode;
                string providerId = request.payload?.providerId;
                string serviceId = request.payload?.serviceId;
                string billId = request.payload?.bills[0]?.billId;
                string amount = request.payload?.bills[0]?.amount;
                string transactionRefNo = request.payload?.internalTransactionRefNo;

                string secretKey = ConfigurationManager.AppSettings["secretKey"];
                string custName = ConfigurationManager.AppSettings["custName"];

                string responseMsgId = Guid.NewGuid().ToString("N").Substring(0, 18);
                string reqSignature = request.signature;

                string msgReq = channelId + "|" + channelRefNumber;
                string msgRes = channelId + "|" + channelRefNumber + "|" + responseMsgId;
                string errorMessage = "";
                int errorCode = 0;

                if (!VerifyMessageSHA256(secretKey, msgReq, reqSignature))
                {
                    errorCode = 18;
                    errorMessage = "Invalid signature";
                    return CreateFailureResponsePayment(Request, channelId, channelRefNumber, errorCode, errorMessage, requestDateTime, responseMsgId, secretKey, msgRes, providerId, serviceId);
                }

                var order = _db.Orders.FirstOrDefault(o => o.CustomerCode == customerCode);

                if (order == null)
                {
                    errorCode = 17;
                    errorMessage = "Invalid user";
                    return CreateFailureResponsePayment(Request, channelId, channelRefNumber, errorCode, errorMessage, requestDateTime, responseMsgId, secretKey, msgRes, providerId, serviceId);
                }

                if (order.Status == "3")
                {
                    errorCode = 1;
                    errorMessage = "Paid";
                    return CreateFailureResponsePayment(Request, channelId, channelRefNumber, errorCode, errorMessage, requestDateTime, responseMsgId, secretKey, msgRes, providerId, serviceId);
                }

                if (order.Amount != long.Parse(amount))
                {
                    errorCode = 3;
                    errorMessage = "Invalid amount";
                    return CreateFailureResponsePayment(Request, channelId, channelRefNumber, errorCode, errorMessage, requestDateTime, responseMsgId, secretKey, msgRes, providerId, serviceId);
                }

                // Save transactionRefNo  
                order.TransactionRefNo = transactionRefNo;
                _db.SaveChanges();

                System.Web.Hosting.HostingEnvironment.QueueBackgroundWorkItem(async cancellationToken =>
                {
                    try
                    {
                        string webUrl = ConfigurationManager.AppSettings["webUrl"];
                        var content = new FormUrlEncodedContent(new[] {
                            new KeyValuePair<string, string>("order_code", customerCode),
                            new KeyValuePair<string, string>("status", "3")
                        });

                        await _httpClient.PostAsync(webUrl, content, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        //System.Diagnostics.Debug.WriteLine($"Lỗi gửi thông báo: {ex.Message}");
                    }
                });

                var apiResponse = new
                {
                    context = new
                    {
                        channelId = channelId,
                        channelRefNumber = channelRefNumber,
                        errorCode = 0,
                        errorMessage = "",
                        requestDateTime = requestDateTime,
                        responseMsgId = responseMsgId,
                        status = "SUCCESS"
                    },
                    payload = new
                    {
                        providerId = providerId,
                        serviceId = serviceId,
                        bills = new[]
                            {
                                new
                                {
                                    amount = amount,
                                    billErrCode = "0",
                                    billErrDesc = "",
                                    billId = billId
                                }
                            }
                    },
                    signature = CreateMessageSHA256(secretKey, msgRes)
                };

                return Request.CreateResponse(HttpStatusCode.OK, apiResponse);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = "500", error_description = "Internal Server Error" });
            }
        }

        private static bool VerifyMessageSHA256(string secretKey, string msgPart, string signature)
        {
            if (string.IsNullOrEmpty(signature)) return false;

            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                string input = msgPart + "|" + secretKey;

                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                return sb.ToString().Equals(signature, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string CreateMessageSHA256(string secretKey, string msgPart)
        {
            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                string input = msgPart + "|" + secretKey;

                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        private HttpResponseMessage CreateFailureResponseInquiry(
            HttpRequestMessage request,
            string channelId,
            string channelRefNumber,
            int errorCode,
            string errorMessage,
            string requestDateTime,
            string responseMsgId,
            string secretKey,
            string msgRes,
            string customerCode
            )
        {
            // 1. Tạo object payload theo đúng cấu trúc của bạn
            var payload = new
            {
                context = new
                {
                    channelId = channelId,
                    channelRefNumber = channelRefNumber,
                    errorCode = errorCode,
                    errorMessage = errorMessage,
                    requestDateTime = requestDateTime,
                    responseMsgId = responseMsgId,
                    status = "FAILURE"
                },
                payload = new
                {
                    customerCode = customerCode,
                    bills = new object[] { }
                },
                signature = CreateMessageSHA256(secretKey, msgRes)
            };

            return request.CreateResponse(HttpStatusCode.OK, payload);
        }

        private HttpResponseMessage checkAuthen(HttpRequestMessage Request)
        {
            var authHeader = Request.Headers.Authorization;

            if (authHeader == null || !authHeader.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, new { error = "401", error_description = "Incorrect Header Authorization format" });
            }
            string credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authHeader.Parameter));
            string[] parts = credentials.Split(':');

            if (parts.Length != 2)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, new { error = "401", error_description = "Incorrect Authorization format" });
            }

            string username = parts[0];
            string password = parts[1];
            string validUsername = ConfigurationManager.AppSettings["userClient"];
            string validPassword = ConfigurationManager.AppSettings["passClient"];
            // 
            if (username != validUsername || password != validPassword)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, new { error = "401", error_description = "Invalid credentials" });
            }
            return null;
        }

        private HttpResponseMessage CreateFailureResponsePayment(
            HttpRequestMessage request,
            string channelId,
            string channelRefNumber,
            int errorCode,
            string errorMessage,
            string requestDateTime,
            string responseMsgId,
            string secretKey,
            string msgRes,
            string providerId,
            string serviceId
            )
            {
                // 1. Tạo object payload theo đúng cấu trúc của bạn
                var payload = new
                {
                    context = new
                    {
                        channelId = channelId,
                        channelRefNumber = channelRefNumber,
                        errorCode = errorCode,
                        errorMessage = errorMessage,
                        requestDateTime = requestDateTime,
                        responseMsgId = responseMsgId,
                        status = "FAILURE"
                    },
                    payload = new
                    {
                        providerId = providerId,
                        serviceId = serviceId,
                        bills = new object[] { }
                    },
                    signature = CreateMessageSHA256(secretKey, msgRes)
                };

                return request.CreateResponse(HttpStatusCode.OK, payload);
            }
    }
}
