// Copyright (c) Microsoft Corporation.
// Licensed under the BSD-3-Clause.

using Polly.Bulkhead;
using Polly.Utilities;
using System;
using System.Threading.Tasks;

namespace Polly.Contrib.MutableBulkheadPolicy
{
    public partial class AsyncMutableBulkheadPolicy
    {
        /// <summary>
        /// <para>Builds a mutable bulkhead isolation <see cref="Policy"/>, which limits the maximum concurrency of actions executed through the policy.  Imposing a maximum concurrency limits the potential of governed actions, when faulting, to bring down the system.</para>
        /// <para>When an execution would cause the number of actions executing concurrently through the policy to exceed <paramref name="maxParallelization"/>, the action is not executed and a <see cref="BulkheadRejectedException"/> is thrown.</para>
        /// </summary>
        /// <param name="maxParallelization">The maximum number of concurrent actions that may be executing through the policy.</param>
        /// <returns>The policy instance.</returns>
        public static AsyncMutableBulkheadPolicy Create(int maxParallelization)
        {
            Func<Context, Task> doNothingAsync = _ => TaskHelper.EmptyTask;
            return Create(maxParallelization, 0, doNothingAsync);
        }

        /// <summary>
        /// <para>Builds a mutable bulkhead isolation <see cref="Policy"/>, which limits the maximum concurrency of actions executed through the policy.  Imposing a maximum concurrency limits the potential of governed actions, when faulting, to bring down the system.</para>
        /// <para>When an execution would cause the number of actions executing concurrently through the policy to exceed <paramref name="maxParallelization"/>, the action is not executed and a <see cref="BulkheadRejectedException"/> is thrown.</para>
        /// </summary>
        /// <param name="maxParallelization">The maximum number of concurrent actions that may be executing through the policy.</param>
        /// <param name="onBulkheadRejectedAsync">An action to call asynchronously, if the MutableBulkhead rejects execution due to oversubscription.</param>
        /// <returns>The policy instance.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">maxParallelization;Value must be greater than zero.</exception>
        /// <exception cref="System.ArgumentNullException">onBulkheadRejectedAsync</exception>
        /// <returns>The policy instance.</returns>
        public static AsyncMutableBulkheadPolicy Create(int maxParallelization, Func<Context, Task> onBulkheadRejectedAsync)
            => Create(maxParallelization, 0, onBulkheadRejectedAsync);

        /// <summary>
        /// Builds a mutable bulkhead isolation <see cref="Policy" />, which limits the maximum concurrency of actions executed through the policy.  Imposing a maximum concurrency limits the potential of governed actions, when faulting, to bring down the system.
        /// <para>When an execution would cause the number of actions executing concurrently through the policy to exceed <paramref name="maxParallelization" />, the policy allows a further <paramref name="maxQueuingActions" /> executions to queue, waiting for a concurrent execution slot.  When an execution would cause the number of queuing actions to exceed <paramref name="maxQueuingActions" />, a <see cref="BulkheadRejectedException" /> is thrown.</para>
        /// </summary>
        /// <param name="maxParallelization">The maximum number of concurrent actions that may be executing through the policy.</param>
        /// <param name="maxQueuingActions">The maximum number of actions that may be queuing, waiting for an execution slot.</param>
        /// <returns>The policy instance.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">maxParallelization;Value must be greater than zero.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">maxQueuingActions;Value must be greater than or equal to zero.</exception>
        public static AsyncMutableBulkheadPolicy Create(int maxParallelization, int maxQueuingActions)
        {
            Func<Context, Task> doNothingAsync = _ => TaskHelper.EmptyTask;
            return Create(maxParallelization, maxQueuingActions, doNothingAsync);
        }

        /// <summary>
        /// Builds a mutable bulkhead isolation <see cref="Policy" />, which limits the maximum concurrency of actions executed through the policy.  Imposing a maximum concurrency limits the potential of governed actions, when faulting, to bring down the system.
        /// <para>When an execution would cause the number of actions executing concurrently through the policy to exceed <paramref name="maxParallelization" />, the policy allows a further <paramref name="maxQueuingActions" /> executions to queue, waiting for a concurrent execution slot.  When an execution would cause the number of queuing actions to exceed <paramref name="maxQueuingActions" />, a <see cref="BulkheadRejectedException" /> is thrown.</para>
        /// </summary>
        /// <param name="maxParallelization">The maximum number of concurrent actions that may be executing through the policy.</param>
        /// <param name="maxQueuingActions">The maximum number of actions that may be queuing, waiting for an execution slot.</param>
        /// <param name="onBulkheadRejectedAsync">An action to call asynchronously, if the MutableBulkhead rejects execution due to oversubscription.</param>
        /// <returns>The policy instance.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">maxParallelization;Value must be greater than zero.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">maxQueuingActions;Value must be greater than or equal to zero.</exception>
        /// <exception cref="System.ArgumentNullException">onBulkheadRejectedAsync</exception>
        public static AsyncMutableBulkheadPolicy Create(
            int maxParallelization, 
            int maxQueuingActions, 
            Func<Context, Task> onBulkheadRejectedAsync)
        {
            if (maxParallelization <= 0) throw new ArgumentOutOfRangeException(nameof(maxParallelization), "Value must be greater than zero.");
            if (maxQueuingActions < 0) throw new ArgumentOutOfRangeException(nameof(maxQueuingActions), "Value must be greater than or equal to zero.");
            if (onBulkheadRejectedAsync == null) throw new ArgumentNullException(nameof(onBulkheadRejectedAsync));

            SemaphoreSlimDynamic maxParallelizationSemaphore = new SemaphoreSlimDynamic(0, maxParallelization, int.MaxValue);

            var maxQueuingCompounded = maxQueuingActions <= int.MaxValue - maxParallelization
                ? maxQueuingActions + maxParallelization
                : int.MaxValue;
            SemaphoreSlimDynamic maxQueuedActionsSemaphore = new SemaphoreSlimDynamic(0, maxQueuingCompounded, int.MaxValue);

            return new AsyncMutableBulkheadPolicy(
                maxParallelization,
                maxQueuingActions,
                maxParallelizationSemaphore,
                maxQueuedActionsSemaphore,
                onBulkheadRejectedAsync
                );
        }
    }
}
