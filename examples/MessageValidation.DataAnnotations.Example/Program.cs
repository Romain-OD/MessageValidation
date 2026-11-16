using System.ComponentModel.DataAnnotations;
using MessageValidation;
using MessageValidation.DataAnnotations;
using MessageValidation.Examples;
using Microsoft.Extensions.DependencyInjection;

var demo = new Demo(expectedValid: 1, expectedInvalid: 1);

await using var services = new ServiceCollection()
    .AddMessageValidation(options =>
    {
        options.MapSource<OrderPlaced>("orders/placed");
        options.DefaultFailureBehavior = FailureBehavior.Custom;
    })
    .AddMessageDataAnnotationsValidation() // validates any message type from its [attributes]
    .AddDemo(demo)
    .BuildServiceProvider();

var pipeline = services.GetRequiredService<IMessageValidationPipeline>();

await pipeline.ProcessAsync(Demo.Context("orders/placed", new OrderPlaced { OrderId = "A-1001", Quantity = 2, CustomerEmail = "ada@example.com" }));
await pipeline.ProcessAsync(Demo.Context("orders/placed", new OrderPlaced { OrderId = "", Quantity = 0, CustomerEmail = "not-an-email" }));

return await demo.WaitAsync();

public sealed record OrderPlaced
{
    [Required]
    public string OrderId { get; init; } = "";

    [Range(1, 100)]
    public int Quantity { get; init; }

    [Required, EmailAddress]
    public string CustomerEmail { get; init; } = "";
}
