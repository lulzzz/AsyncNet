using System.IO;
using System.Threading;
using Xunit;
using Moq;
using AsyncNet.Tcp.Defragmentation;
using AsyncNet.Tcp.Remote;
using System.Linq;
using System;

namespace AsyncNet.Tcp.Tests
{
    public class MixedDefragmenterTests
    {
        [Fact]
        public async void ReadFrameAsync_DefragmentationStrategyReturnsMinusOne_ShouldReturnDroppedReadFrameStatus()
        {
            // Arrange
            var strategy = this.SetupMixedDefragmentationStrategy(MixedDefragmentationStrategyReadType.ReadDefault, 1024, () => -1);
            var defragmenter = new MixedDefragmenter(strategy);
            var remotePeer = this.SetupRemotePeer(new byte[] { 1, 2, 3, 4 });

            // Act
            var result = await defragmenter.ReadFrameAsync(remotePeer, null, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(ReadFrameStatus.FrameDropped, result.ReadFrameStatus);
        }

        [Fact]
        public async void ReadFrameAsync_RemotePeerTcpStreamWithNoData_ShouldReturnStreamClosedReadFrameStatus()
        {
            // Arrange
            var strategy = this.SetupMixedDefragmentationStrategy(MixedDefragmentationStrategyReadType.ReadDefault, 1024, () => 0);
            var defragmenter = new MixedDefragmenter(strategy);
            var remotePeer = this.SetupRemotePeer(new byte[] {});

            // Act
            var result = await defragmenter.ReadFrameAsync(remotePeer, null, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(ReadFrameStatus.StreamClosed, result.ReadFrameStatus);
        }

        [Fact]
        public async void ReadFrameAsync_ReadDefaultDefragmentationStrategyGetFrameLengthReturnsOneAndTcpStreamHasOneByte_ShouldReturnSuccessReadFrameStatus()
        {
            // Arrange
            var strategy = this.SetupMixedDefragmentationStrategy(MixedDefragmentationStrategyReadType.ReadDefault, 1, () => 1);
            var defragmenter = new MixedDefragmenter(strategy);
            var remotePeer = this.SetupRemotePeer(new byte[] { 1 });

            // Act
            var result = await defragmenter.ReadFrameAsync(remotePeer, null, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(ReadFrameStatus.Success, result.ReadFrameStatus);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1)]
        public async void ReadFrameAsync_ReadDefaultDefragmentationStrategyGetFrameLengthReturnsAsManyBytesAsThereIsInTcpStream_ShouldReturnFrameDataWithLengthEqualToTheNumberOfBytesInTcpStream(int numberOfBytes)
        {
            // Arrange
            var strategy = this.SetupMixedDefragmentationStrategy(MixedDefragmentationStrategyReadType.ReadDefault, 1024, () => numberOfBytes);
            var defragmenter = new MixedDefragmenter(strategy);
            var remotePeer = this.SetupRemotePeer(Enumerable.Repeat<byte>(1, numberOfBytes).ToArray());

            // Act
            var result = await defragmenter.ReadFrameAsync(remotePeer, null, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(numberOfBytes, result.FrameData.Length);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1)]
        public async void ReadFrameAsync_ReadDefaultDefragmentationStrategyAndSomeNumberOfBytesInTcpStream_ShouldReadAsManyNumberOfBytesAsThereIsInTcpStream(int numberOfBytes)
        {
            // Arrange
            var strategy = this.SetupMixedDefragmentationStrategy(MixedDefragmentationStrategyReadType.ReadDefault, 1024, () => numberOfBytes);
            var defragmenter = new MixedDefragmenter(strategy);
            var remotePeer = this.SetupRemotePeer(Enumerable.Repeat<byte>(1, numberOfBytes).ToArray());

            // Act
            var result = await defragmenter.ReadFrameAsync(remotePeer, null, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(numberOfBytes, result.FrameData.Length);
        }

        [Fact]
        public async void ReadFrameAsync_ReadFullyDefragmentationStrategyGetFrameLengthReturnsOneAndTcpStreamHasOneByte_ShouldReturnSuccessReadFrameStatus()
        {
            // Arrange
            var strategy = this.SetupMixedDefragmentationStrategy(MixedDefragmentationStrategyReadType.ReadDefault, 1, () => 1);
            var defragmenter = new MixedDefragmenter(strategy);
            var remotePeer = this.SetupRemotePeer(new byte[] { 1 });

            // Act
            var result = await defragmenter.ReadFrameAsync(remotePeer, null, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Equal(ReadFrameStatus.Success, result.ReadFrameStatus);
        }

        private IRemoteTcpPeer SetupRemotePeer(byte[] data)
        {
            var mock = new Mock<IRemoteTcpPeer>();
            mock.Setup(x => x.TcpStream).Returns(() => new MemoryStream(data));

            return mock.Object;
        }

        private IMixedDefragmentationStrategy SetupMixedDefragmentationStrategy(
            MixedDefragmentationStrategyReadType readType,
            int bufferLength,
            Func<int> frameLength)
        {
            var mock = new Mock<IMixedDefragmentationStrategy>();

            mock.Setup(x => x.ReadType).Returns(readType);
            mock.Setup(x => x.ReadBufferLength).Returns(bufferLength);
            mock.Setup(x => x.GetFrameLength(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(frameLength);

            return mock.Object;
        }
    }
}
