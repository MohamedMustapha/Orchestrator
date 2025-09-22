using System;

namespace Orchestrator.Saga;

/// <summary>
/// Exception raised when a compensation handler fails while unwinding a saga.
/// </summary>
public sealed class SagaCompensationException : Exception
{
    public SagaCompensationException(string stepName, Exception inner)
        : base($"Compensation for step '{stepName}' failed.", inner)
    {
        StepName = stepName;
    }

    public string StepName { get; }
}
