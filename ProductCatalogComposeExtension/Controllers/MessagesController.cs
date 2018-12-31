using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Bot.Connector.Teams.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using ThoughtStuff.ProductCatalogComposeExtension.Models;

namespace ThoughtStuff.ProductCatalogComposeExtension.Controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {

        private ConnectorClient connectorClient;


        /// <summary>
        /// POST: api/Messages
        /// receive a message from a user and send replies
        /// </summary>
        /// <param name="activity">BF Activity.</param>
        /// <returns>HTTP response.</returns>
        public virtual async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            ConnectorClient connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl));
            return HandleIncomingRequest(activity, connectorClient);
        }

        /// <summary>
        /// Handles incoming Bot Framework messages.
        /// </summary>
        /// <param name="activity">Incoming request from Bot Framework.</param>
        /// <param name="connectorClient">Connector client instance for posting to Bot Framework.</param>
        /// <returns>HTTP response message.</returns>
        public static HttpResponseMessage HandleIncomingRequest(Activity activity, ConnectorClient connectorClient)
        {
            switch (activity.GetActivityType())
            {
                case ActivityTypes.Invoke:
                    return HandleInvoke(activity, connectorClient);
                case ActivityTypes.Message:
                case ActivityTypes.ConversationUpdate:
                case ActivityTypes.ContactRelationUpdate:
                case ActivityTypes.Typing:
                case ActivityTypes.DeleteUserData:
                case ActivityTypes.Ping:
                default:
                    break;
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        /// <summary>
        /// Handles invoke messages.
        /// </summary>
        /// <param name="activity">Incoming request from Bot Framework.</param>
        /// <param name="connectorClient">Connector client instance for posting to Bot Framework.</param>
        /// <returns>Task tracking operation.</returns>
        private static HttpResponseMessage HandleInvoke(Activity activity, ConnectorClient connectorClient)
        {
            // Check if the Activity if of type compose extension.
            if (activity.IsComposeExtensionQuery())
            {
                return HandleComposeExtensionQuery(activity, connectorClient);
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        /// <summary>
        /// Handles compose extension queries.
        /// </summary>
        /// <param name="activity">Incoming request from Bot Framework.</param>
        /// <param name="connectorClient">Connector client instance for posting to Bot Framework.</param>
        /// <returns>Task tracking operation.</returns>
        private static HttpResponseMessage HandleComposeExtensionQuery(Activity activity, ConnectorClient connectorClient)
        {
            // Get Compose extension query data.
            ComposeExtensionQuery composeExtensionQuery = activity.GetComposeExtensionQueryData();

            var searchTerm = composeExtensionQuery.Parameters.Single(p => p.Name == "Location").Value.ToString();

            var catalog = Products.Instance;
            var results = catalog.ProductCatalog.Where(p => p.SKU.StartsWith(searchTerm)).ToList();
            ComposeExtensionResponse response;
            if (results.Count > 0)
            {
                var attachments = new List<ComposeExtensionAttachment>();
                int i = 1;
                foreach (var item in results)
                {
                    var result = new HeroCard
                    {
                        Buttons = new List<CardAction>
                            {
                                new CardAction
                                {
                                        Type = ActionTypes.OpenUrl,
                                        Title = "Show in CMS",
                                        Value = "https://www.bing.com"
                                },
                            },
                        Title = $"{item.Name} - {item.SKU}",
                        Subtitle = "Item #" + item.SKU,
                        Text = item.NumberInStock + " in stock.",
                        Images = new List<CardImage>() { new CardImage()
                        {
                            Url = "https://placekitten.com/420/320?image=" + i
                        } }
                    };
                    attachments.Add(result.ToAttachment().ToComposeExtensionAttachment());
                    i++;
                }



                response = new ComposeExtensionResponse
                {
                    ComposeExtension = new ComposeExtensionResult
                    {
                        Attachments = attachments,
                        Type = "result",
                        AttachmentLayout = "list"
                    }
                };
            }
            else
            {
                // Process data and return the response.
                response = new ComposeExtensionResponse
                {
                    ComposeExtension = new ComposeExtensionResult
                    {
                        Attachments = new List<ComposeExtensionAttachment>
                        {
                            new HeroCard
                            {
                                Title = "No results..."
                            }.ToAttachment().ToComposeExtensionAttachment()
                        },
                        Type = "result",
                        AttachmentLayout = "list"
                    }
                };
            }


            StringContent stringContent = new StringContent(JsonConvert.SerializeObject(response));
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            httpResponseMessage.Content = stringContent;
            return httpResponseMessage;
        }
    }
}
