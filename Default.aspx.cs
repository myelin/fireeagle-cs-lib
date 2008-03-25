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

public partial class _Default : System.Web.UI.Page 
{
    public bool authorized;
    public JsonData location = null;

    private string BaseUrl
    {
        get
        {
            Uri url = Request.Url;
            string u = url.Scheme + "://" + url.Host;
            if (!((url.Scheme == "http" && url.Port == 80) &&
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
        FireEagle.FE_ROOT = FireEagle.FE_API_ROOT = "http://rails.colinux.tmp:3000/";

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
            location = fe.user();
        }
    }
}
