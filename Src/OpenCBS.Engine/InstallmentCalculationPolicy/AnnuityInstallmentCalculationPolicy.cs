﻿using System;
using System.ComponentModel.Composition;
using OpenCBS.Engine.Interfaces;

namespace OpenCBS.Engine.InstallmentCalculationPolicy
{
    [Export(typeof(IPolicy))]
    [PolicyAttribute(Implementation = "Annuity")]
    public class AnnuityInstallmentCalculationPolicy : BaseInstallmentCalculationPolicy, IInstallmentCalculationPolicy
    {
        public void Calculate(IInstallment installment, IScheduleConfiguration configuration)
        {
            var annuity = configuration.RoundingPolicy.Round(FindAnnuity(configuration));

            installment.Interest = CalculateInterest(installment, configuration, installment.Olb);
            installment.Principal = annuity - installment.Interest;
        }

        /// <summary>
        /// Finding total for all Annuity policy (30/360, Fact/360, Fact/Fact)
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns>total annuity</returns>
        private decimal FindAnnuity(IScheduleConfiguration configuration)
        {
            var number = configuration.NumberOfInstallments - configuration.GracePeriod;
            var numberOfPeriods = configuration.PeriodPolicy.GetNumberOfPeriodsInYear(configuration.PreferredFirstInstallmentDate, configuration.YearPolicy);
            var interestRate = (double)configuration.InterestRate / 100 / numberOfPeriods;

            // at first we are trying to calculate standard annuity for 30/360 using standard formula.
            var numerator = (decimal)interestRate * configuration.Amount;
            var denominator = 1 - 1 / Math.Pow(1 + interestRate, number);
            var annuity = numerator / (decimal)denominator;
            // In order to define the annuity amount for the other types Period and Year policy
            // we need to increase the standard annuity, build schedule according defined amount
            // and if scheduled is not balanced, repeat the procedure again
            // remainder it is surplus amount, which is left after building schedule using calculated annuity
            // remainder = remainder/numberOfInstallemnts * interestRate/100
            // left remainder // should be divided by number of installments in order to proportionally spread between installments
            // but because we need to count interest also that amount should be multiplied by interest rate / 100
            var remainder = 0m;
            var counter = 0;
            var installment = new Installment {Olb = configuration.Amount};
            do
            {
                // loop is only for building schedule and determining the remainder
                for (var i = 1; i <= configuration.NumberOfInstallments; ++i)
                {
                    installment.Number = i;
                    installment.StartDate = i != 1 ? installment.EndDate : configuration.StartDate;
                    installment.EndDate = i != 1
                        ? configuration.PeriodPolicy.GetNextDate(installment.StartDate)
                        : configuration.PreferredFirstInstallmentDate;
                    if (i <= configuration.GracePeriod) continue;
                    installment.Interest = CalculateInterest(installment, configuration, installment.Olb);
                    installment.Principal = annuity - installment.Interest;
                    installment.Olb -= installment.Principal;
                }
                remainder = installment.Olb;
                installment.Olb = configuration.Amount;
                ++counter;
                annuity += (remainder * configuration.InterestRate / 100 / number);
            } while (Math.Abs(remainder) > 0.01m && counter < 1000);
            return annuity;
        }
    }
}
