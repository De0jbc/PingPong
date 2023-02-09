using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using GrainInterfaces;
using Orleans;
using Client;



try
{
    using IHost host = await StartClientAsync();
    var client = host.Services.GetRequiredService<IClusterClient>();  
        await DoClientWorkAsync(client);
    
    Console.ReadKey();
    await host.StopAsync();

    return 0;
}
catch (Exception e)
{
    Console.WriteLine($$"""
        Exception while trying to run client: {{e.Message}}
        Make sure the silo the client is trying to connect to is running.
        Press any key to exit.
        """);

    Console.ReadKey();
    return 1;
}

static async Task<IHost> StartClientAsync()
{
    var builder = new HostBuilder()
        .UseOrleansClient(client =>
        {
            client.UseLocalhostClustering();
        })
        .ConfigureLogging(logging => logging.AddConsole());

    var host = builder.Build();
    await host.StartAsync();

    Console.WriteLine("Client successfully connected to silo host \n");

    return host;
}

static async Task DoClientWorkAsync(IClusterClient client)
{
    int numberOfClients;
    int numberOfRepeatsPerClient;

    

    Benchmark benchmark;


    
        Console.WriteLine("Make sure that local silo is started. Press Enter to proceed ...");
        Console.ReadLine();

        Console.Write("Enter number of clients (thousands): ");
        numberOfClients = int.Parse(Console.ReadLine() ?? Environment.ProcessorCount.ToString("D"));

        Console.Write("Enter number of repeated pings per client (thousands): ");
        numberOfRepeatsPerClient = int.Parse(Console.ReadLine() ?? "15");
    
        benchmark = new Benchmark(numberOfClients*1000, numberOfRepeatsPerClient*1000, client, client, client);
        benchmark.Run();

}