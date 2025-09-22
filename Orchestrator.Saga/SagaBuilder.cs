using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Orchestrator.Saga;

/// <summary>
/// Fluent builder that turns a sequence of steps into an executable saga.
/// </summary>
public sealed class SagaBuilder
{
    private readonly List<ISagaStep> _steps = new();

    private SagaBuilder(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name of the saga.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Starts building a new saga.
    /// </summary>
    public static SagaBuilder Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Saga name must be provided", nameof(name));
        }

        return new SagaBuilder(name);
    }

    /// <summary>
    /// Adds a step to the saga.
    /// </summary>
    public SagaBuilder Step(string name, SagaStepAction execute, SagaStepAction? compensate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(execute);

        _steps.Add(new DelegateSagaStep(name, execute, compensate));
        return this;
    }

    /// <summary>
    /// Finalises the builder into an executable saga.
    /// </summary>
    public SagaOrchestrator Build()
    {
        if (_steps.Count == 0)
        {
            throw new InvalidOperationException("A saga must define at least one step before it can be executed.");
        }

        return new SagaOrchestrator(Name, _steps);
    }
}

/// <summary>
/// Entry point for executing a saga and obtaining diagnostics about its progress.
/// </summary>
public sealed class SagaOrchestrator
{
    private readonly IReadOnlyList<ISagaStep> _steps;

    internal SagaOrchestrator(string name, IReadOnlyList<ISagaStep> steps)
    {
        Name = name;
        _steps = steps;
    }

    /// <summary>
    /// Gets the name of the saga.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Executes the saga.
    /// </summary>
    public async Task<SagaExecutionResult> ExecuteAsync(SagaContext? context = null, CancellationToken cancellationToken = default)
    {
        var sagaContext = context ?? new SagaContext();
        var executedSteps = new Stack<ISagaStep>();
        var log = new List<SagaStepExecution>();

        foreach (var step in _steps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await step.ExecuteAsync(sagaContext, cancellationToken).ConfigureAwait(false);
                executedSteps.Push(step);
                log.Add(SagaStepExecution.Completed(step.Name));
            }
            catch (Exception ex)
            {
                log.Add(SagaStepExecution.Failed(step.Name, ex));
                var compensationError = await CompensateAsync(sagaContext, executedSteps, log, cancellationToken).ConfigureAwait(false);

                if (compensationError is not null)
                {
                    return SagaExecutionResult.Failure(Name, log, new AggregateException(ex, compensationError));
                }

                return SagaExecutionResult.Failure(Name, log, ex);
            }
        }

        return SagaExecutionResult.Success(Name, log);
    }

    private static async Task<Exception?> CompensateAsync(
        SagaContext context,
        Stack<ISagaStep> executedSteps,
        List<SagaStepExecution> log,
        CancellationToken cancellationToken)
    {
        SagaCompensationException? failure = null;

        while (executedSteps.Count > 0)
        {
            var step = executedSteps.Pop();

            try
            {
                await step.CompensateAsync(context, cancellationToken).ConfigureAwait(false);
                log.Add(SagaStepExecution.Compensated(step.Name));
            }
            catch (Exception compensationError)
            {
                log.Add(SagaStepExecution.CompensationFailed(step.Name, compensationError));
                failure ??= new SagaCompensationException(step.Name, compensationError);
            }
        }

        return failure;
    }
}
