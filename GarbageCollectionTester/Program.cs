using GarbageCollectionTester;
using System.Buffers;
using System.Diagnostics;

Console.WriteLine("Choose a demo to run:");
Console.WriteLine("1. Simulate Memory Leak");
Console.WriteLine("2. Simulate Excessive Allocations");
Console.WriteLine("3. Simulate Memory Leak Optimized");
Console.WriteLine("4. Simulate Excessive Allocations Optimized");
Console.WriteLine("5. Simulate Memory Leak Disposable pattern");
Console.Write("Enter your choice: ");

var choice = Console.ReadLine();

switch (choice)
{
    case "1":
        SimulateMemoryLeak();
        break;
    case "2":
        await SimulateExcessiveAllocationsAsync();
        break;
    case "3":
        SimulateMemoryLeakOptimized();
        break;
    case "4":
        await SimulateExcessiveAllocationsOptimizedAsync();
        break;
    case "5":
        SimulateMemoryLeakOptimizedRM();
        break;
    default:
        Console.WriteLine("Invalid choice.");
        break;
}

Console.WriteLine("Simulation complete.");
Console.ReadLine();

static void SimulateMemoryLeak()
{
    // A list to hold references and simulate a memory leak
    List<byte[]> memoryHog = new List<byte[]>();
    var cts = new CancellationTokenSource();
    var inputListener = StartInputListener(cts);

    int iteration = 0;

    try
    {
        while (!cts.Token.IsCancellationRequested)
        {
            // Allocate 10 MB chunks
            byte[] largeArray = new byte[10 * 1024 * 1024];
            for (int i = 0; i < largeArray.Length; i += 1024)
            {
                largeArray[i] = 0xFF; // Simulate some usage
            }

            // Keep a reference in the list to prevent GC from reclaiming it
            memoryHog.Add(largeArray);

            iteration++;
            Console.WriteLine($"Iteration {iteration}: Allocated 10 MB. Total allocations: {memoryHog.Count * 10} MB");

            // Introduce a short delay to simulate a real-world scenario
            Thread.Sleep(100);
        }
    }
    catch (OutOfMemoryException)
    {
        Console.WriteLine("OutOfMemoryException encountered! The program has exhausted available memory.");
    }
    finally
    {
        cts.Cancel(); // Ensure the listener task stops
        inputListener.Wait(); // Wait for the listener task to complete

        Console.WriteLine("Clearing allocations...");
        memoryHog.Clear();
        GC.Collect(); // Force garbage collection
        Console.WriteLine("Memory cleared.");
    }
}

static async Task SimulateExcessiveAllocationsAsync()
{
    // List to hold references and avoid immediate garbage collection
    var allocations = new List<byte[]>();
    var cts = new CancellationTokenSource();
    var inputListener = StartInputListener(cts);

    try
    {
        var random = new Random();
        for (int i = 0; i < 1_000_000; i++) // Adjust number for intensity
        {
            if (cts.Token.IsCancellationRequested)
            {
                break;
            }

            // Allocate a random size byte array (1 KB to 1 MB)
            var size = random.Next(1024, 1_048_576);
            var data = new byte[size];

            // Fill the array with some data
            for (int j = 0; j < data.Length; j++)
            {
                data[j] = (byte)(j % 256);
            }

            allocations.Add(data);

            if (i % 10_000 == 0)
            {
                Console.WriteLine($"Allocated {i} objects so far...");
                await Task.Delay(10); // Simulate work and allow context switching
            }
        }
    }
    catch (OutOfMemoryException)
    {
        Console.WriteLine("Out of memory exception encountered!");
    }
    finally
    {
        cts.Cancel(); // Ensure the listener task stops
        await inputListener; // Wait for the listener task to complete

        Console.WriteLine("Clearing allocations...");
        allocations.Clear();
        GC.Collect(); // Force garbage collection
        Console.WriteLine("Memory cleared.");
    }
}

static void SimulateMemoryLeakOptimized()
{
    // Use ArrayPool for memory management
    List<byte[]> memoryHog = new List<byte[]>();
    var pool = ArrayPool<byte>.Shared;
    var cts = new CancellationTokenSource();
    var inputListener = StartInputListener(cts);

    int iteration = 0;

    try
    {
        while (!cts.Token.IsCancellationRequested)
        {
            // Rent a buffer from the pool
            var buffer = pool.Rent(10 * 1024 * 1024);

            // Simulate usage with Span<T> to avoid allocations
            Span<byte> span = buffer.AsSpan();
            span.Fill(0xFF); // Simulate usage

            memoryHog.Add(buffer); // Keep reference

            iteration++;
            Console.WriteLine($"Iteration {iteration}: Allocated 10 MB. Total allocations: {memoryHog.Count * 10} MB");

            Thread.Sleep(100);
        }
    }
    catch (OutOfMemoryException)
    {
        Console.WriteLine("OutOfMemoryException encountered! The program has exhausted available memory.");
    }
    finally
    {
        cts.Cancel();
        inputListener.Wait();

        Console.WriteLine("Clearing allocations...");
        foreach (var buffer in memoryHog)
        {
            pool.Return(buffer); // Return buffers to the pool
        }
        memoryHog.Clear();
        GC.Collect();
        Console.WriteLine("Memory cleared.");
    }
}

static async Task SimulateExcessiveAllocationsOptimizedAsync()
{
    // Use a pool to manage allocations
    var pool = ArrayPool<byte>.Shared;
    var cts = new CancellationTokenSource();
    var inputListener = StartInputListener(cts);

    try
    {
        var random = new Random();
        for (int i = 0; i < 1_000_000; i++)
        {
            if (cts.Token.IsCancellationRequested)
            {
                break;
            }

            // Allocate a buffer using ArrayPool
            var size = random.Next(1024, 1_048_576);
            var buffer = pool.Rent(size);

            // Simulate usage with Span<T>
            Span<byte> span = buffer.AsSpan(0, size);
            for (int j = 0; j < span.Length; j++)
            {
                span[j] = (byte)(j % 256);
            }

            pool.Return(buffer);

            if (i % 10_000 == 0)
            {
                Console.WriteLine($"Processed {i} buffers so far...");
                await Task.Delay(10);
            }
        }
    }
    catch (OutOfMemoryException)
    {
        Console.WriteLine("Out of memory exception encountered!");
    }
    finally
    {
        cts.Cancel();
        await inputListener;

        GC.Collect();
        Console.WriteLine("Memory cleared.");
    }
}

static void SimulateMemoryLeakOptimizedRM()
{
    List<ResourceManager> resourceManagers = new List<ResourceManager>();
    using var cts = new CancellationTokenSource();
    var inputListener = StartInputListener(cts);

    try
    {
        while (!cts.Token.IsCancellationRequested)
        {
            var manager = new ResourceManager(10 * 1024 * 1024);
            manager.UseResource();

            resourceManagers.Add(manager);

            Console.WriteLine($"Resource allocated. Total resources: {resourceManagers.Count}");
            Thread.Sleep(100);
        }
    }
    catch (OutOfMemoryException)
    {
        Console.WriteLine("OutOfMemoryException encountered!");
    }
    finally
    {
        cts.Cancel();
        inputListener.Wait();

        Console.WriteLine("Disposing resources...");
        foreach (var manager in resourceManagers)
        {
            manager.Dispose();
        }

        resourceManagers.Clear();
        GC.Collect();
        Console.WriteLine("Resources cleared.");
    }
}


static Task StartInputListener(CancellationTokenSource cts)
{
    Console.WriteLine("Press 'c' at any time to stop the process.");
    return Task.Run(() =>
    {
        while (!cts.Token.IsCancellationRequested)
        {
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.C)
            {
                Console.WriteLine("User pressed 'c'. Exiting...");
                cts.Cancel();
            }
        }
    });
}
