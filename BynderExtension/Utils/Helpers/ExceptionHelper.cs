using System;

namespace Bynder.Utils.Helpers
{
    public static class ExceptionHelper
    {
        public static bool IsTooManyRequestsException(Exception ex)
        {
            // Check for WebException, HttpException, or custom Bynder exception with status code 429
            while (ex != null)
            {
                if (ex is System.Net.WebException webEx &&
                    webEx.Response is System.Net.HttpWebResponse response &&
                    (int)response.StatusCode == 429)
                {
                    return true;
                }
                // If you have a custom Bynder exception, check for 429 here as well
                ex = ex.InnerException;
            }
            return false;
        }

        public static bool Is500ServerErrorException(Exception ex)
        {
            while (ex != null)
            {
                if (ex is System.Net.WebException webEx &&
                    webEx.Response is System.Net.HttpWebResponse response)
                {
                    int statusCode = (int)response.StatusCode;
                    if (statusCode >= 500 && statusCode <= 599)
                    {
                        return true;
                    }
                }
                // Add checks for other exception types if needed
                ex = ex.InnerException;
            }
            return false;
        }

        
    }
}
