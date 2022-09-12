using Google.Protobuf;
using Grpc.Net.Client;
using GrpcSample;
using System.Text;

namespace GrpcClient;



public class Program
{
    static async Task Main(string[] args)
    {
        using var channel = GrpcChannel.ForAddress("https://localhost:7227");
        var client = new BlockStreamClient.BlockStreamClientClient(channel);
        var call = client.BlockStreamOperation();

        Console.WriteLine("Sending: StartRequest, BlockName: Foo");
        await call.RequestStream.WriteAsync(new StartRequest
        {
            BlockName = "Foo"
        });
        foreach (var data in _data)
        {
            var bytes = Encoding.ASCII.GetBytes(data);
            Console.WriteLine("Sending: Chunk, Payload ... {0}", BitConverter.ToString(bytes));
            await call.RequestStream.WriteAsync(new StartRequest
            {
                Chunk = UnsafeByteOperations.UnsafeWrap(bytes)
            });
        }
        
        await call.RequestStream.CompleteAsync();
        var response = await call;
        Console.WriteLine("Received: StreamOperationResponse");
        
        Console.WriteLine("Shutting down");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static List<string> _data = new()
    {
        "Lorem ipsum dolor sit amet", 
        "consectetur adipiscing elit", 
        "sed do eiusmod tempor incididunt", 
        "ut labore et dolore magna aliqua"
    };
}
