using FinancialSummaryApi.V1.Boundary.Response;
using FinancialSummaryApi.V1.Factories;
using FinancialSummaryApi.V1.Gateways.Abstracts;
using FinancialSummaryApi.V1.UseCase.Interfaces;
using System;
using System.Threading.Tasks;

namespace FinancialSummaryApi.V1.UseCase
{
    public class GetRentGroupSummaryByNameUseCase : IGetRentGroupSummaryByNameUseCase
    {
        private readonly IFinanceSummaryGateway _gateway;

        public GetRentGroupSummaryByNameUseCase(IFinanceSummaryGateway gateway)
        {
            _gateway = gateway;
        }

        public async Task<RentGroupSummaryResponse> ExecuteAsync(string rentGroupName, DateTime submitDate)
        {
            if (submitDate == DateTime.MinValue)
            {
                submitDate = DateTime.UtcNow;
            }
            var rentGroupDomain = await _gateway.GetRentGroupSummaryByNameAsync(rentGroupName, submitDate).ConfigureAwait(false);

            return rentGroupDomain?.ToResponse();
        }
    }
}
