using ReproBot.BotLogic.API.Managers.Interfaces;
using ReproBot.BotLogic.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ReproBot.BotLogic.API.Managers
{
    [Serializable]
    public class CalculationApi : ICalculationApi
    {
        public CalculationApi()
        {
        }

        public Task<Models.Response.Calculation_v2> CallCalculations_v2Api(Calculation_v2Form extractedCalculationForm)
        {
            //Mocked out this code
            var result = new Models.Response.Calculation_v2()
            {
                PaymentAmount = 12345
            };
            return Task.FromResult(result);
        }
    }
}
