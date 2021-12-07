using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Util;
using AutoMapper;
using FinancialSummaryApi.V1.Domain;
using FinancialSummaryApi.V1.Factories;
using FinancialSummaryApi.V1.Gateways.Abstracts;
using FinancialSummaryApi.V1.Infrastructure.Entities;
using FinancialSummaryApi.V1.UseCase.Helpers;
using Hackney.Core.DynamoDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialSummaryApi.V1.Gateways
{
    public class DynamoDbGateway : IFinanceSummaryGateway
    {
        private const int MAX_RESULTS = 10;
        private const string TARGETID = "target_id";
        private readonly IDynamoDBContext _dynamoDbContext;
        private readonly IMapper _mapper;
        private Guid _rentGroupTargetId = new Guid("51259000-0dfd-4c74-8e25-45a9c7f2fc90");
        public string PaginationToken { get; set; } = "{}";
        public DynamoDbGateway(IDynamoDBContext dynamoDbContext, IMapper mapper)
        {
            _dynamoDbContext = dynamoDbContext;
            _mapper = mapper;
        }

        #region Asset Summary

        public async Task AddAsync(AssetSummary assetSummary)
        {
            await _dynamoDbContext.SaveAsync(assetSummary.ToDatabase()).ConfigureAwait(false);
        }
        public async Task<List<AssetSummary>> GetAllAssetSummaryAsync(Guid assetId, DateTime submitDate)
        {
            var (submitDateStart, submitDateEnd) = submitDate.GetDayRange();
            var dbAssetSummary = new List<AssetSummaryDbEntity>();
            var table = _dynamoDbContext.GetTargetTable<AssetSummaryDbEntity>();
            var queryConfig = new QueryOperationConfig
            {
                BackwardSearch = true,
                ConsistentRead = true,
                Filter = new QueryFilter(TARGETID, QueryOperator.Equal, assetId),
                PaginationToken = PaginationToken
            };
            queryConfig.Filter.AddCondition("summary_type", QueryOperator.Equal, SummaryType.AssetSummary.ToString());
            queryConfig.Filter.AddCondition("submit_date", QueryOperator.Between, submitDateStart.ToString(AWSSDKUtils.ISO8601DateFormat), submitDateEnd.ToString(AWSSDKUtils.ISO8601DateFormat));

            do
            {
                var search = table.Query(queryConfig);
                PaginationToken = search.PaginationToken;
                var resultsSet = await search.GetNextSetAsync().ConfigureAwait(false);
                if (resultsSet.Any())
                {
                    dbAssetSummary.AddRange(_dynamoDbContext.FromDocuments<AssetSummaryDbEntity>(resultsSet));

                }
            }
            while (!string.Equals(PaginationToken, "{}", StringComparison.Ordinal));

            return dbAssetSummary.ToDomain();
        }

        public async Task<AssetSummary> GetAssetSummaryByIdAsync(Guid assetId, DateTime submitDate)
        {

            var (submitDateStart, submitDateEnd) = submitDate.GetDayRange();
            var dbAssetSummary = new List<AssetSummaryDbEntity>();
            var table = _dynamoDbContext.GetTargetTable<AssetSummaryDbEntity>();
            var queryConfig = new QueryOperationConfig
            {
                BackwardSearch = true,
                ConsistentRead = true,
                Filter = new QueryFilter(TARGETID, QueryOperator.Equal, assetId)
            };
            queryConfig.Filter.AddCondition("summary_type", QueryOperator.Equal, SummaryType.AssetSummary.ToString());
            queryConfig.Filter.AddCondition("submit_date", QueryOperator.Between, submitDateStart, submitDateEnd);
            var search = table.Query(queryConfig);
            var resultsSet = await search.GetNextSetAsync().ConfigureAwait(false);
            if (resultsSet.Any())
            {
                dbAssetSummary.AddRange(_dynamoDbContext.FromDocuments<AssetSummaryDbEntity>(resultsSet));

            }
            return dbAssetSummary.OrderByDescending(x => x.SubmitDate).FirstOrDefault().ToDomain();

        }

        #endregion

        #region Rent Group Summary

        public async Task AddRangeAsync(List<RentGroupSummary> groupSummaries)
        {
            var groupSummariesBatch = _dynamoDbContext.CreateBatchWrite<RentGroupSummaryDbEntity>();
            var groupSummariesDb = groupSummaries.Select(s => s.ToDatabase(_rentGroupTargetId));

            groupSummariesBatch.AddPutItems(groupSummariesDb);
            await groupSummariesBatch.ExecuteAsync().ConfigureAwait(false);
        }


        public async Task<List<RentGroupSummary>> GetAllRentGroupSummaryAsync(DateTime submitDate)
        {
            var (submitDateStart, submitDateEnd) = submitDate.GetDayRange();

            var dbRentGroupSummary = new List<RentGroupSummaryDbEntity>();
            var table = _dynamoDbContext.GetTargetTable<RentGroupSummaryDbEntity>();

            var queryConfig = new QueryOperationConfig
            {
                BackwardSearch = true,
                ConsistentRead = true,
                Filter = new QueryFilter(TARGETID, QueryOperator.Equal, _rentGroupTargetId),
                PaginationToken = PaginationToken
            };
            queryConfig.Filter.AddCondition("summary_type", QueryOperator.Equal, SummaryType.RentGroupSummary.ToString());
            queryConfig.Filter.AddCondition("submit_date", QueryOperator.Between, submitDateStart.ToString(AWSSDKUtils.ISO8601DateFormat), submitDateEnd.ToString(AWSSDKUtils.ISO8601DateFormat));

            do
            {
                var search = table.Query(queryConfig);
                PaginationToken = search.PaginationToken;
                var resultsSet = await search.GetNextSetAsync().ConfigureAwait(false);
                if (resultsSet.Any())
                {
                    dbRentGroupSummary.AddRange(_dynamoDbContext.FromDocuments<RentGroupSummaryDbEntity>(resultsSet));

                }
            }
            while (!string.Equals(PaginationToken, "{}", StringComparison.Ordinal));

            return dbRentGroupSummary.ToDomain();
        }

        public async Task<RentGroupSummary> GetRentGroupSummaryByNameAsync(string rentGroupName, DateTime submitDate)
        {
            var (submitDateStart, submitDateEnd) = submitDate.GetDayRange();
            var dbWeeklySummary = new List<RentGroupSummaryDbEntity>();
            var table = _dynamoDbContext.GetTargetTable<RentGroupSummaryDbEntity>();
            var queryConfig = new QueryOperationConfig
            {
                BackwardSearch = true,
                ConsistentRead = true,
                Filter = new QueryFilter(TARGETID, QueryOperator.Equal, _rentGroupTargetId)
            };
            queryConfig.Filter.AddCondition("target_name", QueryOperator.Equal, rentGroupName);
            queryConfig.Filter.AddCondition("summary_type", QueryOperator.Equal, SummaryType.RentGroupSummary.ToString());
            queryConfig.Filter.AddCondition("submit_date", QueryOperator.Between, submitDateStart, submitDateEnd);
            var search = table.Query(queryConfig);
            var resultsSet = await search.GetNextSetAsync().ConfigureAwait(false);
            if (resultsSet.Any())
            {
                dbWeeklySummary.AddRange(_dynamoDbContext.FromDocuments<RentGroupSummaryDbEntity>(resultsSet));

            }
            return dbWeeklySummary.OrderByDescending(x => x.SubmitDate).FirstOrDefault().ToDomain();

        }

        #endregion

        #region Get Weekly Summary
        public async Task<List<WeeklySummary>> GetAllWeeklySummaryAsync(Guid targetId, DateTime? startDate, DateTime? endDate)
        {
            var dbWeeklySummary = new List<WeeklySummaryDbEntity>();
            var table = _dynamoDbContext.GetTargetTable<WeeklySummaryDbEntity>();

            var queryConfig = new QueryOperationConfig
            {
                BackwardSearch = true,
                ConsistentRead = true,
                Filter = new QueryFilter(TARGETID, QueryOperator.Equal, targetId),
                PaginationToken = PaginationToken
            };
            queryConfig.Filter.AddCondition("summary_type", QueryOperator.Equal, SummaryType.WeeklySummary.ToString());
            if (startDate.HasValue && endDate.HasValue)
            {
                queryConfig.Filter.AddCondition("submit_date", QueryOperator.Between, startDate.Value.ToString(AWSSDKUtils.ISO8601DateFormat), endDate.Value.ToString(AWSSDKUtils.ISO8601DateFormat));
            }

            do
            {
                var search = table.Query(queryConfig);
                PaginationToken = search.PaginationToken;
                var resultsSet = await search.GetNextSetAsync().ConfigureAwait(false);
                if (resultsSet.Any())
                {
                    dbWeeklySummary.AddRange(_dynamoDbContext.FromDocuments<WeeklySummaryDbEntity>(resultsSet));

                }
            }
            while (!string.Equals(PaginationToken, "{}", StringComparison.Ordinal));

            return dbWeeklySummary.ToDomain();
        }

        public async Task AddAsync(WeeklySummary weeklySummary)
        {
            await _dynamoDbContext.SaveAsync(weeklySummary.ToDatabase()).ConfigureAwait(false);
        }

        public async Task<WeeklySummary> GetWeeklySummaryByIdAsync(Guid targetId, Guid id)
        {
            var response = await _dynamoDbContext.LoadAsync<WeeklySummaryDbEntity>(targetId, id).ConfigureAwait(false);
            return response.ToDomain();
        }
        #endregion

        public async Task<PagedResult<Statement>> GetPagedStatementsAsync(Guid targetId, DateTime startDate, DateTime endDate, int pageSize, string paginationToken)
        {
            pageSize = pageSize > 0 ? pageSize : MAX_RESULTS;
            var dbStatements = new List<StatementDbEntity>();
            var table = _dynamoDbContext.GetTargetTable<StatementDbEntity>();

            var queryConfig = new QueryOperationConfig
            {
                BackwardSearch = true,
                ConsistentRead = true,
                Limit = pageSize,
                PaginationToken = PaginationDetails.DecodeToken(paginationToken),
                Filter = new QueryFilter(TARGETID, QueryOperator.Equal, targetId)
            };
            queryConfig.Filter.AddCondition("summary_type", QueryOperator.Equal, SummaryType.Statement.ToString());

            if (startDate != DateTime.MinValue && endDate != DateTime.MinValue)
            {
                queryConfig.Filter.AddCondition("statement_period_end_date", QueryOperator.Between, startDate.ToString(AWSSDKUtils.ISO8601DateFormat), endDate.ToString(AWSSDKUtils.ISO8601DateFormat));
            }
            var search = table.Query(queryConfig);

            var resultsSet = await search.GetNextSetAsync().ConfigureAwait(false);

            paginationToken = search.PaginationToken;
            if (resultsSet.Any())
            {
                dbStatements.AddRange(_dynamoDbContext.FromDocuments<StatementDbEntity>(resultsSet));

                // Look ahead for any more, but only if we have a token
                if (!string.IsNullOrEmpty(PaginationDetails.EncodeToken(paginationToken)))
                {
                    queryConfig.PaginationToken = paginationToken;
                    queryConfig.Limit = 1;
                    search = table.Query(queryConfig);
                    resultsSet = await search.GetNextSetAsync().ConfigureAwait(false);
                    if (!resultsSet.Any())
                        paginationToken = null;
                }
            }

            return new PagedResult<Statement>(dbStatements.Select(x => x.ToDomain()), new PaginationDetails(paginationToken));

        }

        public async Task<List<Statement>> GetStatementListAsync(Guid targetId, DateTime startDate, DateTime endDate)
        {
            var dbStatement = new List<StatementDbEntity>();
            string paginationToken = "{}";
            var table = _dynamoDbContext.GetTargetTable<StatementDbEntity>();

            var queryConfig = new QueryOperationConfig
            {
                BackwardSearch = true,
                ConsistentRead = true,
                Filter = new QueryFilter(TARGETID, QueryOperator.Equal, targetId),
                PaginationToken = paginationToken
            };
            if (startDate != DateTime.MinValue && endDate != DateTime.MinValue)
            {
                queryConfig.Filter.AddCondition("submit_date", QueryOperator.Between, startDate.ToString(AWSSDKUtils.ISO8601DateFormat), endDate.ToString(AWSSDKUtils.ISO8601DateFormat));
            }

            do
            {
                var search = table.Query(queryConfig);
                paginationToken = search.PaginationToken;
                var resultsSet = await search.GetNextSetAsync().ConfigureAwait(false);
                if (resultsSet.Any())
                {
                    dbStatement.AddRange(_dynamoDbContext.FromDocuments<StatementDbEntity>(resultsSet));

                }
            }
            while (!string.Equals(paginationToken, "{}", StringComparison.Ordinal));

            return dbStatement.ToDomain();
        }
        public async Task AddRangeAsync(List<Statement> statements)
        {
            var statementBatch = _dynamoDbContext.CreateBatchWrite<StatementDbEntity>();
            var statementsDb = _mapper.Map<IEnumerable<StatementDbEntity>>(statements);
            statementBatch.AddPutItems(statementsDb);
            await statementBatch.ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<Statement> GetStatementByIdAsync(Guid id, Guid targetId)
        {
            var data = await _dynamoDbContext.LoadAsync<StatementDbEntity>(targetId, id).ConfigureAwait(false);
            return _mapper.Map<Statement>(data);
        }
    }
}
