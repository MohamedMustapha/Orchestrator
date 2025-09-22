using System;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestrator.Saga;

/// <summary>
/// Represents an executable unit in a saga.
/// </summary>
public interface ISagaStep
{
    string Name { get; }

    ValueTask ExecuteAsync(SagaContext context, CancellationToken cancellationToken);

    ValueTask CompensateAsync(SagaContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Delegate signature for saga step operations.
/// </summary>
/// <param name="context">The current <see cref="SagaContext"/>.</param>
/// <param name="cancellationToken">Cancellation token.</param>
public delegate ValueTask SagaStepAction(SagaContext context, CancellationToken cancellationToken);

internal sealed class DelegateSagaStep : ISagaStep
{
    private readonly SagaStepAction _execute;
    private readonly SagaStepAction _compensate;

    public DelegateSagaStep(string name, SagaStepAction execute, SagaStepAction? compensate)
    {
        Name = name;
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _compensate = compensate ?? ((_, _) => ValueTask.CompletedTask);
    }

    public string Name { get; }

    public ValueTask ExecuteAsync(SagaContext context, CancellationToken cancellationToken) => _execute(context, cancellationToken);

    public ValueTask CompensateAsync(SagaContext context, CancellationToken cancellationToken) => _compensate(context, cancellationToken);
}
