using FakeItEasy;

namespace Juno.Payload.Client.Mock.UnitTests.Data
{
    public class MockPayloadMetadata : IEquatable<MockPayloadMetadata>
    {
        public Guid PayloadId { get; set; }

        public string? Data { get; set; }

        public bool Equals(MockPayloadMetadata? other)
        {
            return other != null && PayloadId == other.PayloadId
                && Data != null && Data.Equals(other.Data);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MockPayloadMetadata);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PayloadId, Data);
        }
    }

    public class DummyPayloadMetadataFactory : DummyFactory<MockPayloadMetadata>
    {
        protected override MockPayloadMetadata Create()
        {
            return new MockPayloadMetadata
            {
                PayloadId = Guid.NewGuid(),
                Data = Random.Shared.Next().ToString()
            };
        }
    }
}
