using System.Threading.Tasks;

namespace ReproBot.BotLogic.API.Managers.Interfaces
{
    public interface ICalculationApi
    {
        Task<Models.Response.Calculation_v2> CallCalculations_v2Api(Calculation_v2Form extractedCalculationForm);
    }
}
