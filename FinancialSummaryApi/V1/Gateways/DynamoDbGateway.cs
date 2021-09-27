using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using FinancialSummaryApi.V1.Domain;
using FinancialSummaryApi.V1.Factories;
using FinancialSummaryApi.V1.Gateways.Abstracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialSummaryApi.V1.Gateways
{
    public class DynamoDbGateway : IFinanceSummaryGateway
    {
        private readonly IAmazonDynamoDB _amazonDynamoDb; 
        private readonly IDynamoDBContext _dynamoDbContext;

        public DynamoDbGateway(IDynamoDBContext dynamoDbContext, IAmazonDynamoDB amazonDynamoDb)
        {
            _dynamoDbContext = dynamoDbContext;
            _amazonDynamoDb = amazonDynamoDb;
        }

        #region Asset Summary

        public async Task AddAsync(AssetSummary assetSummary)
        {
            await _dynamoDbContext.SaveAsync(assetSummary.ToDatabase()).ConfigureAwait(false);
        }

        public async Task<List<AssetSummary>> GetAllAssetSummaryAsync(DateTime submitDate)
        {
            QueryRequest getSummaryRequest = new QueryRequest
            {
                TableName = "FinancialSummaries",
                IndexName = "summary_type_dx",
                KeyConditionExpression = "summary_type = :V_summary_type ",
                FilterExpression = "submit_date between :V_submit_date_start and :V_submit_date_end",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":V_submit_date_start", new AttributeValue { S = GetDayRange(submitDate).Item1.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ") } },
                    { ":V_submit_date_end", new AttributeValue { S = GetDayRange(submitDate).Item2.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ") } },
                    { ":V_summary_type", new AttributeValue { S = SummaryType.AssetSummary.ToString() } },
                }
            };

            var data = await _amazonDynamoDb.QueryAsync(getSummaryRequest).ConfigureAwait(false);

            return data.ToAssetSummary().OrderByDescending(r => r.SubmitDate).ToList();
        }

        public async Task<AssetSummary> GetAssetSummaryByIdAsync(Guid assetId, DateTime submitDate)
        {
            QueryRequest getAllAssetSummaryRequest = new QueryRequest
            {
                TableName = "FinancialSummaries",
                IndexName = "target_id_dx",
                KeyConditionExpression = "target_id = :V_target_id",
                FilterExpression = "summary_type = :V_summary_type " +
                                   "and submit_date between :V_submit_date_start and :V_submit_date_end",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":V_target_id", new AttributeValue{ S = assetId.ToString() } },
                    { ":V_submit_date_start", new AttributeValue { S = GetDayRange(submitDate).Item1.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ") } },
                    { ":V_submit_date_end", new AttributeValue { S = GetDayRange(submitDate).Item2.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ") } },
                    { ":V_summary_type", new AttributeValue { S = SummaryType.AssetSummary.ToString() } },
                }
            };

            var data = await _amazonDynamoDb.QueryAsync(getAllAssetSummaryRequest).ConfigureAwait(false);

            return data.ToAssetSummary().OrderByDescending(r => r.SubmitDate).FirstOrDefault();
        }

        #endregion

        #region Rent Group Summary

        public async Task AddAsync(RentGroupSummary rentGroupSummary)
        {
            await _dynamoDbContext.SaveAsync(rentGroupSummary.ToDatabase()).ConfigureAwait(false);
        }

        public async Task<List<RentGroupSummary>> GetAllRentGroupSummaryAsync(DateTime submitDate)
        {
            QueryRequest getSummaryRequest = new QueryRequest
            {
                TableName = "FinancialSummaries",
                IndexName = "summary_type_dx",
                KeyConditionExpression = "summary_type = :V_summary_type",
                FilterExpression = "submit_date between :V_submit_date_start and :V_submit_date_end",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":V_submit_date_start", new AttributeValue { S = GetDayRange(submitDate).Item1.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ") } },
                    { ":V_submit_date_end", new AttributeValue { S = GetDayRange(submitDate).Item2.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ") } },
                    { ":V_summary_type", new AttributeValue { S = SummaryType.RentGroupSummary.ToString() } },
                }
            };

            var data = await _amazonDynamoDb.QueryAsync(getSummaryRequest).ConfigureAwait(false);

            return data.ToRentGroupSummary().OrderByDescending(r => r.SubmitDate).ToList();
        }

        public async Task<RentGroupSummary> GetRentGroupSummaryByNameAsync(string rentGroupName, DateTime submitDate)
        {
            QueryRequest getSummaryRequest = new QueryRequest
            {
                TableName = "FinancialSummaries",
                IndexName = "target_name_dx",
                KeyConditionExpression = "target_name = :V_target_name",
                FilterExpression = "summary_type = :V_summary_type " +
                                   "and submit_date between :V_submit_date_start and :V_submit_date_end",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":V_submit_date_start", new AttributeValue { S = GetDayRange(submitDate).Item1.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ") } },
                    { ":V_submit_date_end", new AttributeValue { S = GetDayRange(submitDate).Item2.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ") } },
                    { ":V_target_name", new AttributeValue { S = rentGroupName } },
                    { ":V_summary_type", new AttributeValue { S = SummaryType.RentGroupSummary.ToString() } },
                }
            };

            var data = await _amazonDynamoDb.QueryAsync(getSummaryRequest).ConfigureAwait(false);

            return data.ToRentGroupSummary().OrderByDescending(r => r.SubmitDate).FirstOrDefault();
        }

        #endregion

        #region Get Weekly Summary
        public async Task<List<WeeklySummary>> GetAllWeeklySummaryAsync(Guid targetId, DateTime? startDate, DateTime? endDate)
        {
            QueryRequest getSummaryRequest = new QueryRequest
            {
                TableName = "FinancialSummaries",
                IndexName = "target_id_dx",
                KeyConditionExpression = "target_id = :V_target_id ",
                FilterExpression = "summary_type = :V_summary_type ",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":V_target_id", new AttributeValue { S = targetId.ToString() } },
                    { ":V_summary_type", new AttributeValue { S = SummaryType.WeeklySummary.ToString() } },
                }
            };

            if (startDate.HasValue && endDate.HasValue)
            {
                getSummaryRequest.FilterExpression += "and submit_date between :V_submit_date_start and :V_submit_date_end";
                getSummaryRequest.ExpressionAttributeValues.Add(":V_submit_date_start", new AttributeValue { S = startDate.Value.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ") });
                getSummaryRequest.ExpressionAttributeValues.Add(":V_submit_date_end", new AttributeValue { S = endDate.Value.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ") });
            }

            var data = await _amazonDynamoDb.QueryAsync(getSummaryRequest).ConfigureAwait(false);

            return data.ToWeeklySummary().OrderByDescending(r => r.WeekStartDate).ToList();
        }

        public async Task AddAsync(WeeklySummary weeklySummary)
        {
            await _dynamoDbContext.SaveAsync(weeklySummary.ToDatabase()).ConfigureAwait(false);
        }

        public async Task<WeeklySummary> GetWeeklySummaryByIdAsync(Guid id)
        {
            QueryRequest getWeeklySummaryById = new QueryRequest
            {
                TableName = "FinancialSummaries",
                KeyConditionExpression = "id = :V_id",
                FilterExpression = "summary_type = :V_summary_type ",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":V_id", new AttributeValue{ S = id.ToString() } },
                    { ":V_summary_type", new AttributeValue { S = SummaryType.WeeklySummary.ToString() } },
                }
            };

            var data = await _amazonDynamoDb.QueryAsync(getWeeklySummaryById).ConfigureAwait(false);

            return data.ToWeeklySummary().OrderByDescending(r => r.WeekStartDate).FirstOrDefault();
        }
        #endregion

        private static Tuple<DateTime, DateTime> GetDayRange(DateTime date)
            => new Tuple<DateTime, DateTime>(date.Date, date.Date.AddHours(23).AddMinutes(59));
    }
}
