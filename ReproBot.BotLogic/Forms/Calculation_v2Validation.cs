using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReproBot.BotLogic.Forms
{
    public static class Calculation_v2Validation
    {
        public static ValidateResult ValidatePaymentAmount(Calculation_v2Form state, object value)
        {
            var result = new ValidateResult { IsValid = true, Value = value };

            var matches = Regex.Matches((string)value, @"(-?\d+)");
            if (matches.Count > 0)
            {
                var values = matches.Cast<Group>().Select(t => t.Value);
                var paymentAmount = String.Join("", values);
                var parsedNumber = Double.Parse(paymentAmount);
                if (NumberIsPositive(parsedNumber))
                {
                    result.Value = paymentAmount;
                }
                else
                {
                    result.Feedback = "The amount financed must be a positive number.";
                    result.IsValid = false;
                }
            }
            else
            {
                result.Feedback = "The amount financed must be a positive number.";
                result.IsValid = false;
            }

            return result;
        }

        public static ValidateResult ValidateInterestRate(Calculation_v2Form state, object value)
        {
            var result = new ValidateResult { IsValid = true, Value = value };

            var match = Regex.Match((string)value, @"([-+]?[0-9]*\.?[0-9]+)");
            if (match.Success)
            {
                if (NumberIsPositive(Double.Parse(match.Groups[1].Value)))
                {
                    result.Value = match.Groups[1].Value;
                }
                else
                {
                    result.Feedback = "The interest rate must be a positive number.";
                    result.IsValid = false;
                }
            }
            else
            {
                result.Feedback = "The interest rate must be a positive number.";
                result.IsValid = false;
            }

            return result;
        }

        internal static ValidateResult ValidateNumberOfMonths(Calculation_v2Form state, object value)
        {
            var result = new ValidateResult { IsValid = true, Value = value };
            var valueAsString = (string)value;
            var match = Regex.Match(valueAsString, @"-?\d+");
            if (match.Success)
            {
                int durationNumber = Int32.Parse(match.Value);
                if (NumberIsPositive(durationNumber))
                {
                    int inMonths;
                    if (valueAsString.Contains("year"))
                    {
                        inMonths = (durationNumber * 12);
                    }
                    else
                    {
                        inMonths = durationNumber;
                    }
                    if (inMonths <= 120)
                    {
                        result.Value = inMonths.ToString();
                    }
                    else
                    {
                        result.Feedback = "The number of months must be smaller than 120.";
                        result.IsValid = false;
                    }
                }
                else
                {
                    result.Feedback = "The number of months must be a positive number.";
                    result.IsValid = false;
                }
            }
            else
            {
                result.Feedback = "The number of months must be a positive number.";
                result.IsValid = false;
            }

            return result;
        }

        private static bool NumberIsPositive(double value)
        {
            return value >= 0;
        }
        private static bool NumberIsPositive(int value)
        {
            return value >= 0;
        }
    }
}
