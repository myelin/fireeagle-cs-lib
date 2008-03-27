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
using System.Collections;
using System.Xml.Serialization;

namespace Yahoo.FireEagle.API
{
    public class Location
    {
        [XmlAttributeAttribute("best-guess")]
        public bool IsBestGuess;

        [XmlElementAttribute("id")]
        public string Id;

        //TODO: parse (use a property which can be set with a string)
        [XmlElementAttribute("point", Namespace = "http://www.georss.org/georss")]
        public string Point;

        //TODO: parse (use a property which can be set with a string)
        [XmlElementAttribute("box", Namespace = "http://www.georss.org/georss")]
        public string Box;

        [XmlElementAttribute("label")]
        public string Label;

        [XmlElementAttribute("level")]
        public int Level;

        [XmlElementAttribute("level-name")]
        public string LevelName;

        [XmlElementAttribute("located-at")]
        public string LocatedAt;

        [XmlElementAttribute("name")]
        public string Name;

        [XmlElementAttribute("place-id")]
        public string PlaceId;

        [XmlElementAttribute("woeid")]
        public string WoeId;
    }

    public class LocationHierarchy
    {
        [XmlElementAttribute("location")]
        public Location[] Locations;
    }

    public class User
    {
        [XmlElementAttribute("location-hierarchy")]
        public LocationHierarchy LocationHierarchy;
    }

    [XmlRootAttribute("rsp")]
    public class Response
    {
        [XmlElementAttribute("user")]
        public User User;
    }
}

namespace Yahoo.FireEagle
{

    public class FireEagleException : Exception
    {
        public FireEagleException(string msg)
            : base(msg)
        {
        }

        public FireEagleException(string msg, Exception innerException)
            : base(msg, innerException)
        {
        }
    }

    public class FireEagle : OAuthBase
    {
        public static string FE_ROOT = "http://fireeagle.yahoo.net/";
        public static string FE_API_ROOT = "https://fireeagle.yahooapis.com/";

        // Set these to your FireEagle consumer token and secret
        public static string CONSUMER_KEY = null,
            CONSUMER_SECRET = null;

        private string oauthToken = null,
            oauthTokenSecret = null;

        private bool isRequestToken = false;

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
        /// <param name="oauthToken"></param>
        /// <param name="oauth_secret"></param>
        /// <param name="is_request_token"></param>
        public FireEagle(string oauthToken, string oauthTokenSecret)
        {
            this.oauthToken = oauthToken;
            this.oauthTokenSecret = oauthTokenSecret;
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
            this.oauthToken = token["oauth_token"];
            this.oauthTokenSecret = token["oauth_token_secret"];
            isRequestToken = true;
        }

        public string OauthToken { get { return this.oauthToken; } }
        public string OauthTokenSecret { get { return this.oauthTokenSecret; } }

        // After calling GetRequestToken, this returns the URL to redirect to
        public string AuthorizeUrl
        {
            get
            {
                Debug.Assert(isRequestToken);
                return FE_ROOT + "oauth/authorize?oauth_token=" + oauthToken;
            }
        }

        // Exchange an oauth request token and secret for an access token with FireEagle
        public void GetAccessToken()
        {
            NameValueCollection token = CallOauth("GET", FE_API_ROOT + "oauth/access_token", null);
            if (token["oauth_token"] == null) throw new Exception("Missing oauth_token value in /oauth/access_token response from Fire Eagle");
            if (token["oauth_token_secret"] == null) throw new Exception("Missing oauth_token_secret value in /oauth/access_token response from Fire Eagle");
            this.oauthToken = token["oauth_token"];
            this.oauthTokenSecret = token["oauth_token_secret"];
            isRequestToken = false;
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

        // Generic API method caller
        public JsonData CallJson(string api_method, NameValueCollection args)
        {
            string http_method = (api_method == "user") ? "GET" : "POST";
            string base_url = FE_API_ROOT + "api/0.1/" + api_method + ".json";
            string raw_data = CallInternal(http_method, base_url, args);
            JsonData data = JsonMapper.ToObject(raw_data);
            return data;
        }

        // Wrapper for the 'where am I' method: "user"
        public API.User user()
        {
            string xml = CallInternal("GET", FE_API_ROOT + "api/0.1/user.xml", null);
            XmlSerializer xs = new XmlSerializer(typeof(API.Response));
            API.Response rsp = (API.Response)xs.Deserialize(new StringReader(xml));
            return rsp.User;
        }

        // Wrapper for the 'set my location' method: "update"
        public JsonData update(NameValueCollection where)
        {
            return CallJson("update", where);
        }

        // Wrapper for the 'lookup location' method: "lookup"
        public JsonData lookup(NameValueCollection where)
        {
            return CallJson("lookup", where);
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
                }
                if (data != "") url += (base_url.IndexOf("?") == -1 ? "?" : "&") + data;
            }
            Uri uri = new Uri(url);
            string ts = GenerateTimeStamp(),
                nonce = GenerateNonce();
            string full_url = GenerateSignature(uri, CONSUMER_KEY, CONSUMER_SECRET, oauthToken, oauthTokenSecret, method, ts, nonce, OAuthBase.SignatureTypes.HMACSHA1);

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(method == "POST" ? normalizedUrl : full_url);
            req.Method = method;
            req.ContentType = "application/x-www-form-urlencoded";
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
                try
                {
                    StreamReader resp_stream = new StreamReader(e.Response.GetResponseStream());
                    string raw_data = resp_stream.ReadToEnd();
                    if (e.Response.ContentType.StartsWith("application/json"))
                    {
                        JsonData err = JsonMapper.ToObject(raw_data);
                        JsonData rsp = err["rsp"];
                        JsonData message = rsp["message"];
                        throw new FireEagleException(String.Format("Error from Fire Eagle: {0}", (string)message));
                    }
                }
                catch (FireEagleException fe_e) { throw fe_e; }
                catch (Exception failure_e) { } // if anything fails above, just fall through

                throw new FireEagleException(String.Format("Error communicating with Fire Eagle: {0}", e.Message), e);
            }
        }

    }

}