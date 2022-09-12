using Grpc.Core;
using GrpcSample;

namespace GrpcServer.Services;

public class BlockStreamService: BlockStreamClient.BlockStreamClientBase
{
    private readonly ILogger<BlockStreamService> _logger;
    public BlockStreamService(ILogger<BlockStreamService> logger)
    {
        _logger = logger;
    }
    
    public override async Task<StreamOperationResponse> BlockStreamOperation(
        IAsyncStreamReader<StartRequest> requestStream, 
        ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync())
        {
            if (!string.IsNullOrEmpty(request.BlockName))
            {
                _logger.LogInformation("Received: StartRequest, BlockName: {0}", request.BlockName);
            }
            if (request.Chunk?.Length > 0)
            {
                _logger.LogInformation("Received: Chunk, Payload ...: {0}", request.Chunk);
            }
        }
        _logger.LogInformation("Responding: StreamOperationResponse");
        return new StreamOperationResponse();
    }

}
