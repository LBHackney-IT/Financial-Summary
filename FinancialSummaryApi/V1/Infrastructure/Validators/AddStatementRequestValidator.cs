using FinancialSummaryApi.V1.Boundary.Request;
using FluentValidation;

namespace FinancialSummaryApi.V1.Infrastructure.Validators
{
    public class AddStatementRequestValidator : AbstractValidator<AddStatementRequest>
    {
        public AddStatementRequestValidator()
        {
            RuleFor(x => x.TargetId).NotEmpty();
            RuleFor(x => x.TargetType).IsInEnum();
            RuleFor(x => x.StatementPeriodEndDate).NotEmpty();
            RuleFor(x => x.RentAccountNumber).NotEmpty();
            RuleFor(x => x.Address).NotEmpty();
            RuleFor(x => x.StatementType).IsInEnum();
            RuleFor(x => x.ChargedAmount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.HousingBenefitAmount).GreaterThanOrEqualTo(0);
        }
    }
}
