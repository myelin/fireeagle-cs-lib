/**
 * FireEagle OAuth+API C# bindings
 *
 * Copyright (C) 2007-08 Yahoo! Inc
 *
 */

/// <summary>
/// Summary description for FireEagle
/// </summary>
using System.Diagnostics;
using System.Configuration;
using OAuth;
using System.Collections.Specialized;
using System;
using System.Net;
using System.Text;
using System.IO;
using LitJson;

namespace Yahoo.FireEagle
{

    public class FireEagle : OAuthBase
    {
        public static string FE_ROOT = "http://fireeagle.yahoo.net/";
        public static string FE_API_ROOT = "https://fireeagle.yahooapis.com/";

        // Set these to your FireEagle consumer token and secret
        public static string CONSUMER_KEY = null,
            CONSUMER_SECRET = null;

        private string oauth_token = null,
            oauth_token_secret = null;

        private bool is_request_token = false;

        /// <summary>
        /// Constructor for 
        /// </summary>
        public FireEagle()
        {
            Setup();
        }

        /// <summary>
        /// Constructor for when you have a request or access token.
        /// </summary>
        /// <param name="oauth_token"></param>
        /// <param name="oauth_secret"></param>
        /// <param name="is_request_token"></param>
        public FireEagle(string oauth_token, string oauth_token_secret)
        {
            this.oauth_token = oauth_token;
            this.oauth_token_secret = oauth_token_secret;
            Setup();
        }

        private void Setup()
        {
            // Try to fetch consumer key + secret from web.config if not given
            if (CONSUMER_KEY == null) CONSUMER_KEY = ConfigurationManager.AppSettings["FEConsumerKey"];
            if (CONSUMER_SECRET == null) CONSUMER_SECRET = ConfigurationManager.AppSettings["FEConsumerSecret"];
            Debug.Assert(CONSUMER_KEY != null, "CONSUMER_KEY not set; you can put it the <appSettings> block in Web.Config as FEConsumerKey");
            Debug.Assert(CONSUMER_SECRET != null, "CONSUMER_SECRET not set; you can put it the <appSettings> block in Web.Config as FEConsumerSecret");
        }

        // Obtain an oauth request token and secret from FireEagle
        public void GetRequestToken()
        {
            NameValueCollection token = CallOauth("GET", FE_API_ROOT + "oauth/request_token", null);
            if (token["oauth_token"] == null) throw new Exception("Missing oauth_token value in /oauth/request_token response from Fire Eagle");
            if (token["oauth_token_secret"] == null) throw new Exception("Missing oauth_token_secret value in /oauth/request_token response from Fire Eagle");
            this.oauth_token = token["oauth_token"];
            this.oauth_token_secret = token["oauth_token_secret"];
            is_request_token = true;
        }

        public string OauthToken { get { return this.oauth_token; } }
        public string OauthTokenSecret { get { return this.oauth_token_secret; } }

        // After calling GetRequestToken, this returns the URL to redirect to
        public string AuthorizeUrl
        {
            get
            {
                Debug.Assert(is_request_token);
                return FE_ROOT + "oauth/authorize?oauth_token=" + oauth_token;
            }
        }

        // Exchange an oauth request token and secret for an access token with FireEagle
        public void GetAccessToken()
        {
            NameValueCollection token = CallOauth("GET", FE_API_ROOT + "oauth/access_token", null);
            if (token["oauth_token"] == null) throw new Exception("Missing oauth_token value in /oauth/access_token response from Fire Eagle");
            if (token["oauth_token_secret"] == null) throw new Exception("Missing oauth_token_secret value in /oauth/access_token response from Fire Eagle");
            this.oauth_token = token["oauth_token"];
            this.oauth_token_secret = token["oauth_token_secret"];
            is_request_token = false;
        }

        private NameValueCollection CallOauth(string method, string base_url, NameValueCollection args)
        {
            string text = CallInternal(method, base_url, args);
            NameValueCollection resp = new NameValueCollection();
            foreach (string pair in text.Split(new char[] { '&' }))
            {
                string[] split_pair = pair.Split(new char[] { '=' }, 2);
                resp.Add(split_pair[0], split_pair[1]);
            }
            return resp;
        }

        private JsonData CallJson(string method, string base_url, NameValueCollection args)
        {
            string raw_data = CallInternal(method, base_url, args);
            JsonData data = JsonMapper.ToObject(raw_data);
            return data;
        }

        // Generic API method caller
        public JsonData Call(string api_method, NameValueCollection args)
        {
            string http_method = (api_method == "user") ? "GET" : "POST";
            string base_url = FE_API_ROOT + "api/0.1/" + api_method + ".json";
            return CallJson(http_method, base_url, args);
        }

        // Wrapper for the 'where am I' method: "user"
        public JsonData user()
        {
            return Call("user", null);
        }

        // Wrapper for the 'set my location' method: "update"
        public JsonData update(NameValueCollection where)
        {
            return Call("update", where);
        }

        // Wrapper for the 'lookup location' method: "lookup"
        public JsonData lookup(NameValueCollection where)
        {
            return Call("lookup", where);
        }

        private string CallInternal(string method, string base_url, NameValueCollection args)
        {
            string url = base_url;
            if (args != null)
            {
                string data = "";
                foreach (string k in args)
                {
                    data += (data == "" ? "" : "&") + UrlEncode(k) + "=" + UrlEncode(args[k]);
                    if (data != "") url += (base_url.IndexOf("?") == -1 ? "?" : "&") + data;
                }
            }
            Uri uri = new Uri(url);
            string ts = GenerateTimeStamp(),
                nonce = GenerateNonce();
            string full_url = GenerateSignature(uri, CONSUMER_KEY, CONSUMER_SECRET, oauth_token, oauth_token_secret, method, ts, nonce, OAuthBase.SignatureTypes.HMACSHA1);

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(method == "POST" ? normalizedUrl : full_url);
            req.Method = method;
            req.ContentType = "application/x-www-urlencoded";
            try
            {
                if (method == "POST")
                {
                    byte[] post_data = Encoding.UTF8.GetBytes(normalizedRequestParameters);
                    req.ContentLength = post_data.Length;

                    Stream post_stream = req.GetRequestStream();
                    post_stream.Write(post_data, 0, post_data.Length);
                    post_stream.Close();
                }

                string raw_data = "";

                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                StreamReader resp_stream = new StreamReader(resp.GetResponseStream());
                raw_data = resp_stream.ReadToEnd();
                resp.Close();

                return raw_data;
            }
            catch (WebException e)
            {
                throw new Exception(String.Format("Error communicating with widget server: {0}", e.Message), e);
            }
        }

    }

}