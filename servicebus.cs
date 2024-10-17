using Azure.Messaging.ServiceBus;
using System;
using System.Text.Json;
using System.Threading.Tasks;

public class Order
{
    public string OrderId { get; set; }
    public string CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
}

public class Program
{
    // connection string for Service Bus namespace
    static string connectionString = "<your_connection_string>";

    // name of  Service Bus queue
    static string queueName = "orderqueue";

    static async Task Main()
    {
        // Sending a message
        await SendMessageAsync(new Order
        {
            OrderId = "12345",
            CustomerName = "John Doe",
            TotalAmount = 99.99m
        });

        // Receiving and process messages
        await ReceiveMessagesAsync();
    }

    static async Task SendMessageAsync(Order order)
    {
        await using var client = new ServiceBusClient(connectionString);
        var sender = client.CreateSender(queueName);

        var message = new ServiceBusMessage(JsonSerializer.Serialize(order));
        await sender.SendMessageAsync(message);

        Console.WriteLine($"Sent order with ID: {order.OrderId}");
    }

    static async Task ReceiveMessagesAsync()
    {
        await using var client = new ServiceBusClient(connectionString);

        var options = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false
        };

        await using var processor = client.CreateProcessor(queueName, options);

        processor.ProcessMessageAsync += MessageHandler;
        processor.ProcessErrorAsync += ErrorHandler;

        await processor.StartProcessingAsync();

        Console.WriteLine("Wait for a minute and then press any key to end the processing");
        Console.ReadKey();

        await processor.StopProcessingAsync();
    }

    static async Task MessageHandler(ProcessMessageEventArgs args)
    {
        string body = args.Message.Body.ToString();
        var order = JsonSerializer.Deserialize<Order>(body);

        Console.WriteLine($"Received: OrderId={order.OrderId}, CustomerName={order.CustomerName}, TotalAmount={order.TotalAmount}");

        // Oeder processing logic here

        // Complete the message
        await args.CompleteMessageAsync(args.Message);
    }

    static Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Console.WriteLine($"Error: {args.Exception.Message}");
        return Task.CompletedTask;
    }
}
