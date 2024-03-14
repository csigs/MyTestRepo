// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//    Defines constants of Payload.Services.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Juno.Payload.Service.UnitTests")]
namespace Juno.Payload.Service;

/// <summary>
/// Defines constants of Payload.Services.
/// </summary>
internal static class Constants
{
    /// <summary>
    /// Collection id for payload collection.
    /// </summary>
    public const string PayloadCollectionId = "Payloads";

    /// <summary>
    /// Collection id for DataReference collection.
    /// </summary>
    public const string DefaultDataReferenceCollectionId = "DataRefs";

    /// <summary>
    /// Collection id for Metadata collection.
    /// </summary>
    public const string DefaultMetadataCollectionId = "Metadata";

    public const string PayloadInlineMetadataAsBlobAttachmentType = "BlobAttachment";

    #region Configuration names
    public const string PreReqKeyVaultUrlConfigName = "PreReqKeyVaultUrl";
    public const string MessageSignCertificateName = "MessageSignCertificate";
    public const string HashAlgorithmNameSecretName = "HashAlgorithmName";
    public const string RSASignaturePaddingSecretName = "RSASignaturePadding";
    #endregion

    #region Http Parameter

    public const string CategoryParamName = "category";

    public const string ContinuationTokenParamName = "continuationToken";

    public const string PartitionKeyParamName = "partitionKey";

    public const string PayloadDataTypeParamName = "payloadDataType";

    public const string PayloadIdParamName = "id";

    public const string RequireSignQueryParamName = "sign";

    public const string WithPayloadDataParamName = "withPayloadData";

    public const string SignatureHttpHeader = "Signature";

    #endregion

    #region Function names

    public const string AddPayloadDataReferencesV2 = "AddPayloadDataReferencesV2";

    public const string AddPayloadMetadataCollectionItemsV2 = "AddPayloadMetadataCollectionItemsV2";

    public const string CreatePayloadV1 = "CreatePayload";

    public const string CreatePayloadV2 = "CreatePayloadV2";

    public const string DeletePayloadV1 = "DeletePayload";

    public const string DeletePayloadV2 = "DeletePayloadV2";

    public const string DeletePayloadDataReferencesV1 = "DeletePayloadDataReferences";

    public const string DeletePayloadDataReferencesV2 = "DeletePayloadDataReferencesV2";

    public const string DeletePayloadMetadataV1 = "DeletePayloadMetadata";

    public const string DeletePayloadMetadataV2 = "DeletePayloadMetadataV2";

    public const string GetPayloadV1 = "GetPayload";

    public const string GetPayloadV2 = "GetPayloadV2";

    public const string GetPayloadInlineMetadataV1 = "GetPayloadInlineMetadata";

    public const string GetPayloadMetadataV1 = "GetPayloadMetadata";

    public const string GetPayloadMetadataByIdV1 = "GetPayloadMetadataById";

    public const string GeStoredPayloadDataReferencesV1 = "GeStoredPayloadDataReferences";

    public const string GetStoredPayloadDataReferenceByProvidedIdsV1 = "GetStoredPayloadDataReferenceByProvidedIds";

    public const string ReadPayloadDataReferencesV2 = "ReadPayloadDataReferencesV2";

    public const string ReadPayloadDataV2 = "ReadPayloadDataV2";

    public const string ReadPayloadMetadataCollectionItemsByIdV2 = "ReadPayloadMetadataCollectionItemsByIdV2";

    public const string ReadPayloadMetadataCollectionV2 = "ReadPayloadMetadataCollectionV2";

    public const string UpdatePayloadDataV2 = "UpdatePayloadDataV2";

    public const string UpdatePayloadInlineMetadataV1 = "UpdatePayloadInlineMetadata";

    public const string UpdatePayloadMetadataV1 = "UpdatePayloadMetadata";

    public const string UpdatePayloadMetadataCollectionItemsV2 = "UpdatePayloadMetadataCollectionItemsV2";

    public const string UploadPayloadDataReferencesV1 = "UploadPayloadDataReference";

    public const string UploadPayloadMetadataV1 = "UploadPayloadMetadata";

    #endregion
}
