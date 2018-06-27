using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using ReproBot.BotLogic.Forms;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;

public enum PaymentFrequency
{
    [Terms("monthly", "month")]
    [EnumMember(Value = "monthly")]
    Monthly = 1,
    [Terms("quarterly", "quarter")]
    [EnumMember(Value = "quarterly")]
    Quarterly,
    [Terms("semiannually", "semi annually", "semi-annually", "semiannual", "semi annual", "semi annually", "half year")]
    [EnumMember(Value = "semi-annually")]
    SemiAnnually,
    [Terms("annually", "annual", "yearly")]
    [EnumMember(Value = "annually")]
    Annually
};

// For more information about this template visit http://aka.ms/azurebots-csharp-form
[Serializable]
public class Calculation_v2Form
{
    [Prompt("What payment frequency would you like? {||}")]
    public PaymentFrequency PaymentFrequency { get; set; }

    [Describe("finance amount")]
    [Prompt("What is the amount financed?")]
    public string SalePrice { get; set; }

    [Prompt("What is the interest rate?")]
    public string InterestRate { get; set; }
    
    [Prompt("Can you please specify the duration in number of months?")]
    public string NumberOfMonths { get; set; }

    public static IForm<Calculation_v2Form> BuildForm()
    {
        return new FormBuilder<Calculation_v2Form>()
            .Field(new FieldReflector<Calculation_v2Form>(nameof(SalePrice))
                .SetType(typeof(string))
                .SetValidate(async (state, value) =>
                {
                    Trace.TraceInformation(DateTime.Now+ " Verifying SalePrice: " + value.ToString());
                    return Calculation_v2Validation.ValidatePaymentAmount(state, value);
                }))
            .Field(new FieldReflector<Calculation_v2Form>(nameof(InterestRate))
                .SetType(typeof(string))
                .SetValidate(async (state, value) =>
                {
                    Trace.TraceInformation(DateTime.Now + " Verifying InterestRate: " + value.ToString());
                    return Calculation_v2Validation.ValidateInterestRate(state, value);
                }))
            .Field(new FieldReflector<Calculation_v2Form>(nameof(NumberOfMonths))
                .SetType(typeof(string))
                .SetValidate(async (state, value) =>
                {
                    Trace.TraceInformation(DateTime.Now + " Verifying NumberOfMonths: " + value.ToString());
                    return Calculation_v2Validation.ValidateNumberOfMonths(state, value);
                }))
            .Field(new FieldReflector<Calculation_v2Form>(nameof(PaymentFrequency))
                .SetType(null)
                .SetValidate(async (state, value) =>
                {
                    Trace.TraceInformation(DateTime.Now + " Verifying Payment Frequency " + value.ToString());
                    return new ValidateResult(){IsValid = true, Value = value};
                }))
            .Build();
    }
}