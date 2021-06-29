using Amazon.DynamoDBv2.DataModel;

namespace FinancialSummaryApi.V1.Infrastructure.Entities
{
    public class AssetSummaryDbEntity
    {
        [DynamoDBProperty(AttributeName = "total_dwelling_rent")]
        public decimal TotalDwellingRent { get; set; }

        [DynamoDBProperty(AttributeName = "total_non_dwelling_rent")]
        public decimal TotalNonDwellingRent { get; set; }

        [DynamoDBProperty(AttributeName = "total_service_charges")]
        public decimal TotalServiceCharges { get; set; }

        [DynamoDBProperty(AttributeName = "total_rental_service_charge")]
        public decimal TotalRentalServiceCharge { get; set; }
    }
}
