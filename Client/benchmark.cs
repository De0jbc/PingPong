using GrainInterfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace Client;

    public class Benchmark
    {
        const string resultsFile = @"C:\Temp\OrleansPingPong.txt";

        readonly List<IClient> clients = new List<IClient>();
        readonly List<IClientObserver> observers = new List<IClientObserver>();

        int numberOfClients;
        int numberOfRepeatsPerClient;
        IClusterClient ClientFactory;
        IClusterClient DestinationFactory;
        IClusterClient ClientObserverFactory;

    public void SetBenchmark(int numberOfClients, int numberOfRepeatsPerClient, IClusterClient ClientFactory, IClusterClient DestinationFactory, IClusterClient ClientObserverFactory)
    {
        this.numberOfClients = numberOfClients;
        this.numberOfRepeatsPerClient = numberOfRepeatsPerClient;
        this.ClientFactory = ClientFactory;
        this.DestinationFactory = DestinationFactory;
        this.ClientObserverFactory = ClientObserverFactory;

    }
    public Benchmark(int numberOfClients, int numberOfRepeatsPerClient,IClusterClient ClientFactory, IClusterClient DestinationFactory, IClusterClient ClientObserverFactory)
        {
            this.numberOfClients = numberOfClients;
            this.numberOfRepeatsPerClient = numberOfRepeatsPerClient;
            this.ClientFactory = ClientFactory;
            this.DestinationFactory = DestinationFactory;
            this.ClientObserverFactory = ClientObserverFactory;

    }

        public async void Run()
        {

       
         
            var results = new List<Task<ClientResult>>();

            for (var i = 0; i < numberOfClients; i++)
            {
                var destination = DestinationFactory.GetGrain<IDestination>(0);
                var client = ClientFactory.GetGrain<IClient>(0);

                await client.Initialize(destination, numberOfRepeatsPerClient);

                var observer = new ClientObserver();
                await client.Subscribe(ClientObserverFactory.CreateObjectReference<IClientObserver>(observer));

                clients.Add(client);
                observers.Add(observer);
                results.Add(observer.AsTask());
            
        }
            Console.WriteLine("Init. Done!.");
            clients.ForEach(c => c.Run());
            var stopwatch = Stopwatch.StartNew();
            await Task.WhenAll(results.ToArray());
            stopwatch.Stop();

            WriteResultsToConsole(stopwatch);
            WriteResultsToFile(stopwatch);

            Console.WriteLine();
            Console.WriteLine("Done!. Press any key to exit ...");
            Console.ReadLine();
        
        
    }

        void WriteResultsToConsole(Stopwatch stopwatch)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            WriteResults(stopwatch, Console.Out);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        void WriteResultsToFile(Stopwatch stopwatch)
        {
            using (var writer = File.AppendText(resultsFile))
                WriteResults(stopwatch, writer);
        }

        void WriteResults(Stopwatch stopwatch, TextWriter writer)
        {
            var totalActorCount = numberOfClients * 2L; // ping and pong actor
            var totalMessagesReceived = numberOfRepeatsPerClient * totalActorCount * 2; // communication in Orleans' is always two-way
            var throughput = (totalMessagesReceived / stopwatch.Elapsed.TotalSeconds);

            writer.WriteLine("OSVersion: {0}", Environment.OSVersion);
            writer.WriteLine("ProcessorCount: {0}", Environment.ProcessorCount);
            writer.WriteLine("ClockSpeed: {0} MHZ", CpuSpeed());
            writer.WriteLine();
            writer.WriteLine("Number of Clients: {0}", numberOfClients);
            writer.WriteLine("Number of Repeats per Client: {0}", numberOfRepeatsPerClient);
            writer.WriteLine("Actor Count: {0}", totalActorCount);
            writer.WriteLine("Total: {0:N} messages", totalMessagesReceived);
            writer.WriteLine("Time: {0:F} sec", stopwatch.Elapsed.TotalSeconds);
            writer.WriteLine("TPS: {0:##,###} per/sec", throughput);
        }

        static uint CpuSpeed()
        {
            using (var mo = new ManagementObject("Win32_Processor.DeviceID='CPU0'"))
                return (uint)(mo["CurrentClockSpeed"]);
        }
    }

    public class ClientObserver : IClientObserver
    {
        readonly TaskCompletionSource<ClientResult> tcs = new TaskCompletionSource<ClientResult>();

    public void Done(long pings, long pongs)
    {
         
             tcs.TrySetResult(new ClientResult(pings, pongs));
           
    }

        public Task<ClientResult> AsTask()
        {
            return tcs.Task;
        }
    }

    public class ClientResult
    {
        public readonly long Pings;
        public readonly long Pongs;

        public ClientResult(long pings, long pongs)
        {
            Pings = pings;
            Pongs = pongs;
        }
    }