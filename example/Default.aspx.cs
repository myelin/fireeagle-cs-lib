using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using Yahoo.FireEagle;
using LitJson;
using System.Collections.Specialized;

public partial class _Default : System.Web.UI.Page 
{
    // flags to pass back to Default.aspx
    public bool authorized;
    public JsonData location = null,
        lookup = null;

    // current url, minus any query params
    private string BaseUrl
    {
        get
        {
            Uri url = Request.Url;
            string u = url.Scheme + "://" + url.Host;
            if (!((url.Scheme == "http" && url.Port == 80) ||
                  (url.Scheme == "https" && url.Port == 443)))
            {
                u += ":" + url.Port;
            }
            u += url.AbsolutePath;
            return u;
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        // for FE devs: set FERoot and FEApiRoot appSettings in Web.Config to point at a local FireEagle install
        if (ConfigurationManager.AppSettings["FERoot"] != null)
            FireEagle.FE_ROOT = ConfigurationManager.AppSettings["FERoot"];
        if (ConfigurationManager.AppSettings["FEApiRoot"] != null)
            FireEagle.FE_API_ROOT = ConfigurationManager.AppSettings["FEApiRoot"];

        if (Request.QueryString["f"] == "start")
        {
            // get a request token + secret from FE and redirect to the authorization page
            FireEagle fe = new FireEagle();
            fe.GetRequestToken();
            Session["auth_state"] = "start";
            Session["request_token"] = fe.OauthToken;
            Session["request_secret"] = fe.OauthTokenSecret;
            Response.Redirect(fe.AuthorizeUrl);
            return;
        }

        if (Request.QueryString["f"] == "callback")
        {
            if ((string)Session["auth_state"] != "start")
            {
                Response.Redirect(BaseUrl);
                return;
            }
            if ((string)Session["request_token"] != Request.QueryString["oauth_token"]) throw new Exception("Request token mismatch");

            FireEagle fe = new FireEagle((string)Session["request_token"], (string)Session["request_secret"]);
            fe.GetAccessToken();
            Session["auth_state"] = "done";
            Session["access_token"] = fe.OauthToken;
            Session["access_secret"] = fe.OauthTokenSecret;
            Response.Redirect(BaseUrl);
            return;
        }

        authorized = ((string)Session["auth_state"] == "done");
        if (authorized)
        {
            FireEagle fe = new FireEagle((string)Session["access_token"], (string)Session["access_secret"]);

            if (Request.HttpMethod == "POST")
            {
                NameValueCollection where = new NameValueCollection();
                foreach (string k in Request.Form.Keys)
                {
                    switch (k)
                    {
                        case "lat":
                        case "lon":
                        case "q":
                        case "place_id":
                        case "woeid":
                            string v = Request.Form[k];
                            if (!string.IsNullOrEmpty(v)) where[k] = v;
                            break;
                    }
                }

                switch (Request.Form["submit"]) {
                    case "Move!":
                        fe.update(where);
                        Response.Redirect(BaseUrl);
                        return;
                    case "Lookup":
                        lookup = fe.lookup(where);
                        break;
                }
            }

            location = fe.user();
        }
    }
}
