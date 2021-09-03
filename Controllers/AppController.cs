using Intuit.Ipp.OAuth2PlatformClient;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Net;
using Intuit.Ipp.Core;
using Intuit.Ipp.Data;
using Intuit.Ipp.QueryFilter;
using Intuit.Ipp.Security;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace MvcCodeFlowClientManual.Controllers
{
    public class AppController : Controller
    {
        /*
            Initialization of auth2 object and strings.
            Static because it will never change in the current Session
         */
        public static string clientid = ConfigurationManager.AppSettings["clientid"];
        public static string clientsecret = ConfigurationManager.AppSettings["clientsecret"];
        public static string redirectUrl = ConfigurationManager.AppSettings["redirectUrl"];
        public static string environment = ConfigurationManager.AppSettings["appEnvironment"];

        public static OAuth2Client auth2Client = new OAuth2Client(clientid, clientsecret, redirectUrl, environment);

        
        public ActionResult Index()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Session.Clear();
            Session.Abandon();
            Request.GetOwinContext().Authentication.SignOut("Cookies");
            return View();
        }

        /*
            An authorization mode is needed to get the connection to Quickbooks account
         */
        public ActionResult InitiateAuth()
        {
            List<OidcScopes> scopes = new List<OidcScopes>();
            scopes.Add(OidcScopes.Accounting);
            var auth = auth2Client.GetAuthorizationURL(scopes);
            return Redirect(auth);
        }

        /*
            The apps can connect to Quickbooks API once the token is received.
         */
        public ActionResult ApiCallService()
        {
            if (Session["realmId"] != null)
            {
                string realmId = Session["realmId"].ToString();
                try
                {
                    /*
                        A ServiceContext is created to request the API, also an access token and realmId are created
                     */
                    var principal = User as ClaimsPrincipal;
                    OAuth2RequestValidator oauthValidator = new OAuth2RequestValidator(principal.FindFirst("access_token").Value);

                    ServiceContext serviceContext = new ServiceContext(realmId, IntuitServicesType.QBO, oauthValidator);
                    serviceContext.IppConfiguration.MinorVersion.Qbo = "23";

                    //According to the requested, the Customer entitie is called from the API and receive all the customers
                    //into a List of customers
                    QueryService<Customer> querySvc = new QueryService<Customer>(serviceContext);
                    List<Customer> companyInfo = querySvc.ExecuteIdsQuery("SELECT * FROM Customer").ToList();

                    return View("ApiCallService", companyInfo);
                }
                catch (Exception ex)
                {
                    return View("ApiCallService", null);
                }
            }
            else
                return View("ApiCallService", null);
        }

        public ActionResult Error()
        {
            return View("Error");
        }

        
        public ActionResult Tokens()
        {
            return View("Tokens");
        }
    }
}