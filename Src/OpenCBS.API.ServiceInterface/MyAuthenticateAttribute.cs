﻿using System;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.ServiceInterface;

namespace OpenCBS.API.ServiceInterface
{
    /// <summary>
    /// Indicates that the request dto, which is associated with this attribute,
    /// requires authentication.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class MyAuthenticateAttribute : RequestFilterAttribute
    {
        /// <summary>
        /// Restrict authentication to a specific <see cref="IAuthProvider"/>.
        /// For example, if this attribute should only permit access
        /// if the user is authenticated with <see cref="BasicAuthProvider"/>,
        /// you should set this property to <see cref="BasicAuthProvider.Name"/>.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Redirect the client to a specific URL if authentication failed.
        /// If this property is null, simply `401 Unauthorized` is returned.
        /// </summary>
        public string HtmlRedirect { get; set; }

        public MyAuthenticateAttribute(ApplyTo applyTo)
            : base(applyTo)
        {
            this.Priority = (int)RequestFilterPriority.Authenticate;
        }

        public MyAuthenticateAttribute()
            : this(ApplyTo.All) { }

        public MyAuthenticateAttribute(string provider)
            : this(ApplyTo.All)
        {
            this.Provider = provider;
        }

        public MyAuthenticateAttribute(ApplyTo applyTo, string provider)
            : this(applyTo)
        {
            this.Provider = provider;
        }

        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            if (AuthService.AuthProviders == null) throw new InvalidOperationException("The AuthService must be initialized by calling "
                 + "AuthService.Init to use an authenticate attribute");

            var matchingOAuthConfigs = AuthService.AuthProviders.Where(x =>
                this.Provider.IsNullOrEmpty()
                || x.Provider == this.Provider).ToList();

            if (matchingOAuthConfigs.Count == 0)
            {
                res.WriteError(req, requestDto, "No OAuth Configs found matching {0} provider"
                    .Fmt(this.Provider ?? "any"));
                //res.EndServiceStackRequest();
                res.Close();
                return;
            }
            /*
            if (matchingOAuthConfigs.Any(x => x.Provider == DigestAuthProvider.Name))
                AuthenticateIfDigestAuth(req, res);

            if (matchingOAuthConfigs.Any(x => x.Provider == BasicAuthProvider.Name))
                AuthenticateIfBasicAuth(req, res);
            */
            var session = req.GetSession();

            if (session == null )//|| !matchingOAuthConfigs.Any(x => session.IsAuthorized(x.Provider)))
            {
                //if (this.DoHtmlRedirectIfConfigured(req, res, true)) return;

                AuthProvider.HandleFailedAuth(matchingOAuthConfigs[0], null, req, res);
            }

            if (matchingOAuthConfigs.Any(x => x.Provider == "custom"))
            {
                if (session.IsAuthenticated && session.UserName != null)
                    return;
                else
                    AuthProvider.HandleFailedAuth(matchingOAuthConfigs[0], session, req, res);
            }
        }

        protected bool DoHtmlRedirectIfConfigured(IHttpRequest req, IHttpResponse res, bool includeRedirectParam = false)
        {
            var htmlRedirect = this.HtmlRedirect ?? AuthService.HtmlRedirect;
            if (htmlRedirect != null && req.ResponseContentType.MatchesContentType(ContentType.Html))
            {
                var url = req.ResolveAbsoluteUrl(htmlRedirect);
                if (includeRedirectParam)
                {
                    url = url.AddQueryParam("redirect", req.AbsoluteUri);
                }

                res.RedirectToUrl(url);
                return true;
            }

            return false;
        }
        /*
        public static void AuthenticateIfBasicAuth(IHttpRequest req, IHttpResponse res)
        {
            //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)			
            SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

            var userPass = req.GetBasicAuthUserAndPassword();
            if (userPass != null)
            {
                var authService = req.TryResolve<AuthService>();
                authService.RequestContext = new HttpRequestContext(req, res, null);
                var response = authService.Post(new Auth.Auth
                {
                    provider = BasicAuthProvider.Name,
                    UserName = userPass.Value.Key,
                    Password = userPass.Value.Value
                });
            }
        }
        public static void AuthenticateIfDigestAuth(IHttpRequest req, IHttpResponse res)
        {
            //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)			
            SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

            var digestAuth = req.GetDigestAuth();
            if (digestAuth != null)
            {
                var authService = req.TryResolve<AuthService>();
                authService.RequestContext = new HttpRequestContext(req, res, null);
                var response = authService.Post(new Auth.Auth
                {
                    provider = DigestAuthProvider.Name,
                    nonce = digestAuth["nonce"],
                    uri = digestAuth["uri"],
                    response = digestAuth["response"],
                    qop = digestAuth["qop"],
                    nc = digestAuth["nc"],
                    cnonce = digestAuth["cnonce"],
                    UserName = digestAuth["username"]
                });
            }
        }
         * */
    }
}