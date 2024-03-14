using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Juno.Common.Contracts.Eol;
using Juno.Common.DataReference;
using Juno.Common.Metadata;
using Juno.Payload.Handoff;
using Juno.Payload.Service.Model;

namespace Juno.Payload.Service.UnitTests
{
    internal class MockDataProvider
    {
        internal static PayloadWithInlineMetadata<HandoffPayloadMetadata> CreateTestPayload()
        {
            return new PayloadWithInlineMetadata<HandoffPayloadMetadata>()
            {
                Category = "Handoff",
                CreatedTime = DateTimeOffset.UtcNow,
                UpdatedTime = DateTimeOffset.UtcNow,
                Metadata = new HandoffPayloadMetadata()
                {
                    Build = new Juno.Common.Contracts.BuildMetadata()
                    {
                        BuildLabel = "test",
                        BuildId = Guid.NewGuid()
                    },
                    Branch = new Juno.Common.Contracts.BranchMetadata()
                    {
                        BranchId = Guid.NewGuid()
                    }
                    ,
                    EolMapMetadata = new Juno.Common.Contracts.Eol.FullEolMapMetadata()
                    {
                        Lcgs = new List<LcgFullEolMap>()
                        {
                            new LcgFullEolMap()
                            {
                                LcgMetadata = new Juno.Common.Contracts.LcgMetadata()
                                {
                                    GroupId = Guid.NewGuid(),
                                    Id = Guid.NewGuid(),
                                    OriginalFileName = "test.lcg",
                                    OriginalRelativePath = "test\\test.lcg",
                                },
                                EolMap = new List<EolMap>()
                                {
                                    new EolMap()
                                    {
                                        CultureName = "ja-JP",
                                        LclMetadata = new Juno.Common.Contracts.LclMetadata()
                                        {
                                            Id = Guid.NewGuid(),
                                            GroupId = Guid.NewGuid(),
                                            OriginalFileName = "test.lcl",
                                            OriginalRelativePath = "test\\test.lcl"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }; ;
        }

        internal static IEnumerable<LocElementDataReferenceDescriptor> CreateTestDataReferenceDescriptors()
        {
            return new List<LocElementDataReferenceDescriptor>()
        {
            new LocElementDataReferenceDescriptor()
            {
                DataAccessDescriptor = new Juno.Common.DataReference.AccessSource.DataAccessDescriptor()
                {
                    DataAccessSourceId = Guid.NewGuid(),
                    RelativeAccessPath = "test\\test.lcl"
                },
                LocElementMetadata = new DefaultLocElementMetadata()
                                        {
                                            Id = Guid.NewGuid(),
                                            GroupId = Guid.NewGuid(),
                                            OriginalFileName = "test.lcl",
                                            OriginalRelativePath = "test\\test.lcl",
                                            LocDataElementType = LocElementDataType.Lcl
                                        }
            },
            new LocElementDataReferenceDescriptor()
            {
                DataAccessDescriptor = new Juno.Common.DataReference.AccessSource.DataAccessDescriptor()
                {
                    DataAccessSourceId = Guid.NewGuid(),
                    RelativeAccessPath = "test\\test.lcg"
                },
                LocElementMetadata = new DefaultLocElementMetadata()
                                {
                                    GroupId = Guid.NewGuid(),
                                    Id = Guid.NewGuid(),
                                    OriginalFileName = "test.lcg",
                                    OriginalRelativePath = "test\\test.lcg",
                                    LocDataElementType = LocElementDataType.Lcg
                                }
            },
            new LocElementDataReferenceDescriptor()
            {
                DataAccessDescriptor = new Juno.Common.DataReference.AccessSource.DataAccessDescriptor()
                {
                    DataAccessSourceId = Guid.NewGuid(),
                    RelativeAccessPath = "test\\test2.lcg"
                },
                LocElementMetadata = new DefaultLocElementMetadata()
                                {
                                    GroupId = Guid.NewGuid(),
                                    Id = Guid.NewGuid(),
                                    OriginalFileName = "test2.lcg",
                                    OriginalRelativePath = "test\\test2.lcg",
                                    LocDataElementType = LocElementDataType.Lcg
                                }
            },
            new LocElementDataReferenceDescriptor()
            {
                DataAccessDescriptor = new Juno.Common.DataReference.AccessSource.DataAccessDescriptor()
                {
                    DataAccessSourceId = Guid.NewGuid(),
                    RelativeAccessPath = "test\\test3.lcg"
                },
                LocElementMetadata = new DefaultLocElementMetadata()
                                {
                                    GroupId = Guid.NewGuid(),
                                    Id = Guid.NewGuid(),
                                    OriginalFileName = "test3.lcg",
                                    OriginalRelativePath = "test\\test3.lcg",
                                    LocDataElementType = LocElementDataType.Lcg
                                }
            },

        }; ;
        }
    }
}
