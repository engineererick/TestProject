using Intuit.Ipp.OAuth2PlatformClient;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MvcCodeFlowClientManual.Controllers
{
    public class CallbackController : Controller
    {
        /*
            Request the auth code(code) and Quickbooks account ID (realmId) and await
            the token to authorize the callback
         */
        public async Task<ActionResult> Index()
        {
            var state = Request.QueryString["state"];
            if (state.Equals(AppController.auth2Client.CSRFToken, StringComparison.Ordinal))
                ViewBag.State = state + " (valid)";
            else ViewBag.State = state + " (invalid)";

            string code = Request.QueryString["code"] ?? "none";
            string realmId = Request.QueryString["realmId"] ?? "none";
            await GetAuthTokensAsync(code, realmId);

            ViewBag.Error = Request.QueryString["error"] ?? "none";

            return RedirectToAction("Tokens", "App");
        }

        /*
            Stored the realmId and code in Claims to be used in the current session.
            Also refresh tokens time to avoid the end of the session.
         */
        private async Task GetAuthTokensAsync(string code, string realmId)
        {
            if (realmId != null) Session["realmId"] = realmId;

            Request.GetOwinContext().Authentication.SignOut("TempState");
            var tokenResponse = await AppController.auth2Client.GetBearerTokenAsync(code);

            var claims = new List<Claim>();

            if (Session["realmId"] != null) claims.Add(new Claim("realmId", Session["realmId"].ToString()));
            
            if (!string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
                claims.Add(new Claim("access_token", tokenResponse.AccessToken));
                claims.Add(new Claim("access_token_expires_at", (DateTime.Now.AddSeconds(tokenResponse.AccessTokenExpiresIn)).ToString()));
            

            if (!string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
                claims.Add(new Claim("refresh_token", tokenResponse.RefreshToken));
                claims.Add(new Claim("refresh_token_expires_at", (DateTime.Now.AddSeconds(tokenResponse.RefreshTokenExpiresIn)).ToString()));
            

            var id = new ClaimsIdentity(claims, "Cookies");
            Request.GetOwinContext().Authentication.SignIn(id);
        }
    }
}