using ReproBot.BotLogic.Calling;
using Microsoft.Bot.Builder.Calling;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace ReproBot.Bot.Controllers
{
    [BotAuthentication]
    [RoutePrefix("api/calling")]
    public class CallingController : ApiController
    {
        public CallingController() : base()
        {
            CallingConversation.RegisterCallingBot(callingBotService => new IVRBot(callingBotService));
        }

        [Route("callback")]
        public async Task<HttpResponseMessage> ProcessCallingEventAsync()
        {
            Trace.TraceInformation(DateTime.Now + " Callback");
            return await CallingConversation.SendAsync(this.Request, CallRequestType.CallingEvent);
        }

        [Route("call")]
        public async Task<HttpResponseMessage> ProcessIncomingCallAsync()
        {
            Trace.TraceInformation(DateTime.Now  +" Call");
            return await CallingConversation.SendAsync(this.Request, CallRequestType.IncomingCall);
        }
    }
}