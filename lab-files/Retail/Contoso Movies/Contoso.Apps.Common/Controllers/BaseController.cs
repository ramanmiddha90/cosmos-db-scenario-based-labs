﻿using Contoso.Apps.Common;
using Contoso.Apps.Movies.Data.Models;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Contoso.Apps.Common.Controllers
{
    [System.Web.Mvc.Authorize]
    public class BaseController : Controller
    {
        protected DocumentClient client;
        protected Database database;
        protected string databaseId;
        protected DocumentCollection productColl, shoppingCartItems;

        static protected IQueryable<Item> items;
        static protected IQueryable<Category> categories;

        protected IQueryable<Order> orders;
        protected IQueryable<Movies.Data.Models.User> users;

        protected static readonly FeedOptions DefaultOptions = new FeedOptions { EnableCrossPartitionQuery = true };

        public BaseController()
        {
            string endpointUrl = ConfigurationManager.AppSettings["dbConnectionUrl"];
            string authorizationKey = ConfigurationManager.AppSettings["dbConnectionKey"];
            databaseId = ConfigurationManager.AppSettings["databaseId"];

            client = new DocumentClient(new Uri(endpointUrl), authorizationKey, new ConnectionPolicy { ConnectionMode = ConnectionMode.Gateway, ConnectionProtocol = Protocol.Https });
            database = client.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseId }).Result;

            Uri productCollectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, "item");

            if (items == null)
                items = client.CreateDocumentQuery<Item>(productCollectionUri, "SELECT * FROM item", DefaultOptions);

            Uri catCollectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, "category");

            if (categories == null)
                categories = client.CreateDocumentQuery<Category>(catCollectionUri, "SELECT * FROM category", DefaultOptions);

            Uri userCollectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, "user");
            users = client.CreateDocumentQuery<Movies.Data.Models.User>(userCollectionUri, "SELECT * FROM user", DefaultOptions);
        }

        public ActionResult SetUser(int userId)
        {
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, "user");
            var query = client.CreateDocumentQuery<Movies.Data.Models.User>(collectionUri, new SqlQuerySpec()
            {
                QueryText = "SELECT * FROM user f WHERE f.UserId = @id",
                Parameters = new SqlParameterCollection()
                    {
                        new SqlParameter("@id", userId)
                    }
            }, DefaultOptions);

            Movies.Data.Models.User user = query.ToList().FirstOrDefault();
            Session["User"] = user;

            this.HttpContext.User = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(user.Email), new string[] { /* fill roles if any */ });

            return RedirectToAction("Index", "Home");
        }

        public ActionResult NewUser(string email)
        {
            Movies.Data.Models.User user = new Movies.Data.Models.User();
            user.Email = email;

            Session["User"] = user;

            this.HttpContext.User = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(email), new string[] { /* fill roles if any */ });

            return RedirectToAction("Index", "Home");
        }

        public string DisplayName { get; set; }

        protected override void EndExecute(IAsyncResult asyncResult)
        {
            if (Request.IsAuthenticated)
            {
                var claimsIdentity = User.Identity as System.Security.Claims.ClaimsIdentity;
                if (claimsIdentity != null)
                {
                    string displayNameClaim = "http://schemas.microsoft.com/identity/claims/displayname";
                    var claim = claimsIdentity.FindFirst(displayNameClaim);
                    if (claim != null)
                    {
                        DisplayName = claim.Value;
                    }
                }
                //DisplayName = ((System.Security.Claims.ClaimsIdentity)User.Identity).FindFirst("http://schemas.microsoft.com/identity/claims/displayname").Value;
            }
            base.EndExecute(asyncResult);
        }
    }
}