namespace Acode.Infrastructure.Ollama.ToolCall;

using System.Text;
using Acode.Infrastructure.Ollama.ToolCall.Models;

/// <summary>
/// Accumulates tool call fragments from streaming responses.
/// Thread-safe for concurrent delta processing.
/// </summary>
/// <remarks>
/// FR-007d: Streaming tool call accumulation.
/// Assembles complete tool calls from streaming deltas.
/// </remarks>
public sealed class StreamingToolCallAccumulator
{
    private readonly Dictionary<int, AccumulatorState> states = new();
    private readonly object lockObj = new();

    /// <summary>
    /// Gets a value indicating whether there are pending (incomplete) tool calls.
    /// </summary>
    public bool HasPendingToolCalls
    {
        get
        {
            lock (this.lockObj)
            {
                return this.states.Count > 0;
            }
        }
    }

    /// <summary>
    /// Gets the count of pending tool calls.
    /// </summary>
    public int PendingCount
    {
        get
        {
            lock (this.lockObj)
            {
                return this.states.Count;
            }
        }
    }

    /// <summary>
    /// Accumulates a streaming delta into the appropriate tool call.
    /// </summary>
    /// <param name="delta">The delta to accumulate.</param>
    public void AccumulateDelta(ToolCallDelta delta)
    {
        ArgumentNullException.ThrowIfNull(delta);

        lock (this.lockObj)
        {
            if (!this.states.TryGetValue(delta.Index, out var state))
            {
                state = new AccumulatorState();
                this.states[delta.Index] = state;
            }

            // Update ID if present
            if (!string.IsNullOrEmpty(delta.Id))
            {
                state.Id = delta.Id;
            }

            // Update function name if present
            if (!string.IsNullOrEmpty(delta.FunctionName))
            {
                state.FunctionName.Clear();
                state.FunctionName.Append(delta.FunctionName);
            }

            // Append function name fragment if present
            if (!string.IsNullOrEmpty(delta.FunctionNameFragment))
            {
                state.FunctionName.Append(delta.FunctionNameFragment);
            }

            // Append arguments fragment if present
            if (!string.IsNullOrEmpty(delta.ArgumentsFragment))
            {
                state.Arguments.Append(delta.ArgumentsFragment);
            }

            // Mark complete if signaled
            if (delta.IsComplete)
            {
                state.IsComplete = true;
            }
        }
    }

    /// <summary>
    /// Gets the accumulated arguments for a specific index.
    /// </summary>
    /// <param name="index">The tool call index.</param>
    /// <returns>The accumulated arguments string.</returns>
    public string GetAccumulatedArguments(int index)
    {
        lock (this.lockObj)
        {
            if (this.states.TryGetValue(index, out var state))
            {
                return state.Arguments.ToString();
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Marks a tool call at the specified index as complete.
    /// </summary>
    /// <param name="index">The tool call index.</param>
    public void MarkComplete(int index)
    {
        lock (this.lockObj)
        {
            if (this.states.TryGetValue(index, out var state))
            {
                state.IsComplete = true;
            }
        }
    }

    /// <summary>
    /// Checks if a tool call at the specified index is complete.
    /// </summary>
    /// <param name="index">The tool call index.</param>
    /// <returns>True if complete, false otherwise.</returns>
    public bool IsComplete(int index)
    {
        lock (this.lockObj)
        {
            if (this.states.TryGetValue(index, out var state))
            {
                return state.IsComplete;
            }

            return false;
        }
    }

    /// <summary>
    /// Flushes all completed tool calls and removes them from the accumulator.
    /// </summary>
    /// <returns>List of completed OllamaToolCall objects.</returns>
    public IReadOnlyList<OllamaToolCall> Flush()
    {
        lock (this.lockObj)
        {
            var completed = new List<OllamaToolCall>();
            var indicesToRemove = new List<int>();

            foreach (var kvp in this.states)
            {
                if (kvp.Value.IsComplete)
                {
                    var toolCall = new OllamaToolCall
                    {
                        Id = kvp.Value.Id,
                        Type = "function",
                        Function = new OllamaFunction
                        {
                            Name = kvp.Value.FunctionName.ToString(),
                            Arguments = kvp.Value.Arguments.ToString(),
                        },
                    };

                    completed.Add(toolCall);
                    indicesToRemove.Add(kvp.Key);
                }
            }

            // Remove completed items
            foreach (var index in indicesToRemove)
            {
                this.states.Remove(index);
            }

            return completed;
        }
    }

    /// <summary>
    /// Resets all state, discarding any pending tool calls.
    /// </summary>
    public void Reset()
    {
        lock (this.lockObj)
        {
            this.states.Clear();
        }
    }

    /// <summary>
    /// Internal state for accumulating a single tool call.
    /// </summary>
    private sealed class AccumulatorState
    {
        public string? Id { get; set; }

        public StringBuilder FunctionName { get; } = new();

        public StringBuilder Arguments { get; } = new();

        public bool IsComplete { get; set; }
    }
}
