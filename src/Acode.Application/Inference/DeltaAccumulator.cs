namespace Acode.Application.Inference;

using System;
using System.Text;
using Acode.Domain.Models.Inference;

/// <summary>
/// Mutable accumulator for building complete ChatResponse from streaming ResponseDelta chunks.
/// </summary>
/// <remarks>
/// FR-066: DeltaAccumulator MUST be defined as a mutable class for building responses.
/// FR-067 to FR-077: Append(), Build(), Current, DeltaCount, thread-safety.
/// </remarks>
public sealed class DeltaAccumulator
{
    private readonly StringBuilder contentBuilder = new();
    private readonly StringBuilder toolCallBuilder = new();
    private readonly object lockObject = new();
    private FinishReason? finishReason;
    private UsageInfo? usage;
    private int deltaCount;

    /// <summary>
    /// Gets the number of deltas appended so far.
    /// </summary>
    /// <remarks>
    /// FR-074: DeltaAccumulator MUST track delta count for debugging.
    /// </remarks>
    public int DeltaCount
    {
        get
        {
            lock (this.lockObject)
            {
                return this.deltaCount;
            }
        }
    }

    /// <summary>
    /// Gets the accumulated tool call content.
    /// </summary>
    public string ToolCallContent
    {
        get
        {
            lock (this.lockObject)
            {
                return this.toolCallBuilder.ToString();
            }
        }
    }

    /// <summary>
    /// Gets the current partial response state.
    /// </summary>
    /// <remarks>
    /// FR-075: DeltaAccumulator MUST provide Current property for partial response access.
    /// </remarks>
    public PartialResponse? Current
    {
        get
        {
            lock (this.lockObject)
            {
                if (this.deltaCount == 0)
                {
                    return null;
                }

                return new PartialResponse(
                    this.contentBuilder.ToString(),
                    this.finishReason,
                    this.usage);
            }
        }
    }

    /// <summary>
    /// Appends a streaming delta to the accumulator.
    /// </summary>
    /// <param name="delta">The delta to append.</param>
    /// <remarks>
    /// FR-067: DeltaAccumulator MUST provide Append(ResponseDelta) method.
    /// FR-068: Concatenate ContentDelta strings efficiently (StringBuilder).
    /// FR-069: Merge ToolCallDelta by Index into complete ToolCalls.
    /// FR-070: Capture final FinishReason from last delta.
    /// FR-071: Capture final Usage from last delta.
    /// FR-076: Thread-safe for concurrent Append calls.
    /// </remarks>
    public void Append(ResponseDelta delta)
    {
        ArgumentNullException.ThrowIfNull(delta);

        lock (this.lockObject)
        {
            if (delta.ContentDelta is not null)
            {
                this.contentBuilder.Append(delta.ContentDelta);
            }

            if (delta.ToolCallDelta is not null)
            {
                // FR-004b-069: Merge ToolCallDelta by Index into complete ToolCalls
                // For now, concatenate ArgumentsDelta to build up the complete arguments JSON
                if (delta.ToolCallDelta.ArgumentsDelta is not null)
                {
                    this.toolCallBuilder.Append(delta.ToolCallDelta.ArgumentsDelta);
                }
            }

            if (delta.FinishReason is not null)
            {
                this.finishReason = delta.FinishReason;
            }

            if (delta.Usage is not null)
            {
                this.usage = delta.Usage;
            }

            this.deltaCount++;
        }
    }

    /// <summary>
    /// Builds the complete ChatResponse from accumulated deltas.
    /// </summary>
    /// <returns>The complete ChatResponse.</returns>
    /// <exception cref="InvalidOperationException">Thrown if Build() called before final delta received.</exception>
    /// <remarks>
    /// FR-072: DeltaAccumulator MUST provide Build() returning complete ChatResponse.
    /// FR-077: DeltaAccumulator MUST throw if Build() called before final delta received.
    /// </remarks>
    public ChatResponse Build()
    {
        lock (this.lockObject)
        {
            if (this.finishReason is null)
            {
                throw new InvalidOperationException("Cannot build ChatResponse before final delta with FinishReason has been received.");
            }

            var content = this.contentBuilder.ToString();
            var message = ChatMessage.CreateAssistant(content);

            return new ChatResponse(
                Id: Guid.NewGuid().ToString(),
                Message: message,
                FinishReason: this.finishReason.Value,
                Usage: this.usage ?? UsageInfo.Empty,
                Metadata: new ResponseMetadata("streaming", "unknown", TimeSpan.Zero),
                Created: DateTimeOffset.UtcNow,
                Model: "unknown");
        }
    }

    /// <summary>
    /// Represents a partial response state during streaming.
    /// </summary>
    public sealed record PartialResponse(
        string Content,
        FinishReason? FinishReason,
        UsageInfo? Usage);
}
