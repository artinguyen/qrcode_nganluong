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
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;


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
                    errorCode = 3;
                    errorMessage = "Invalid signature";
                    return CreateFailureResponse(Request, channelId, channelRefNumber, errorCode, errorMessage, requestDateTime, responseMsgId, secretKey, msgRes);
                }

                var order = _db.Orders.FirstOrDefault(o => o.CustomerCode == customerCode);

                if (order == null)
                {
                    errorCode = 400;
                    errorMessage = "No data found";
                    return CreateFailureResponse(Request, channelId, channelRefNumber, errorCode, errorMessage, requestDateTime, responseMsgId, secretKey, msgRes);
                }

                if (order.Status == "3")
                {
                    errorCode = 1;
                    errorMessage = "Customer paid";
                    return CreateFailureResponse(Request, channelId, channelRefNumber, errorCode, errorMessage, requestDateTime, responseMsgId, secretKey, msgRes);
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
                    errorCode = 3;
                    errorMessage = "Invalid signature";
                    return CreateFailureResponse(Request, channelId, channelRefNumber, errorCode, errorMessage, requestDateTime, responseMsgId, secretKey, msgRes);
                    //return Request.CreateResponse(HttpStatusCode.BadRequest, new { error = 3, error_description = "Invalid signature" });
                }

                var order = _db.Orders.FirstOrDefault(o => o.CustomerCode == customerCode);

                if (order == null)
                {
                    errorCode = 400;
                    errorMessage = "No data found";
                    return CreateFailureResponse(Request, channelId, channelRefNumber, errorCode, errorMessage, requestDateTime, responseMsgId, secretKey, msgRes);
                }

                if (order.Amount != long.Parse(amount))
                {
                    errorCode = 2;
                    errorMessage = "Invalid amount";
                    return CreateFailureResponse(Request, channelId, channelRefNumber, errorCode, errorMessage, requestDateTime, responseMsgId, secretKey, msgRes);
                }

                // Save transactionRefNo  
                order.TransactionRefNo = transactionRefNo;
                _db.SaveChanges();

                Task.Run(async () =>
                {
                    try
                    {
                        using (var client = new HttpClient())
                        {
                            string webUrl = ConfigurationManager.AppSettings["webUrl"];
                            var content = new FormUrlEncodedContent(new[] {
                            new KeyValuePair<string, string>("order_code", customerCode),
                            new KeyValuePair<string, string>("status", "3")
                        });
                            await client.PostAsync(webUrl, content);
                        }
                    }
                    catch
                    {
                        // Luôn giữ try-catch trống hoặc ghi log để bảo vệ luồng nền
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
                return Request.CreateResponse(HttpStatusCode.InternalServerError, new { error = "500", error_description = ex.Message });
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

        private HttpResponseMessage CreateFailureResponse(
            HttpRequestMessage request,
            string channelId,
            string channelRefNumber,
            int errorCode,
            string errorMessage,
            string requestDateTime,
            string responseMsgId,
            string secretKey,
            string msgRes)
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
    }
}
