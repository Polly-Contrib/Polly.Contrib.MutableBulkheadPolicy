// Copyright (c) Microsoft Corporation.
// Licensed under the BSD-3-Clause.

using Polly.Bulkhead;

namespace Polly.Contrib.MutableBulkheadPolicy
{
    /// <summary>
    /// Defines properties and methods common to all MutableBulkhead policies.
    /// </summary>

    public interface IMutableBulkheadPolicy : IBulkheadPolicy
    {
        /// <summary>
        /// Gets or sets the max number of slots available for executing actions through the MutableBulkheadPolicy.
        /// </summary>
        int MaxParallelization { get; set; }

        /// <summary>
        /// Gets or sets the max number of slots available for queuing actions for execution through the MutableBulkheadPolicy.
        /// </summary>
        int MaxQueueingActions { get; set; }
    }

    /// <summary>
    /// Defines properties and methods common to all MutableBulkhead policies generic-typed for executions returning results of type <typeparamref name="TResult"/>.
    /// </summary>
    public interface IMutableBulkheadPolicy<TResult> : IMutableBulkheadPolicy
    {
        
    }
}
