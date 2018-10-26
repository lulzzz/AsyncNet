namespace AsyncNet.Tcp
{
    public class MessageDefragmenter
    {
        private readonly MessageDefragmenterConfig confg;
        private readonly IAsyncTcpServer asyncTcpServer;

        public MessageDefragmenter(
            IDefragmentationStrategy defragmentationStrategy,
            IAsyncTcpServer asyncTcpServer) : this(
                new MessageDefragmenterConfig()
                {
                    DefragmentationStrategy = defragmentationStrategy
                }, asyncTcpServer)
        {
            
        }

        public MessageDefragmenter(
            MessageDefragmenterConfig config, 
            IAsyncTcpServer asyncTcpServer)
        {
            this.confg = new MessageDefragmenterConfig()
            {
                DefragmentationStrategy = confg.DefragmentationStrategy
            };
            this.asyncTcpServer = asyncTcpServer;
        }
    }
}
