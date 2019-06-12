using Bynder.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace Bynder.Api
{
    public class OAuthClient
    {
        protected OAuth.Manager OAuthManager;

        public void InitializeManager(string consumerKey, string consumerSecret, string token, string tokenSecret)
        {
            OAuthManager = new OAuth.Manager(consumerKey, consumerSecret, token, tokenSecret);
        }

        /// <summary>
        /// make a GET call
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public string Get(string uri)
        {
            var response = SendRequest(
                new HttpRequestMessage()
                {
                    RequestUri = new Uri(uri),
                    Method = HttpMethod.Get,
                }
            );
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().Result;
        }

        /// <summary>
        /// make a GET call with a number of retries
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="maxRetries"></param>
        /// <returns></returns>
        public string GetWithRetry(string uri, int maxRetries = 8)
        {
            int retryCount = 0;
            while (true)
            {
                var response = SendRequest(
                    new HttpRequestMessage()
                    {
                        RequestUri = new Uri(uri),
                        Method = HttpMethod.Get,
                    }
                );
                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
                if (retryCount >= maxRetries)
                {
                    response.EnsureSuccessStatusCode();
                    return null;
                }
                System.Threading.Thread.Sleep((2 ^ retryCount++) * 1000);
            }
        }

        /// <summary>
        /// make a POST using raw string body
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="postData"></param>
        /// <returns></returns>
        public string Post(string uri, string postData)
        {
            var response = SendRequest(
                new HttpRequestMessage()
                {
                    RequestUri = new Uri(uri),
                    Method = HttpMethod.Post,
                    Content = new StringContent(postData)
                }
            );
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().Result;
        }

        /// <summary>
        /// make a POST using formdata (key/value pairs) that are encoded using FormUrlEncodedContent
        /// with a application/x-www-form-urlencoded content-type
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="formData"></param>
        /// <returns></returns>
        public string Post(string uri, List<KeyValuePair<string, string>> formData)
        {
            var response = SendRequest(
                new HttpRequestMessage()
                {
                    RequestUri = new Uri(uri),
                    Method = HttpMethod.Post,
                    Content = new FormUrlEncodedContent(formData)
                }
            );
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().Result;
        }

        /// <summary>
        /// make a DELETE call
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public string Delete(string uri)
        {
            var response = SendRequest(
                new HttpRequestMessage()
                {
                    RequestUri = new Uri(uri),
                    Method = HttpMethod.Delete
                }
            );
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().Result;
        }

        /// <summary>
        /// sign and send the http request, return the response
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <returns></returns>
        protected HttpResponseMessage SendRequest(HttpRequestMessage requestMessage)
        {
            using (var httpClient = new HttpClient())
            {
                SignRequestMessage(requestMessage);
                return httpClient.SendAsync(requestMessage).Result;
            }
        }

        /// <summary>
        /// sign the request using Oauth
        /// </summary>
        /// <param name="message"></param>
        private void SignRequestMessage(HttpRequestMessage message)
        {
            var uri = message.RequestUri.ToString();
            if (message.Method.Equals(HttpMethod.Post) && message.Content.GetType() == typeof(FormUrlEncodedContent))
            {
                uri += "?" + message.Content.ReadAsStringAsync().Result;
            }

            if (OAuthManager == null) throw new Exception("OAuthManager is not initialized");
            message.Headers.Add(HttpRequestHeader.Authorization.ToString(), OAuthManager.GenerateAuthzHeader(uri, message.Method.Method));
        }
    }
}