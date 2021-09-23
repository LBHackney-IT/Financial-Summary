using Amazon.DynamoDBv2.DataModel;
using FinancialSummaryApi.V1.Domain;
using System;

namespace FinancialSummaryApi.V1.Infrastructure.Entities
{
    [DynamoDBTable("FinancialSummaries", LowerCamelCaseProperties = true)]
    public class AssetSummaryDbEntity
    {
        [DynamoDBHashKey(AttributeName = "id")]
        public Guid Id { get; set; }

        [DynamoDBProperty(AttributeName = "summary_type", Converter = typeof(DynamoDbEnumConverter<SummaryType>))]
        [DynamoDBGlobalSecondaryIndexHashKey("summary_type_dx")]
        public SummaryType SummaryType { get; set; }

        [DynamoDBProperty(AttributeName = "target_id")]
        [DynamoDBGlobalSecondaryIndexHashKey("target_id_dx")]
        public Guid TargetId { get; set; }

        [DynamoDBProperty(AttributeName = "target_type", Converter = typeof(DynamoDbEnumConverter<TargetType>))]
        public TargetType TargetType { get; set; }

        [DynamoDBProperty(AttributeName = "submit_date", Converter = typeof(DynamoDbDateTimeConverter))]
        public DateTime SubmitDate { get; set; }

        [DynamoDBProperty(AttributeName = "target_name")]
        [DynamoDBGlobalSecondaryIndexHashKey("target_name_dx")]
        public string TargetName { get; set; }

        [DynamoDBProperty(AttributeName = "total_dwelling_rent")]
        public decimal TotalDwellingRent { get; set; }

        [DynamoDBProperty(AttributeName = "total_non_dwelling_rent")]
        public decimal TotalNonDwellingRent { get; set; }

        [DynamoDBProperty(AttributeName = "total_service_charges")]
        public decimal TotalServiceCharges { get; set; }

        [DynamoDBProperty(AttributeName = "total_rental_service_charge")]
        public decimal TotalRentalServiceCharge { get; set; }

        [DynamoDBProperty(AttributeName = "total_income")]
        public decimal TotalIncome { get; set; }

        [DynamoDBProperty(AttributeName = "total_expenditure")]
        public decimal TotalExpenditure { get; set; }
    }
}
