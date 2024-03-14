// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PayloadFunctionTests.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//   Unit test for payload functions 
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Payload.Service.Test
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Autofac;
    using FluentAssertions;
    using Microsoft.AspNetCore.JsonPatch;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using Juno.Common.Contracts;
    using Juno.Common.DataReference;
    using Juno.Payload.Dto;
    using Juno.Payload.Handoff;
    using Juno.Payload.Service;
    using Juno.Payload.Service.Metrics;
    using Juno.Payload.Service.Model;
    using Juno.Payload.Service.Repository;
    using Juno.Payload.Service.UnitTests;
    using Juno.Payload.Service.UnitTests.Data;
    using Juno.Payload.Service.V1;
    using Juno.Payload.Service.V2;
    using Microsoft.Localization.SignProviders;
    using Microsoft.Localization.Lego.FunctionTestHelper;

    using UpdatePayloadDataV2 = Juno.Payload.Service.V2.UpdatePayloadDataV2;
    using ReadPayloadDataV2 = Juno.Payload.Service.V2.ReadPayloadDataV2;

    /// <summary>
    /// Test payload functions.
    /// </summary>
    [TestClass]
    public class PayloadServiceTests : FunctionTest
    {
        private IContainer _applicationContainer;

        private readonly PayloadWithInlineMetadata<HandoffPayloadMetadata> _payload = MockDataProvider.CreateTestPayload();

        private readonly IEnumerable<LocElementDataReferenceDescriptor> _dataReferenceDescriptors = MockDataProvider.CreateTestDataReferenceDescriptors();

        private string GetBody(IActionResult result)
        {
            if (result is OkObjectResult)
            {
                return ((OkObjectResult)result).Value as string;
            }
            else if (result is ContentResult)
            {
                return ((ContentResult)result).Content;
            }

            throw new NotImplementedException();
        }

        private async Task RunPayloadMetadataServiceAPIs<T>(T payload, bool requireSign, T updatePayload = default(T))
        {
            using (var scope = _applicationContainer.BeginLifetimeScope())
            {
                var log = Mock.Of<ILogger>();
                var metrics = Mock.Of<IPayloadMetricEmitter>();
                var payloadMetadataRepository = scope.Resolve<IPayloadMetadataRepository>();

                var query = new Dictionary<string, StringValues>();
                var headers = new Dictionary<string, StringValues>();
                var newPaylodId = Guid.NewGuid().ToString();
                ISignProvider signProvider = null;

                var body = JsonConvert.SerializeObject(payload);
                if (requireSign)
                {
                    signProvider = scope.Resolve<ISignProvider>();
                    query.Add(Constants.RequireSignQueryParamName, true.ToString());

                    var signature = signProvider.Sign(Encoding.UTF8.GetBytes(body));
                    headers.Add(Constants.SignatureHttpHeader, signature);
                }

                var uploadResult = await UploadPayloadMetadata.RunAsync(
                    req: HttpPostRequestSetup(query, body, httpHeaders: headers),
                    id: newPaylodId,
                    payloadMetadataRepository: payloadMetadataRepository,
                    metricEmitter: metrics,
                    signProvider: signProvider,
                    log: log);

                uploadResult.Should().BeOfType<OkObjectResult>();

                var getResult = await GetPayloadMetadata.RunAsync(
                    HttpGetRequestSetup(query),
                    id: newPaylodId,
                    metricEmitter: metrics,
                    payloadMetadataRepository: payloadMetadataRepository,
                    signProvider: signProvider,
                    log: log);

                getResult.Should().BeAssignableTo<OkObjectResult>();
                var metadata = ((OkObjectResult)getResult).Value;

                var returnedMetadata = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(((OkObjectResult)getResult).Value));

                returnedMetadata.Should().BeEquivalentTo(payload);

                if (updatePayload != null)
                {
                    // TODO: Call a pair of update and get APIs to complete CRUD test scenarios
                }

                var deleteResult = await DeletePayloadMetadata.RunAsync(
                    HttpDeleteRequestSetup(query),
                    id: newPaylodId,
                    metricEmitter: metrics,
                    payloadMetadataRepository: payloadMetadataRepository,
                    log: log);

                deleteResult.Should().BeOfType<OkObjectResult>();
            }
        }

        /// <summary>
        /// Test CreatePayload, UpdatePayloadInlineMetadata, GetPayloadInlineMetadataById, DeletePayloadById  function verifying if the function returns correct payload by id.
        /// </summary>
        /// <returns></returns>
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task Request_PayloadInlineMetadataTestAsync(bool requireSign)
        {
            //create, upload payload inline metadata, get payload inline metadata, delete
            using (var scope = _applicationContainer.BeginLifetimeScope())
            {
                var log = Mock.Of<ILogger>();

                var metricDimProvider = Mock.Of<IGenevaMetricDimensionProvider>();
                var metrics = Mock.Of<IPayloadMetricEmitter>();

                var payloadRepository = scope.Resolve<IPayloadRepository>();
                var payloadDataRefRepository = scope.Resolve<IPayloadDataRefRepository>();
                var payloadAttachmentRepository = scope.Resolve<IPayloadAttachmentRepository>();
                var payloadMetadataRepository = scope.Resolve<IPayloadMetadataRepository>();

                var query = new Dictionary<string, StringValues>();
                string newPaylodId = Guid.NewGuid().ToString();

                ISignProvider signProvider = null;
                if (requireSign)
                {
                    signProvider = scope.Resolve<ISignProvider>();
                    query.Add(Constants.RequireSignQueryParamName, true.ToString());
                }

                var result = await CreatePayload.RunAsync(
                    req: HttpPostRequestSetup(query),
                    metricEmitter: metrics,
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    signProvider: signProvider,
                    log: log);

                var resultObject = (OkObjectResult)result;
                var createdResult = JsonConvert.DeserializeObject<PayloadDefinitionDto>(JsonConvert.SerializeObject(resultObject.Value));

                Assert.IsFalse(createdResult == null || createdResult.Id == Guid.Empty, "Payload ID was not returned.");

                var body = JsonConvert.SerializeObject(_payload.Metadata);

                var headers = new Dictionary<string, StringValues>();
                if (requireSign)
                {
                    var signature = signProvider.Sign(Encoding.UTF8.GetBytes(body));
                    headers.Add(Constants.SignatureHttpHeader, signature);
                }

                var result2 = await UpdatePayloadInlineMetadata.RunAsync(
                    req: HttpPostRequestSetup(query, body, "application/json", headers),
                    metricEmitter: metrics,
                    id: createdResult.Id.ToString(),
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    signProvider: signProvider,
                    log: log);
                var resultObject2 = (OkObjectResult)result2;

                query = new Dictionary<String, StringValues>()
                {
                    { "id", newPaylodId }
                };

                body = "";

                var result3 = await GetPayloadInlineMetadataById.RunAsync(
                    HttpGetRequestSetup(query),
                    id: createdResult.Id.ToString(),
                    metricEmitter: metrics,
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    signProvider: signProvider,
                    log: log);

                var resultData = GetBody(result3);
                var returnedPayload = JsonConvert.DeserializeObject<HandoffPayloadMetadata>(resultData);

                Assert.AreEqual(_payload.Metadata.Branch.BranchId, returnedPayload.Branch.BranchId);
                Assert.AreEqual(_payload.Metadata.Build.BuildId, returnedPayload.Build.BuildId);
                Assert.AreEqual(_payload.Metadata.EolMapMetadata.Lcgs.Count(), returnedPayload.EolMapMetadata.Lcgs.Count());

                var resultDeleted = await DeletePayload.RunAsync(
                    HttpDeleteRequestSetup(query),
                    id: createdResult.Id.ToString(),
                    payloadRepository: payloadRepository,
                    metricEmitter: metrics,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    payloadMetadataRepository: payloadMetadataRepository,
                    payloadDataReferencesRepository: payloadDataRefRepository,
                    log: log);
                Assert.IsTrue(resultDeleted is OkObjectResult);
            }
        }

        /// <summary>
        /// Test UploadPayloadMetadata, GetPayloadMetadataById function verifying if the function returns correct metadata by id.
        /// </summary>
        /// <returns></returns>
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task Request_PayloadMetadataTestsAsync(bool requireSign)
        {
            //upload payload metadata, get payload metadata, delete
            using (var scope = _applicationContainer.BeginLifetimeScope())
            {
                var log = Mock.Of<ILogger>();

                var payloadMetadataRepository = scope.Resolve<IPayloadMetadataRepository>();

                var query = new Dictionary<string, StringValues>();
                var newPaylodId = Guid.NewGuid().ToString();
                var metrics = Mock.Of<IPayloadMetricEmitter>();

                ISignProvider signProvider = null;
                if (requireSign)
                {
                    signProvider = scope.Resolve<ISignProvider>();
                    query.Add(Constants.RequireSignQueryParamName, true.ToString());
                }

                var body = JsonConvert.SerializeObject(_payload.Metadata.EolMapMetadata.Lcgs);

                var headers = new Dictionary<String, StringValues>();
                if (requireSign)
                {
                    var signature = signProvider.Sign(Encoding.UTF8.GetBytes(body));
                    headers.Add(Constants.SignatureHttpHeader, signature);
                }

                var result2 = await UploadPayloadMetadata.RunAsync(
                    req: HttpPostRequestSetup(query, body, httpHeaders: headers),
                    id: newPaylodId,
                    metricEmitter: metrics,
                    payloadMetadataRepository: payloadMetadataRepository,
                    signProvider: signProvider,
                    log: log);

                var resultObject2 = (OkObjectResult)result2;

                body = string.Empty;

                var result3 = await GetPayloadMetadata.RunAsync(
                    HttpPostRequestSetup(query, body),
                    id: newPaylodId,
                    metricEmitter: metrics,
                    payloadMetadataRepository: payloadMetadataRepository,
                    signProvider: signProvider,
                    log: log);

                var returnedMetadata = JsonConvert.DeserializeObject<IEnumerable<LcgMetadata>>(
                    JsonConvert.SerializeObject(((OkObjectResult)result3).Value));

                var returnedLcg = returnedMetadata.First();
                Assert.AreEqual(_payload.Metadata.EolMapMetadata.Lcgs.Count(), returnedMetadata.Count(),
                    $"All payload metadata in a payload should be returned with GetPayloadMetadata function.");

                var resultDeleted = await DeletePayloadMetadata.RunAsync(
                    HttpDeleteRequestSetup(query),
                    id: newPaylodId,
                    metricEmitter: metrics,
                    payloadMetadataRepository: payloadMetadataRepository,
                    log: log);

                Assert.IsTrue(resultDeleted is OkObjectResult,
                    $"All payload metadata in a payload should be deleted.");
            }
        }

        /// <summary>
        /// Test UploadPayloadMetadata, GetPayloadMetadataById function verifying if the function returns correct metadata by id.
        /// </summary>
        /// <returns></returns>
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task Request_PayloadMetadataByIdTestsAsync(bool requireSign)
        {
            //create, upload payload inline metadata, get payload inline metadata, delete
            using (var scope = _applicationContainer.BeginLifetimeScope())
            {
                var log = Mock.Of<ILogger>();

                var payloadMetadataRepository = scope.Resolve<IPayloadMetadataRepository>();

                var query = new Dictionary<String, StringValues>();
                var newPaylodId = Guid.NewGuid().ToString();
                var metrics = Mock.Of<IPayloadMetricEmitter>();

                var lcgMetadata = new
                {
                    Id = Guid.NewGuid(),
                    _payload.Metadata.EolMapMetadata.Lcgs.First().LcgMetadata
                };

                var body = JsonConvert.SerializeObject(lcgMetadata);

                var result2 = await UploadPayloadMetadata.RunAsync(
                    req: HttpRequestSetup(query, body),
                    id: newPaylodId,
                    metricEmitter: metrics,
                    payloadMetadataRepository: payloadMetadataRepository,
                    signProvider: null,
                    log: log);

                var resultObject2 = (OkObjectResult)result2;

                query = new Dictionary<string, StringValues>()
                {
                    { "id", newPaylodId }
                };

                var param = new
                {
                    ProvidedIds = new List<Guid>()
                    {
                        lcgMetadata.Id
                    }
                };

                body = JsonConvert.SerializeObject(param);

                var result3 = await GetPayloadMetadataById.RunAsync(
                    HttpPostRequestSetup(query, body),
                    id: newPaylodId,
                    metricEmitter: metrics,
                    payloadMetadataRepository: payloadMetadataRepository,
                    signProvider: null,
                    log: log);

                var returnedMetadata = JsonConvert.DeserializeObject<IEnumerable<LcgMetadata>>(
                                            JsonConvert.SerializeObject(((OkObjectResult)result3).Value));

                var returnedLcg = returnedMetadata.First();
                Assert.AreEqual(lcgMetadata.Id, returnedLcg.Id,
                    $"Matching payload matadata with provided Id should be returned.");

                var resultDeleted = await DeletePayloadMetadata.RunAsync(
                    HttpDeleteRequestSetup(query),
                    id: newPaylodId,
                    metricEmitter: metrics,
                    payloadMetadataRepository: payloadMetadataRepository,
                    log: log);

                Assert.IsTrue(resultDeleted is OkObjectResult);
            }
        }

        /// <summary>
        /// Test CreatePayload, UploadPayloadDataReference, GetAllPayloadDataReferences function verifying if the function returns correct payload by id.
        /// </summary>
        /// <returns></returns>
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task Request_PayloadDataReferencesTestAsync(bool requireSign)
        {
            using (var scope = _applicationContainer.BeginLifetimeScope())
            {
                var log = Mock.Of<ILogger>();

                var payloadRepository = scope.Resolve<IPayloadRepository>();
                var payloadDataRefRepository = scope.Resolve<IPayloadDataRefRepository>();
                var payloadAttachmentRepository = scope.Resolve<IPayloadAttachmentRepository>();
                var payloadMetadataRepository = scope.Resolve<IPayloadMetadataRepository>();
                var metrics = Mock.Of<IPayloadMetricEmitter>();

                var query = new Dictionary<String, StringValues>();
                var newPaylodId = Guid.NewGuid().ToString();

                ISignProvider signProvider = null;
                if (requireSign)
                {
                    signProvider = scope.Resolve<ISignProvider>();
                    query.Add(Constants.RequireSignQueryParamName, true.ToString());
                }

                var result = await CreatePayload.RunAsync(
                    req: HttpGetRequestSetup(query),
                    payloadRepository: payloadRepository,
                    metricEmitter: metrics,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    signProvider: signProvider,
                    log: log);
                var resultObject = (OkObjectResult)result;
                var createdResult = JsonConvert.DeserializeObject<PayloadDefinitionDto>(JsonConvert.SerializeObject(resultObject.Value));

                Assert.IsFalse(createdResult == null || createdResult.Id == Guid.Empty, "Payload ID was not returned.");

                var body = JsonConvert.SerializeObject(new { DataReferences = _dataReferenceDescriptors });
                var headers = new Dictionary<String, StringValues>();
                if (requireSign)
                {
                    var signature = signProvider.Sign(Encoding.UTF8.GetBytes(body));
                    headers.Add(Constants.SignatureHttpHeader, signature);
                }
                var result2 = await UploadPayloadDataReferences.RunAsync(
                    req: HttpPostRequestSetup(query, body, httpHeaders: headers),
                    id: createdResult.Id.ToString(),
                    metricEmitter: metrics,
                    payloadDataRefRepository: payloadDataRefRepository,
                    signProvider: signProvider,
                    log: log);

                var resultObject2 = (OkObjectResult)result2;

                query = new Dictionary<String, StringValues>()
                {
                    { "id", newPaylodId }
                };

                body = "";

                var result3 = await GetStoredPayloadDataReferences.RunAsync(
                    HttpGetRequestSetup(query),
                    metricEmitter: metrics,
                    id: createdResult.Id.ToString(),
                    payloadDataRefRepository: payloadDataRefRepository,
                    signProvider: signProvider,
                    log: log);

                var returnedPayload = JsonConvert.DeserializeObject<IEnumerable<LocElementDataReferenceDescriptor>>(JsonConvert.SerializeObject(((OkObjectResult)result3).Value));

                Assert.AreEqual(_dataReferenceDescriptors.Count(), returnedPayload.Count(),
                    "All data references in a payload should be returned with GetStoredPayloadDataReferences function.");

                var resultDeleted = await DeletePayload.RunAsync(
                    HttpDeleteRequestSetup(query),
                    id: createdResult.Id.ToString(),
                    payloadRepository: payloadRepository,
                    metricEmitter: metrics,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    payloadMetadataRepository: payloadMetadataRepository,
                    payloadDataReferencesRepository: payloadDataRefRepository,
                    log: log);
                Assert.IsTrue(resultDeleted is OkObjectResult);
            }
        }

        /// <summary>
        /// Test CreatePayload, UploadPayloadDataReference, GetAllPayloadDataReferences function verifying if the function returns correct payload by id.
        /// </summary>
        /// <returns></returns>
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task Request_PayloadDataReferencesPerIdsTestAsync(bool requireSign)
        {
            using (var scope = _applicationContainer.BeginLifetimeScope())
            {
                var log = Mock.Of<ILogger>();

                var payloadRepository = scope.Resolve<IPayloadRepository>();
                var payloadDataRefRepository = scope.Resolve<IPayloadDataRefRepository>();
                var payloadAttachmentRepository = scope.Resolve<IPayloadAttachmentRepository>();
                var payloadMetadataRepository = scope.Resolve<IPayloadMetadataRepository>();
                var metrics = Mock.Of<IPayloadMetricEmitter>();

                var query = new Dictionary<String, StringValues>();
                var headers = new Dictionary<String, StringValues>();

                ISignProvider signProvider = null;
                if (requireSign)
                {
                    signProvider = scope.Resolve<ISignProvider>();
                    query.Add(Constants.RequireSignQueryParamName, true.ToString());
                }

                var newPaylodId = Guid.NewGuid().ToString();

                var result = await CreatePayload.RunAsync(
                    req: HttpPostRequestSetup(query),
                    metricEmitter: metrics,
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    signProvider: signProvider,
                    log: log);
                var resultObject = (OkObjectResult)result;
                var createdResult = JsonConvert.DeserializeObject<PayloadDefinitionDto>(JsonConvert.SerializeObject(resultObject.Value));

                Assert.IsFalse(createdResult == null || createdResult.Id == Guid.Empty, "Payload ID was not returned.");

                var body = JsonConvert.SerializeObject(new { DataReferences = _dataReferenceDescriptors });

                if (requireSign)
                {
                    var signature = signProvider.Sign(Encoding.UTF8.GetBytes(body));
                    headers.Add(Constants.SignatureHttpHeader, signature);
                }

                var result2 = await UploadPayloadDataReferences.RunAsync(
                    req: HttpPostRequestSetup(query, body, httpHeaders: headers),
                    id: createdResult.Id.ToString(),
                    metricEmitter: metrics,
                    payloadDataRefRepository: payloadDataRefRepository,
                    signProvider: signProvider,
                    log: log);

                var resultObject2 = (OkObjectResult)result2;

                var getPayloadDataReferencesBody = JsonConvert.SerializeObject(new
                {
                    ProvidedIds = _dataReferenceDescriptors.Skip(1).Select(i => i.LocElementMetadata.GroupId).ToArray()
                });

                headers = new Dictionary<string, StringValues>();
                if (requireSign)
                {
                    var signature = signProvider.Sign(Encoding.UTF8.GetBytes(getPayloadDataReferencesBody));
                    headers.Add(Constants.SignatureHttpHeader, signature);
                }

                var result3 = await GetStoredPayloadDataReferenceByProvidedIds.RunAsync(
                    req: HttpPostRequestSetup(query, getPayloadDataReferencesBody, httpHeaders: headers),
                    id: createdResult.Id.ToString(),
                    metricEmitter: metrics,
                    payloadDataRefRepository: payloadDataRefRepository,
                    signProvider: signProvider,
                    log: log);

                var returnedPayload = JsonConvert.DeserializeObject<IEnumerable<LocElementDataReferenceDescriptor>>(JsonConvert.SerializeObject(((OkObjectResult)result3).Value));

                Assert.AreEqual(_dataReferenceDescriptors.Count() - 1, returnedPayload.Count(),
                    "Data references should be returned that matches with given Ids.");

                var getOnePayloadDataReferenceBody = JsonConvert.SerializeObject(new
                {
                    ProvidedIds = new[] { _dataReferenceDescriptors.Select(i => i.LocElementMetadata.Id).First() }
                });

                var resultDeleted = await DeletePayload.RunAsync(
                    HttpDeleteRequestSetup(query),
                    id: createdResult.Id.ToString(),
                    metricEmitter: metrics,
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    payloadMetadataRepository: payloadMetadataRepository,
                    payloadDataReferencesRepository: payloadDataRefRepository,
                    log: log);
                Assert.IsTrue(resultDeleted is OkObjectResult);
            }
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task CanUploadJsonPatchesAsPayloadMetadata(bool requireSign)
        {
            var json = "["
                + "{\"value\":\"SoftwareBranchConfigCultureUpdation.fe9457a8-c051-4735-b9df-41cb426c027e\",\"path\":\"/CorrelationId\",\"op\":\"replace\"},"
                + "{\"value\":637840837442569518,\"path\":\"/UtcTicksCreatedAt\",\"op\":\"replace\"},"
                + "{\"value\":\"bec5a930-08b5-4392-9908-8d6c015c2e4a\",\"path\":\"/BranchId\",\"op\":\"replace\"},"
                + "{\"value\":24,\"path\":\"/FriendlyBranchId\",\"op\":\"replace\"},"
                + "{\"value\":[{\"CultureName\":\"ja-JP\",\"CustomFolderName\":\"ja\"},{\"CultureName\":\"haw-US\",\"CustomFolderName\":\"haw\"}],\"path\":\"/BranchCultureSettings\",\"op\":\"replace\"}]";

            var patches = JsonConvert.DeserializeObject<JsonPatchDocument>(json);

            await this.RunPayloadMetadataServiceAPIs(patches, requireSign).ConfigureAwait(false);
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task Request_CreatePayloadWithInlineMetadataTest_akka_CreatePayloadV1(bool requireSign)
        {
            using (var scope = _applicationContainer.BeginLifetimeScope())
            {
                var log = Mock.Of<ILogger>();

                var payloadRepository = scope.Resolve<IPayloadRepository>();
                var payloadDataRefRepository = scope.Resolve<IPayloadDataRefRepository>();
                var payloadAttachmentRepository = scope.Resolve<IPayloadAttachmentRepository>();
                var metrics = Mock.Of<IPayloadMetricEmitter>();

                var query = new Dictionary<String, StringValues>();

                ISignProvider signProvider = null;
                if (requireSign)
                {
                    signProvider = scope.Resolve<ISignProvider>();
                    query.Add(Constants.RequireSignQueryParamName, true.ToString());
                }

                var result1 = await CreatePayload.RunAsync(
                    req: HttpPostRequestSetup(query),
                    payloadRepository: payloadRepository,
                    metricEmitter: metrics,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    signProvider: signProvider,
                    log: log);
                var resultObject = (OkObjectResult)result1;
                var createdResult = JsonConvert.DeserializeObject<PayloadDefinitionDto>(JsonConvert.SerializeObject(resultObject.Value));

                Assert.IsFalse(createdResult == null || createdResult.Id == Guid.Empty, "Payload ID was not returned.");

                var newPayloadId = createdResult.Id;

                var payloadData = new MockPayloadData()
                {
                    Id = Guid.NewGuid(),
                    Data = new Dictionary<string, string>() { { "key1", "data1" }, { "key2", "data2" } }
                };

                var body = JsonConvert.SerializeObject(payloadData);
                var headers = new Dictionary<String, StringValues>();
                if (requireSign)
                {
                    var signature = signProvider.Sign(Encoding.UTF8.GetBytes(body));
                    headers.Add(Constants.SignatureHttpHeader, signature);
                }

                var result2 = await UpdatePayloadInlineMetadata.RunAsync(
                    req: HttpPostRequestSetup(query, body, httpHeaders: headers),
                    id: newPayloadId.ToString(),
                    metricEmitter: metrics,
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    signProvider: signProvider,
                    log: log);
                var responseCode2 = ((OkObjectResult)result2).StatusCode;
                Assert.IsTrue(responseCode2 == (int?)HttpStatusCode.OK || responseCode2 == (int?)HttpStatusCode.Created);

                var result3 = await GetPayloadInlineMetadataById.RunAsync(
                    req: HttpGetRequestSetup(query),
                    id: newPayloadId.ToString(),
                    metricEmitter: metrics,
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    signProvider: signProvider,
                    log: log);

                var jsonData = ((ContentResult)result3).Content;
                var payloadDataResponse = JsonConvert.DeserializeObject<MockPayloadData>(jsonData);

                Assert.IsNotNull(payloadDataResponse);
                Assert.AreEqual(payloadData.Id, payloadDataResponse.Id);

                foreach (var key in payloadData.Data.Keys)
                {
                    Assert.AreEqual(payloadData.Data[key], payloadDataResponse.Data[key]);
                }
            }
        }

        [TestMethod]
        public async Task Request_CreatePayloadV2_WithoutId()
        {
            using (var scope = _applicationContainer.BeginLifetimeScope())
            {
                var log = Mock.Of<ILogger>();

                var payloadRepository = scope.Resolve<IPayloadRepository>();
                var payloadDataRefRepository = scope.Resolve<IPayloadDataRefRepository>();
                var payloadAttachmentRepository = scope.Resolve<IPayloadAttachmentRepository>();
                var metrics = Mock.Of<IPayloadMetricEmitter>();
                var branchId = Guid.NewGuid();
                var partitionKey = branchId.ToString();

                var query1 = new Dictionary<String, StringValues>();
                query1["category"] = new StringValues("MockPayload");

                var result1 = await CreatePayloadV2.RunAsync(
                    req: HttpPostRequestSetup(query1),
                    partitionKey,
                    null,
                    metricEmitter: metrics,
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    signProvider: null,
                    log: log);
                var resultObject = (OkObjectResult)result1;
                var createdResult = JsonConvert.DeserializeObject<PayloadDefinitionDto>(JsonConvert.SerializeObject(resultObject.Value));


                Assert.IsFalse(createdResult == null || createdResult.Id == Guid.Empty, "Payload ID was not returned.");

                var newPayloadId = createdResult.Id;

                var payloadRepositoryItem = await payloadRepository.GetItemAsync<PayloadWithInlineMetadata<JObject>>(newPayloadId.ToString(), partitionKey);

                Assert.AreEqual("MockPayload", payloadRepositoryItem.Category);
            }
        }

        [TestMethod]
        public async Task Request_CreatePayloadV2_WithId()
        {
            using (var scope = _applicationContainer.BeginLifetimeScope())
            {
                var log = Mock.Of<ILogger>();

                var payloadRepository = scope.Resolve<IPayloadRepository>();
                var payloadDataRefRepository = scope.Resolve<IPayloadDataRefRepository>();
                var payloadAttachmentRepository = scope.Resolve<IPayloadAttachmentRepository>();
                var metrics = Mock.Of<IPayloadMetricEmitter>();
                var branchId = Guid.NewGuid();
                var partitionKey = branchId.ToString();
                var newPayloadId = Guid.NewGuid().ToString();

                var query1 = new Dictionary<String, StringValues>();
                query1["category"] = new StringValues("MockPayload");

                var result1 = await CreatePayloadV2.RunAsync(
                    req: HttpPostRequestSetup(query1),
                    partitionKey,
                    newPayloadId,
                    metricEmitter: metrics,
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    signProvider: null,
                    log: log);
                var resultObject = (OkObjectResult)result1;
                var createdResult = JsonConvert.DeserializeObject<PayloadDefinitionDto>(JsonConvert.SerializeObject(resultObject.Value));


                Assert.IsFalse(createdResult == null || createdResult.Id == Guid.Empty, "Payload ID was not returned.");

                Assert.AreEqual(newPayloadId, createdResult.Id.ToString());

                var payloadRepositoryItem = await payloadRepository.GetItemAsync<PayloadWithInlineMetadata<JObject>>(newPayloadId.ToString(), partitionKey);

                Assert.AreEqual("MockPayload", payloadRepositoryItem.Category);
            }
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task Request_CreatePayloadV2_UploadPayloadData_WithBlobAttachment_NonCompressed(bool requireSign)
        {
            using (var scope = _applicationContainer.BeginLifetimeScope())
            {
                var log = Mock.Of<ILogger>();

                var payloadRepository = scope.Resolve<IPayloadRepository>();
                var payloadDataRefRepository = scope.Resolve<IPayloadDataRefRepository>();
                var payloadAttachmentRepository = scope.Resolve<IPayloadAttachmentRepository>();
                var payloadMetadataRepository = scope.Resolve<IPayloadMetadataRepository>();
                var metrics = Mock.Of<IPayloadMetricEmitter>();
                var branchId = Guid.NewGuid();
                var partitionKey = branchId.ToString();

                var query1 = new Dictionary<String, StringValues>();
                query1["category"] = new StringValues("MockPayload");
                ISignProvider signProvider = null;
                if (requireSign)
                {
                    signProvider = scope.Resolve<ISignProvider>();
                    query1.Add(Constants.RequireSignQueryParamName, true.ToString());
                }

                var result1 = await CreatePayloadV2.RunAsync(
                    req: HttpPostRequestSetup(query1),
                    partitionKey,
                    null,
                    metricEmitter: metrics,
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    signProvider: signProvider,
                    log: log);
                var resultObject = (OkObjectResult)result1;
                var createdResult = JsonConvert.DeserializeObject<PayloadDefinitionDto>(JsonConvert.SerializeObject(resultObject.Value));

                Assert.IsFalse(createdResult == null || createdResult.Id == Guid.Empty, "Payload ID was not returned.");

                var newPayloadId = createdResult.Id;

                var payloadRepositoryItem = await payloadRepository.GetItemAsync<PayloadWithInlineMetadata<JObject>>(newPayloadId.ToString(), partitionKey);

                Assert.AreEqual("MockPayload", payloadRepositoryItem.Category);

                var payloadData = new MockPayloadData()
                {
                    Id = Guid.NewGuid(),
                    Data = new Dictionary<string, string>() { { "key1", "data1" }, { "key2", "data2" } }
                };

                var query2 = new Dictionary<String, StringValues>();
                var headers = new Dictionary<String, StringValues>();

                query2["payloadDataType"] = new StringValues(PayloadDataType.BlobAttachment.ToString());

                var body = JsonConvert.SerializeObject(payloadData);
                if (requireSign)
                {
                    query2.Add(Constants.RequireSignQueryParamName, true.ToString());
                    var signature = signProvider.Sign(Encoding.UTF8.GetBytes(body));
                    headers.Add(Constants.SignatureHttpHeader, signature);
                }

                var result2 = await UpdatePayloadDataV2.RunAsync(
                    req: HttpPostRequestSetup(query2, body, httpHeaders: headers),
                    partitionKey: partitionKey,
                    id: newPayloadId.ToString(),
                    metricEmitter: metrics,
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    signProvider: signProvider,
                    log: log);
                var responseCode2 = ((OkObjectResult)result2).StatusCode;
                Assert.IsTrue(responseCode2 == (int?)HttpStatusCode.OK || responseCode2 == (int?)HttpStatusCode.Created);

                var query3 = new Dictionary<string, StringValues>();
                if (requireSign)
                {
                    query3.Add(Constants.RequireSignQueryParamName, true.ToString());
                }

                var result3 = await ReadPayloadDataV2.RunAsync(
                    req: HttpGetRequestSetup(query3),
                    partitionKey: partitionKey,
                    id: newPayloadId.ToString(),
                    metricEmitter: metrics,
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    signProvider: signProvider,
                    log: log);

                var jsonData = result3.Content.ReadAsStringAsync().Result;
                var payloadDataResponse = JsonConvert.DeserializeObject<MockPayloadData>(jsonData);

                Assert.IsNotNull(payloadDataResponse);
                Assert.AreEqual(payloadData.Id, payloadDataResponse.Id);
                foreach (var key in payloadData.Data.Keys)
                {
                    Assert.AreEqual(payloadData.Data[key], payloadDataResponse.Data[key]);
                }

                var query4 = new Dictionary<string, StringValues>();
                if (requireSign)
                {
                    query4.Add(Constants.RequireSignQueryParamName, true.ToString());
                }

                var result4 = await DeletePayloadV2.RunAsync(
                    req: HttpDeleteRequestSetup(query4),
                    partitionKey: partitionKey,
                    id: newPayloadId.ToString(),
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    payloadMetadataRepository: payloadMetadataRepository,
                    metricEmitter: metrics,
                    payloadDataReferencesRepository: payloadDataRefRepository,
                    CancellationToken.None,
                    log: log);

                Assert.IsTrue(((OkObjectResult)result4).StatusCode == (int?)HttpStatusCode.OK);
            }
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task Request_CreatePayloadV2_WithPayloadDataAsBlobAttachment(bool requireSign)
        {
            using (var scope = _applicationContainer.BeginLifetimeScope())
            {
                var log = Mock.Of<ILogger>();

                var payloadRepository = scope.Resolve<IPayloadRepository>();
                var payloadDataRefRepository = scope.Resolve<IPayloadDataRefRepository>();
                var payloadAttachmentRepository = scope.Resolve<IPayloadAttachmentRepository>();
                var payloadMetadataRepository = scope.Resolve<IPayloadMetadataRepository>();
                var metrics = Mock.Of<IPayloadMetricEmitter>();
                var branchId = Guid.NewGuid();
                var partitionKey = branchId.ToString();

                var query1 = new Dictionary<String, StringValues>();
                query1[Constants.CategoryParamName] = new StringValues("MockPayload");
                query1[Constants.PayloadDataTypeParamName] = new StringValues(PayloadDataType.BlobAttachment.ToString());
                query1[Constants.WithPayloadDataParamName] = new StringValues(true.ToString());

                ISignProvider signProvider = null;
                if (requireSign)
                {
                    signProvider = scope.Resolve<ISignProvider>();
                    query1.Add(Constants.RequireSignQueryParamName, true.ToString());
                }

                var payloadData = new MockPayloadData()
                {
                    Id = Guid.NewGuid(),
                    Data = new Dictionary<string, string>() { { "key1", "data1" }, { "key2", "data2" } }
                };

                var body = JsonConvert.SerializeObject(payloadData);
                var headers = new Dictionary<String, StringValues>();

                if (requireSign)
                {
                    var signature = signProvider.Sign(Encoding.UTF8.GetBytes(body));
                    headers.Add(Constants.SignatureHttpHeader, signature);
                }

                var result1 = await CreatePayloadV2.RunAsync(
                    req: HttpPostRequestSetup(query1, body, httpHeaders: headers),
                    partitionKey,
                    null,
                    metricEmitter: metrics,
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    signProvider: signProvider,
                    log: log);
                var resultObject = (OkObjectResult)result1;
                var createdResult = JsonConvert.DeserializeObject<PayloadDefinitionDto>(JsonConvert.SerializeObject(resultObject.Value));

                Assert.IsFalse(createdResult == null || createdResult.Id == Guid.Empty, "Payload ID was not returned.");

                var newPayloadId = createdResult.Id;

                var payloadRepositoryItem = await payloadRepository.GetItemAsync<PayloadWithInlineMetadata<JObject>>(newPayloadId.ToString(), partitionKey);

                Assert.AreEqual("MockPayload", payloadRepositoryItem.Category);

                var query3 = new Dictionary<string, StringValues>();
                if (requireSign)
                {
                    query3.Add(Constants.RequireSignQueryParamName, true.ToString());
                }

                var result3 = await ReadPayloadDataV2.RunAsync(
                    req: HttpGetRequestSetup(query3),
                    partitionKey: partitionKey,
                    metricEmitter: metrics,
                    id: newPayloadId.ToString(),
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    signProvider: signProvider,
                    log: log);

                var jsonData = result3.Content.ReadAsStringAsync().Result;
                var payloadDataResponse = JsonConvert.DeserializeObject<MockPayloadData>(jsonData);

                Assert.IsNotNull(payloadDataResponse);
                Assert.AreEqual(payloadData.Id, payloadDataResponse.Id);
                foreach (var key in payloadData.Data.Keys)
                {
                    Assert.AreEqual(payloadData.Data[key], payloadDataResponse.Data[key]);
                }

                var query4 = new Dictionary<string, StringValues>();
                if (requireSign)
                {
                    query4.Add(Constants.RequireSignQueryParamName, true.ToString());
                }

                var result4 = await DeletePayloadV2.RunAsync(
                    req: HttpDeleteRequestSetup(query4),
                    partitionKey: partitionKey,
                    id: newPayloadId.ToString(),
                    payloadRepository: payloadRepository,
                    payloadAttachmentRepository: payloadAttachmentRepository,
                    payloadMetadataRepository: payloadMetadataRepository,
                    payloadDataReferencesRepository: payloadDataRefRepository,
                    metricEmitter: metrics,
                    cancellationToken: CancellationToken.None,
                    log: log
                    );

                Assert.IsTrue(((OkObjectResult)result4).StatusCode == (int?)HttpStatusCode.OK);
            }
        }

        /// <summary>
        /// Initialize test environment, setting up local environment variables that functions use.
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            using (var sr = new StreamReader("local.settings.json"))
            {
                var settings = sr.ReadToEnd();
                var localSettings = JsonConvert.DeserializeObject<dynamic>(settings);

                foreach (var var in localSettings.Values)
                {
                    try
                    {
                        Environment.SetEnvironmentVariable(var.Name, var.Value.ToString(), EnvironmentVariableTarget.Process);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Couldn't set environment variable {var.Name} with {var.Value}. {ex.Message}.");
                    }
                }
            }

            var containerBuilder = new ContainerBuilder();

            var payloadAttachmentRepository = new MockPayloadRepository();
            var payloadRepository = new MockPayloadRepository();
            var payloadMetadataRepository = new MockPayloadRepository();
            var payloadDataRefRepository = new MockPayloadRepository();



            containerBuilder.Register(ctx => payloadAttachmentRepository).As<IPayloadAttachmentRepository>().InstancePerLifetimeScope();
            containerBuilder.Register(ctx => payloadRepository).As<IPayloadRepository>().InstancePerLifetimeScope();
            containerBuilder.Register(ctx => payloadMetadataRepository).As<IPayloadMetadataRepository>().InstancePerLifetimeScope();
            containerBuilder.Register(ctx => payloadDataRefRepository).As<IPayloadDataRefRepository>().InstancePerLifetimeScope();
            containerBuilder.Register(ctx =>
            {
                var rsa = RSA.Create();
                var certificateRequest = new CertificateRequest("cn=foobar", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                var cert = certificateRequest.CreateSelfSigned(DateTime.Now, DateTime.Now.AddHours(1));
                return new SignProvider(cert, hashAlgorithmName: HashAlgorithmName.SHA512, signaturePadding: RSASignaturePadding.Pkcs1);
            }).As<ISignProvider>().SingleInstance();

            _applicationContainer = containerBuilder.Build();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            var payloadMetadataRepository = _applicationContainer.Resolve<IPayloadMetadataRepository>();
            payloadMetadataRepository.DeleteItemsAsync<PayloadMetadata<HandoffPayloadMetadata>>(c => string.IsNullOrEmpty(c.Id))
                .ConfigureAwait(false);

            var payloadRepository = _applicationContainer.Resolve<IPayloadRepository>();
            payloadRepository.DeleteItemsAsync<PayloadWithInlineMetadata<JObject>>(c => string.IsNullOrEmpty(c.Id))
                .ConfigureAwait(false);

            var payloadDataRefRepository = _applicationContainer.Resolve<IPayloadDataRefRepository>();
            payloadDataRefRepository.DeleteItemsAsync<PayloadDataReference<LocElementDataReferenceDescriptor>>(c => string.IsNullOrEmpty(c.Id))
                .ConfigureAwait(false);
        }
    }
}
