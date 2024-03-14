using FakeItEasy;

namespace Juno.Payload.Client.Mock.UnitTests.Data
{
    public class MockPayloadData : IEquatable<MockPayloadData>
    {
        public Guid Id { get; set; }

        public string? Data { get; set; }

        public bool Equals(MockPayloadData? other)
        {
            return other != null && Id == other.Id
                && Data != null && Data.Equals(other.Data);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MockPayloadData);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Data);
        }
    }

    public class DummyMockPayloadDataFactory : DummyFactory<MockPayloadData>
    {
        protected override MockPayloadData Create()
        {
            return new MockPayloadData { 
                Id = Guid.NewGuid(),
                Data = Random.Shared.Next().ToString()
            };
        }
    }
}
