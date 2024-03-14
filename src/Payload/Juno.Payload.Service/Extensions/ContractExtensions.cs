namespace Juno.Payload.Service.Extensions;
using System;

using Juno.Payload.Dto;
using Juno.Payload.Service.Model;

internal static class ContractExtensions
{
    internal static PayloadDataType ToDomain(this PayloadDataStorageTypeDto payloadDataTypeDto)
    {
        switch (payloadDataTypeDto)
        {
            case PayloadDataStorageTypeDto.BlobAttachment:
                return PayloadDataType.BlobAttachment;
            default:
                throw new ArgumentOutOfRangeException(payloadDataTypeDto.ToString());
        }
    }
}
