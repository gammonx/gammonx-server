using GammonX.DynamoDb.Items;

using GammonX.Models.Contracts;

using GammonX.Lambda.Extensions;

using Xunit;

namespace GammonX.Lambda.Tests.Contracts
{
    public class PlayerRecordContractExtTests
    {
        [Fact]
        public void ToPlayerShouldMapIdAndUserName()
        {
            var contract = new PlayerRecordContract
            {
                Id = Guid.NewGuid(),
                UserName = "TestUser"
            };

            var result = contract.ToPlayer();

            Assert.Equal(contract.Id, result.Id);
            Assert.Equal(contract.UserName, result.UserName);
        }

        [Fact]
        public void ToPlayerShouldSetCreatedAtCloseToNow()
        {
            var before = DateTime.UtcNow;
            var contract = new PlayerRecordContract { Id = Guid.NewGuid(), UserName = "User" };

            var result = contract.ToPlayer();
            var after = DateTime.UtcNow;

            Assert.True(result.CreatedAt >= before && result.CreatedAt <= after);
        }

        [Fact]
        public void ToPlayerShouldGenerateValidPK()
        {
            var contract = new PlayerRecordContract
            {
                Id = Guid.NewGuid(),
                UserName = "User"
            };

            var result = contract.ToPlayer();

            // construct expected PK using same factory
            var expectedPK = string.Format(ItemFactoryCreator.Create<PlayerItem>().PKFormat, contract.Id);

            Assert.Equal(expectedPK, result.PK);
        }

        [Fact]
        public void ToPlayerShouldGenerateValidSK()
        {
            var contract = new PlayerRecordContract
            {
                Id = Guid.NewGuid(),
                UserName = "User"
            };

            var result = contract.ToPlayer();

            // construct expected SK using factory
            var expectedSK = ItemFactoryCreator.Create<PlayerItem>().SKPrefix;

            Assert.Equal(expectedSK, result.SK);
        }

        [Fact]
        public void ToPlayerShouldNotModifySourceObject()
        {
            var contract = new PlayerRecordContract
            {
                Id = Guid.NewGuid(),
                UserName = "Original"
            };

            var result = contract.ToPlayer();

            Assert.Equal("Original", contract.UserName);
            Assert.Equal(result.Id, contract.Id);
        }

        [Fact]
        public void ToPlayerShouldHandleEmptyContract()
        {
            var contract = new PlayerRecordContract();

            var result = contract.ToPlayer();

            Assert.Equal(Guid.Empty, result.Id);
            Assert.Equal(string.Empty, result.UserName);
            Assert.NotEqual(default, result.CreatedAt);
        }
    }
}
