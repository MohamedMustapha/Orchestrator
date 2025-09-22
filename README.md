# Orchestrator

This repository contains a minimal saga orchestration library that focuses on removing boilerplate when composing distributed workflows.

## Creating a saga

```csharp
var saga = SagaBuilder
    .Create("Order checkout")
    .Step(
        "Reserve inventory",
        async (ctx, ct) =>
        {
            var reservationId = await inventory.ReserveAsync(orderId, ct);
            ctx.Set("reservationId", reservationId);
        },
        async (ctx, ct) =>
        {
            if (ctx.TryGet<Guid>("reservationId", out var reservationId))
            {
                await inventory.ReleaseAsync(reservationId, ct);
            }
        })
    .Step(
        "Charge payment",
        async (ctx, ct) =>
        {
            var paymentId = await payments.CaptureAsync(orderId, ct);
            ctx.Set("paymentId", paymentId);
        },
        async (ctx, ct) =>
        {
            if (ctx.TryGet<Guid>("paymentId", out var paymentId))
            {
                await payments.RefundAsync(paymentId, ct);
            }
        })
    .Build();

var result = await saga.ExecuteAsync();
```

The `SagaBuilder` captures steps, executes them sequentially, and automatically triggers compensation when an exception is raised. The provided `SagaContext` allows steps to share state without additional plumbing, ensuring minimal boilerplate.

## Inspecting execution details

Execution results include a chronological log of step outcomes:

```csharp
if (!result.Succeeded)
{
    foreach (var entry in result.Steps)
    {
        Console.WriteLine($"{entry.StepName}: {entry.Outcome}");
    }
}
```

This makes it easy to observe which steps succeeded, failed, or required compensation.

