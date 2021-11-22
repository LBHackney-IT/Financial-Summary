using AutoFixture;
using FinancialSummaryApi.V1.Boundary.Response;
using FinancialSummaryApi.V1.Controllers;
using FinancialSummaryApi.V1.Domain;
using FinancialSummaryApi.V1.Infrastructure.Entities;
using FluentAssertions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace FinancialSummaryApi.Tests.V1.E2ETests
{
    [Collection("MainCollection")]
    public class DynamoDbStatementIntegrationTest : DynamoDbIntegrationTests<Startup>
    {
        private readonly Fixture _fixture = new Fixture();

        /// <summary>
        /// Method to construct a test entity that can be used in a test
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private Statement ConstructStatement()
        {
            var entity = _fixture.Create<Statement>();

            entity.StatementPeriodEndDate = DateTime.UtcNow;

            return entity;
        }

        [Fact]
        public async Task GetStatementListWithInvalidAssetIdReturns400()
        {
            Guid assetId = Guid.Empty;

            var uri = new Uri($"api/v1/statements/{assetId}", UriKind.Relative);
            var response = await Client.GetAsync(uri).ConfigureAwait(false);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var apiEntity = JsonConvert.DeserializeObject<BaseErrorResponse>(responseContent);

            apiEntity.Should().NotBeNull();
            apiEntity.Message.Should().BeEquivalentTo("AssetId should be provided!");
            apiEntity.StatusCode.Should().Be(400);
            apiEntity.Details.Should().BeEquivalentTo(string.Empty);
        }

        [Fact]
        public async Task GetStatementListWithInvalidPageNumberReturns400()
        {
            Guid assetId = new Guid("2a6e12ca-3691-4fa7-bd77-5039652f0354");
            var startDate = DateTime.Now;

            var uri = new Uri($"api/v1/statements/{assetId}?pageNumber=0&startDate={startDate}&endDate={startDate}", UriKind.Relative);
            var response = await Client.GetAsync(uri).ConfigureAwait(false);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var apiEntity = JsonConvert.DeserializeObject<BaseErrorResponse>(responseContent);

            apiEntity.Should().NotBeNull();
            apiEntity.Message.Should().BeEquivalentTo("'Page Number' must be greater than or equal to '1'.");
            apiEntity.StatusCode.Should().Be(400);
            apiEntity.Details.Should().BeEquivalentTo(string.Empty);
        }

        [Fact]
        public async Task CreateStatementListCreatedReturns201()
        {
            var statementDomain = new List<Statement> { ConstructStatement() };

            await CreateStatementListAndValidateResponse(statementDomain).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateStatementList_WithTwoStatements_CreatedReturns201()
        {
            var statementDomains = new List<Statement> { ConstructStatement(), ConstructStatement() };

            await CreateStatementListAndValidateResponse(statementDomains).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateStatementWithSomeEmptyFieldsCreatedReturns201()
        {
            var request = new List<Statement>
            {
                new Statement
                {
                    TargetId = new Guid("2a6e12ca-3691-4fa7-bd77-5039652f0354"),
                    TargetType = TargetType.Estate,
                    StatementPeriodEndDate = new DateTime(2021, 7, 1),
                    RentAccountNumber = "123456789",
                    Address = "16 Macron Court, E8 1ND",
                    StatementType = StatementType.Leasehold
                }
            };

            await CreateStatementListAndValidateResponse(request).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateStatementBadRequestReturns400()
        {
            var statementDomain = new List<Statement> { ConstructStatement() };

            statementDomain[0].ChargedAmount = -100;
            statementDomain[0].PaidAmount = -99;
            statementDomain[0].HousingBenefitAmount = -500;

            var uri = new Uri("api/v1/statements", UriKind.Relative);
            string body = JsonConvert.SerializeObject(statementDomain);

            HttpResponseMessage response;
            using (StringContent stringContent = new StringContent(body))
            {
                stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                response = await Client.PostAsync(uri, stringContent).ConfigureAwait(false);
            }

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var apiEntity = JsonConvert.DeserializeObject<BaseErrorResponse>(responseContent);

            apiEntity.Should().NotBeNull();
            apiEntity.StatusCode.Should().Be(400);
            apiEntity.Details.Should().Be(string.Empty);

            apiEntity.Message.Should().Contain("'Paid Amount' must be greater than or equal to '0'.");
            apiEntity.Message.Should().Contain("'Charged Amount' must be greater than or equal to '0'.");
            apiEntity.Message.Should().Contain("'Housing Benefit Amount' must be greater than or equal to '0'.");
        }

        [Fact]
        public async Task CreateTwoStatementsGetListReturns200()
        {
            var statementDomains = new List<Statement> { ConstructStatement(), ConstructStatement() };
            var assetId = statementDomains[0].TargetId;
            if (assetId != statementDomains[1].TargetId)
            {
                statementDomains[1].TargetId = assetId;
            }
            var startDate = statementDomains[0].StatementPeriodEndDate.Date.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ");
            var endDate = statementDomains[1].StatementPeriodEndDate.Date.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ");

            await CreateStatementListAndValidateResponse(statementDomains).ConfigureAwait(false);

            for (int i = 0; i < statementDomains.Count; i++)
            {
                await GetStatementByIdAndValidateResponse(statementDomains[i]).ConfigureAwait(false);
            }

            var uri = new Uri($"api/v1/statements/{assetId}?pageNumber=1&pageSize={int.MaxValue}&startDate={startDate}&endDate={endDate}", UriKind.Relative);
            using var response = await Client.GetAsync(uri).ConfigureAwait(false);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var apiEntity = JsonConvert.DeserializeObject<StatementListResponse>(responseContent);

            apiEntity.Should().NotBeNull();
            apiEntity.Statements.Count.Should().BeGreaterOrEqualTo(2);
            apiEntity.Statements.Should().BeEquivalentTo(statementDomains);
        }

        [Fact]
        public async Task CreateThreeStatementsSecondPageGetListReturns200()
        {
            var statementDomains = new List<Statement> { ConstructStatement(), ConstructStatement(), ConstructStatement() };
            var assetId = statementDomains[0].TargetId;
            if (assetId != statementDomains[1].TargetId)
            {
                statementDomains[1].TargetId = assetId;
            }
            if (assetId != statementDomains[2].TargetId)
            {
                statementDomains[2].TargetId = assetId;
            }
            var startDate = statementDomains[0].StatementPeriodEndDate.Date.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ");
            var endDate = statementDomains[2].StatementPeriodEndDate.Date.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ");

            await CreateStatementListAndValidateResponse(statementDomains).ConfigureAwait(false);

            for (int i = 0; i < statementDomains.Count; i++)
            {
                await GetStatementByIdAndValidateResponse(statementDomains[i]).ConfigureAwait(false);
            }

            var uri = new Uri($"api/v1/statements/{assetId}?pageNumber=2&pageSize=2&startDate={startDate}&endDate={endDate}", UriKind.Relative);
            using var response = await Client.GetAsync(uri).ConfigureAwait(false);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var apiEntity = JsonConvert.DeserializeObject<StatementListResponse>(responseContent);

            apiEntity.Should().NotBeNull();
            apiEntity.Total.Should().BeGreaterOrEqualTo(statementDomains.Count);
            apiEntity.Statements.Count.Should().Be(1);
            var apiStatement = apiEntity.Statements.Find(r => r.Id == statementDomains[0].Id ||
                                                              r.Id == statementDomains[1].Id ||
                                                              r.Id == statementDomains[2].Id);
            var domainStatement = statementDomains.FirstOrDefault(s => s.Id == apiStatement.Id);
            domainStatement.Should().NotBeNull();
            apiStatement.Should().BeEquivalentTo(domainStatement);
        }

        private async Task CreateStatementListAndValidateResponse(List<Statement> statements)
        {
            var expectedStatements = new List<Statement>(statements);
            var uri = new Uri("api/v1/statements", UriKind.Relative);

            string body = JsonConvert.SerializeObject(expectedStatements);

            using StringContent stringContent = new StringContent(body);
            stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var response = await Client.PostAsync(uri, stringContent).ConfigureAwait(false);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var apiEntityList = JsonConvert.DeserializeObject<List<StatementResponse>>(responseContent);
            foreach (var apiEntity in apiEntityList)
            {
                CleanupActions.Add(async () => await DynamoDbContext.DeleteAsync<StatementDbEntity>(Constants.PartitionKey, apiEntity.Id).ConfigureAwait(false));
            }

            apiEntityList.Should().NotBeNull();

            apiEntityList.Should().BeEquivalentTo(expectedStatements, options => options.Excluding(a => a.Id));

            for (int i = 0; i < apiEntityList.Count; i++)
            {
                statements[i].Id = apiEntityList[i].Id;
            }
        }

        private async Task GetStatementByIdAndValidateResponse(Statement statement)
        {
            var startDate = statement.StatementPeriodEndDate.Date.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffffffZ");
            var endDate = startDate;
            var uri = new Uri($"api/v1/statements/{statement.TargetId}?startDate={startDate}&endDate={endDate}&pageSize={100}", UriKind.Relative);
            using var response = await Client.GetAsync(uri).ConfigureAwait(false);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var apiEntities = JsonConvert.DeserializeObject<StatementListResponse>(responseContent);
            apiEntities.Should().NotBeNull();
            apiEntities.Statements.Should().NotBeNull();
            apiEntities.Total.Should().BeGreaterOrEqualTo(1);

            var apiEntity = apiEntities.Statements.FirstOrDefault(e => e.Id == statement.Id);
            apiEntity.Should().NotBeNull();
            apiEntity.ShouldBeEqualTo(statement);
        }

    }
}
