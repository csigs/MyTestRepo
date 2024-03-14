// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
//    Defines constants of Payload.Services.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Juno.Payload.Client.IntegrationTests;

/// <summary>
/// Defines constants of Payload.Services.
/// </summary>
internal static class Constants
{
    #region Configuration names
    public const string PreReqKeyVaultUrlConfigName = "PreReqKeyVaultUrl";
    public const string MessageSignCertificateName = "MessageSignCertificate";
    public const string HashAlgorithmNameSecretName = "HashAlgorithmName";
    public const string RSASignaturePaddingSecretName = "RSASignaturePadding";
    #endregion
}
