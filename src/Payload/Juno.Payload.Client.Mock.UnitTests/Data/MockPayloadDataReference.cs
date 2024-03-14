using System.Collections;
using FakeItEasy;
using Juno.Common.DataReference;
using Juno.Common.DataReference.AccessSource;
using Juno.Common.Metadata;

namespace Juno.Payload.Client.Mock.UnitTests.Data
{
    public class DummyLocElementDataReferenceDescriptorFactory : DummyFactory<LocElementDataReferenceDescriptor>
    {
        protected override LocElementDataReferenceDescriptor Create()
        {
            return new LocElementDataReferenceDescriptor
            {
                DataAccessDescriptor = A.Dummy<DataAccessDescriptor>(),
                LocElementMetadata = A.Dummy<DefaultLocElementMetadata>()
            };
        }
    }

    public class DummytLocElementMetadataFactory : DummyFactory<DefaultLocElementMetadata>
    {
        protected override DefaultLocElementMetadata Create()
        {
            var fileName = Random.Shared.Next().ToString();
            return new DefaultLocElementMetadata
            {
                GroupId = Guid.NewGuid(),
                Id = Guid.NewGuid(),
                OriginalFileName = fileName,
                LocDataElementType = LocElementDataType.Binary,
                OriginalRelativePath = $"path/to/{fileName}"
            };
        }
    }

    public class DummyDataAccessDescriptorFactory : DummyFactory<DataAccessDescriptor>
    {
        protected override DataAccessDescriptor Create()
        {
            return new DataAccessDescriptor
            {
                DataAccessSourceId = Guid.NewGuid(),
                RelativeAccessPath = $"path/to/{Random.Shared.Next()}"
            };
        }
    }

    public class LocElementDataReferenceDescriptorComparer : IComparer
    {
        public int Compare(object? x, object? y)
        {
            if (x == null || y == null)
            {
                return -1;
            }

            if (x is not LocElementDataReferenceDescriptor || y is not LocElementDataReferenceDescriptor)
            {
                return -1;
            }

            var xe = (LocElementDataReferenceDescriptor)x;
            var ye = (LocElementDataReferenceDescriptor)y;

            return xe.DataAccessDescriptor.DataAccessSourceId.Equals(ye.DataAccessDescriptor.DataAccessSourceId)
                && xe.DataAccessDescriptor.RelativeAccessPath.Equals(ye.DataAccessDescriptor.RelativeAccessPath)
                && xe.LocElementMetadata.Id.Equals(ye.LocElementMetadata.Id)
                && xe.LocElementMetadata.OriginalFileName.Equals(ye.LocElementMetadata.OriginalFileName)
                && xe.LocElementMetadata.OriginalRelativePath.Equals(ye.LocElementMetadata.OriginalRelativePath)
                ? 0 : -1;
        }
    }
}
