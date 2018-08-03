using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Web.Script.Serialization;

namespace Microsoft.Bot.Sample.QnABot
{
    [LuisModel("fb1a7a00-8d15-467b-96e6-bb264ce96ca0", "8f8d6831f3634a04b99c468d248536ee")]
    [Serializable]
    public class RootLuisDialog : LuisDialog<object>
    {
        private string CountryCode;

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, IAwaitable<IMessageActivity> message, LuisResult result)
        {

            var messageToForward = await message as Activity;
            await context.Forward(new Root, this.AfterQnA, messageToForward, CancellationToken.None);

            //context.Wait(this.MessageReceived);
        }
        [LuisIntent("Application.Access")]
        public async Task ApplicationAccess(IDialogContext context, LuisResult result)
        {
            string message = $"Let me assist you on 'adding users to the application'. ";
            await context.PostAsync(message);
            context.Call(new ApplicationAccess(), this.AfterApplicationAccess);
            //context.Wait(this.MessageReceived);
        }
        [LuisIntent("Inactive.Models")]
        public async Task InactiveModels(IDialogContext context, LuisResult result)
        {
            var cc = result.Entities != null ? result.Entities.FirstOrDefault(p => p.Type == "countryCode") : null;
            string countryCode = cc != null ? cc.Entity.ToUpper() : string.Empty;
            context.Call(new InactiveModels(countryCode), this.AfterApplicationAccess);

        }
        [LuisIntent("Slow.Vec")]
        public async Task SlowVec(IDialogContext context, LuisResult result)
        {
            var cc = result.Entities != null ? result.Entities.FirstOrDefault(p => p.Type == "countryCode") : null;
            string countryCode = cc != null ? cc.Entity.ToUpper() : string.Empty;

            if (cc.Resolution.ContainsKey("values"))
            {

                var json = new JavaScriptSerializer().Serialize(cc.Resolution);
                dynamic responseJObject = JObject.Parse(json);
                string tt = string.Empty;
                foreach (string s in responseJObject.values)
                {
                    tt += s;
                }
                countryCode = tt;
                //dynamic responseJObject = JObject.Parse(cc.Resolution.ToString());      
                //tmp.

            }

            CountryCode = countryCode;
            string message = $"Slow VEC may be due to too many items in model list which may be inactive or not used. Do you want me to find inactive models?";
            PromptDialog.Confirm(context, ResultHandler, message);


        }


        private async Task ResultHandler(IDialogContext context, IAwaitable<bool> result)
        {
            if (await result)
            {

                context.Call(new InactiveModels(CountryCode), this.AfterApplicationAccess);
            }
            else
            {
                await context.PostAsync("Fine then...");
                context.Done(String.Empty);
            }

        }
        private async Task AfterQnA(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            // wait for the next user message
            context.Wait(this.MessageReceived);
        }
        private async Task AfterApplicationAccess(IDialogContext context, IAwaitable<bool> result)
        {
            var success = await result;

            if (!success)
            {
                await context.PostAsync("Requested operation could not be completed. Please try again later.");
            }

            await this.StartAsync(context);
        }
    }
}