using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Data.Entity.Validation;
using System.Data.Entity.Infrastructure;

namespace QLXDK.Helpers
{
    public class Logger
    {
        public static void LogException(Exception ex)
        {
            try
            {
                string logFolder = HttpContext.Current.Server.MapPath("~/App_Data/Logs");

                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                string filePath = Path.Combine(logFolder, $"Log_{DateTime.Now:yyyy_MM_dd}.txt");

                string logMessage = $"=========================================================================\n" +
                                    $"Time: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
                                    $"Message: {ex.Message}\n" +
                                    $"(StackTrace):\n{ex.StackTrace}\n";

                if (ex.InnerException != null)
                {
                    logMessage += $"(InnerException): {ex.InnerException.Message}\n" +
                                  $"Detail:\n{ex.InnerException.StackTrace}\n";
                }

                //object dbUpdateEx = null;
                var dbUpdateEx = ex as DbUpdateException;
                if (dbUpdateEx != null)
                {
                    logMessage += "--- DATABASE (DbUpdateException) ---\n";

                    Exception inner = dbUpdateEx.InnerException;
                    while (inner != null)
                    {
                        logMessage += $"Detail: {inner.Message}\n";
                        inner = inner.InnerException;
                    }
                }

                logMessage += "=========================================================================\n\n";
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine(logMessage);
                }
            }
            catch
            {

            }
        }
    }
}