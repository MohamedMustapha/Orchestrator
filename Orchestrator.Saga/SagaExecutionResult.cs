using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Orchestrator.Saga;

/// <summary>
/// Describes the outcome of a saga execution.
/// </summary>
public sealed record SagaExecutionResult
{
    private SagaExecutionResult(string sagaName, bool succeeded, Exception? error, ImmutableArray<SagaStepExecution> steps)
    {
        SagaName = sagaName;
        Succeeded = succeeded;
        Error = error;
        Steps = steps;
    }

    /// <summary>
    /// Name of the saga that was executed.
    /// </summary>
    public string SagaName { get; }

    /// <summary>
    /// Indicates whether the saga completed successfully.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// When the saga failed, contains the originating error.
    /// </summary>
    public Exception? Error { get; }

    /// <summary>
    /// Ordered history of each step execution (including compensations).
    /// </summary>
    public ImmutableArray<SagaStepExecution> Steps { get; }

    internal static SagaExecutionResult Success(string sagaName, IEnumerable<SagaStepExecution> steps) =>
        new(sagaName, succeeded: true, error: null, steps.ToImmutableArray());

    internal static SagaExecutionResult Failure(string sagaName, IEnumerable<SagaStepExecution> steps, Exception error) =>
        new(sagaName, succeeded: false, error, steps.ToImmutableArray());
}

/// <summary>
/// Details for the execution of a single saga step.
/// </summary>
public sealed record SagaStepExecution
{
    private SagaStepExecution(string stepName, SagaStepOutcome outcome, Exception? error)
    {
        StepName = stepName;
        Outcome = outcome;
        Error = error;
    }

    public string StepName { get; }

    public SagaStepOutcome Outcome { get; }

    public Exception? Error { get; }

    public static SagaStepExecution Completed(string stepName) => new(stepName, SagaStepOutcome.Completed, null);

    public static SagaStepExecution Failed(string stepName, Exception error) => new(stepName, SagaStepOutcome.Faulted, error);

    public static SagaStepExecution Compensated(string stepName) => new(stepName, SagaStepOutcome.Compensated, null);

    public static SagaStepExecution CompensationFailed(string stepName, Exception error) => new(stepName, SagaStepOutcome.CompensationFailed, error);
}

public enum SagaStepOutcome
{
    Completed,
    Faulted,
    Compensated,
    CompensationFailed
}
