// Copyright (c) Microsoft Corporation.
// Licensed under the BSD-3-Clause.

using Polly.Bulkhead;
using System;

namespace Polly.Contrib.MutableBulkheadPolicy
{
    public partial class MutableBulkheadPolicy
    {
        /// <summary>
        /// <para>Builds a mutable bulkhead isolation <see cref="Policy"/>, which limits the maximum concurrency of actions executed through the policy.  Imposing a maximum concurrency limits the potential of governed actions, when faulting, to bring down the system.</para>
        /// <para>When an execution would cause the number of actions executing concurrently through the policy to exceed <paramref name="maxParallelization"/>, the action is not executed and a <see cref="BulkheadRejectedException"/> is thrown.</para>
        /// </summary>
        /// <param name="maxParallelization">The maximum number of concurrent actions that may be executing through the policy.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">maxParallelization;Value must be greater than zero.</exception>
        /// <returns>The policy instance.</returns>
        public static MutableBulkheadPolicy Create(int maxParallelization)
        {
            Action<Context> doNothing = _ => { };
            return Create(maxParallelization, 0, doNothing);
        }

        /// <summary>
        /// <para>Builds a mutable bulkhead isolation <see cref="Policy"/>, which limits the maximum concurrency of actions executed through the policy.  Imposing a maximum concurrency limits the potential of governed actions, when faulting, to bring down the system.</para>
        /// <para>When an execution would cause the number of actions executing concurrently through the policy to exceed <paramref name="maxParallelization"/>, the action is not executed and a <see cref="BulkheadRejectedException"/> is thrown.</para>
        /// </summary>
        /// <param name="maxParallelization">The maximum number of concurrent actions that may be executing through the policy.</param>
        /// <param name="onBulkheadRejected">An action to call, if the MutableBulkhead rejects execution due to oversubscription.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">maxParallelization;Value must be greater than zero.</exception>
        /// <exception cref="System.ArgumentNullException">onBulkheadRejected</exception>
        /// <returns>The policy instance.</returns>
        public static MutableBulkheadPolicy Create(int maxParallelization, Action<Context> onBulkheadRejected)
            => Create(maxParallelization, 0, onBulkheadRejected);

        /// <summary>
        /// Builds a mutable bulkhead isolation <see cref="Policy" />, which limits the maximum concurrency of actions executed through the policy.  Imposing a maximum concurrency limits the potential of governed actions, when faulting, to bring down the system.
        /// <para>When an execution would cause the number of actions executing concurrently through the policy to exceed <paramref name="maxParallelization" />, the policy allows a further <paramref name="maxQueuingActions" /> executions to queue, waiting for a concurrent execution slot.  When an execution would cause the number of queuing actions to exceed <paramref name="maxQueuingActions" />, a <see cref="BulkheadRejectedException" /> is thrown.</para>
        /// </summary>
        /// <param name="maxParallelization">The maximum number of concurrent actions that may be executing through the policy.</param>
        /// <param name="maxQueuingActions">The maximum number of actions that may be queuing, waiting for an execution slot.</param>
        /// <returns>The policy instance.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">maxParallelization;Value must be greater than zero.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">maxQueuingActions;Value must be greater than or equal to zero.</exception>
        public static MutableBulkheadPolicy Create(int maxParallelization, int maxQueuingActions)
        {
            Action<Context> doNothing = _ => { };
            return Create(maxParallelization, maxQueuingActions, doNothing);
        }

        /// <summary>
        /// Builds a mutable bulkhead isolation <see cref="Policy" />, which limits the maximum concurrency of actions executed through the policy.  Imposing a maximum concurrency limits the potential of governed actions, when faulting, to bring down the system.
        /// <para>When an execution would cause the number of actions executing concurrently through the policy to exceed <paramref name="maxParallelization" />, the policy allows a further <paramref name="maxQueuingActions" /> executions to queue, waiting for a concurrent execution slot.  When an execution would cause the number of queuing actions to exceed <paramref name="maxQueuingActions" />, a <see cref="BulkheadRejectedException" /> is thrown.</para>
        /// </summary>
        /// <param name="maxParallelization">The maximum number of concurrent actions that may be executing through the policy.</param>
        /// <param name="maxQueuingActions">The maximum number of actions that may be queuing, waiting for an execution slot.</param>
        /// <param name="onBulkheadRejected">An action to call, if the MutableBulkhead rejects execution due to oversubscription.</param>
        /// <returns>The policy instance.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">maxParallelization;Value must be greater than zero.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">maxParallelization;Value must be greater than zero.</exception>
        /// <exception cref="System.ArgumentNullException">onBulkheadRejected</exception>
        public static MutableBulkheadPolicy Create(int maxParallelization, int maxQueuingActions, Action<Context> onBulkheadRejected)
        {
            if (maxParallelization <= 0) throw new ArgumentOutOfRangeException(nameof(maxParallelization), "Value must be greater than zero.");
            if (maxQueuingActions < 0) throw new ArgumentOutOfRangeException(nameof(maxQueuingActions), "Value must be greater than or equal to zero.");
            if (onBulkheadRejected == null) throw new ArgumentNullException(nameof(onBulkheadRejected));

            SemaphoreSlimDynamic maxParallelizationSemaphore = new SemaphoreSlimDynamic(0, maxParallelization, int.MaxValue);

            var maxQueuingCompounded = maxQueuingActions <= int.MaxValue - maxParallelization
                ? maxQueuingActions + maxParallelization
                : int.MaxValue;
            SemaphoreSlimDynamic maxQueuedActionsSemaphore = new SemaphoreSlimDynamic(0, maxQueuingCompounded, int.MaxValue);

            return new MutableBulkheadPolicy(
                maxParallelization,
                maxQueuingActions,
                maxParallelizationSemaphore,
                maxQueuedActionsSemaphore,
                onBulkheadRejected
            );
        }
        
    }
}
