// Copyright (c) Microsoft Corporation.
// Licensed under the BSD-3-Clause.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Polly.Contrib.MutableBulkheadPolicy
{
    /// <summary>
    /// A MutableBulkhead-isolation policy which can be applied to delegates.
    /// </summary>
    public partial class AsyncMutableBulkheadPolicy : AsyncPolicy, IMutableBulkheadPolicy
    {
        private readonly SemaphoreSlimDynamic _maxParallelizationSemaphore;
        private readonly SemaphoreSlimDynamic _maxQueuedActionsSemaphore;

        private Func<Context, Task> _onBulkheadRejectedAsync;

        private int _maxParallelization;
        private int _maxQueuedActions;

        internal AsyncMutableBulkheadPolicy(
            int maxParallelization,
            int maxQueueingActions,
            SemaphoreSlimDynamic maxParallelizationSemaphore,
            SemaphoreSlimDynamic maxQueuedActionsSemaphore,
            Func<Context, Task> onBulkheadRejectedAsync)
        {
            _maxParallelization = maxParallelization;
            _maxQueuedActions = maxQueueingActions;
            _maxParallelizationSemaphore = maxParallelizationSemaphore;
            _maxQueuedActionsSemaphore = maxQueuedActionsSemaphore;
            _onBulkheadRejectedAsync = onBulkheadRejectedAsync ?? throw new ArgumentNullException(nameof(onBulkheadRejectedAsync));
        }

        /// <summary>
        /// Gets the number of slots currently available for executing actions through the MutableBulkhead.
        /// </summary>
        public int BulkheadAvailableCount => _maxParallelizationSemaphore.CurrentCount;

        /// <summary>
        /// Gets the number of slots currently available for queuing actions for execution through the MutableBulkhead.
        /// </summary>
        public int QueueAvailableCount => Math.Min(_maxQueuedActionsSemaphore.CurrentCount, MaxQueueingActions);

        /// <summary>
        /// Gets or sets the max number of slots available for executing actions through the MutableBulkheadPolicy.
        /// </summary>
        public int MaxParallelization
        {
            get { return _maxParallelization; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(MaxParallelization));
                }

                this._maxParallelization = value;
                this._maxParallelizationSemaphore.SetAvailableSlot(this._maxParallelization);

                // Recalculate compound available slots for max queuing action semaphore as well
                // since it is dependent on the max parallelization number.
                //
                // If max parallelization has more tasks than available, then max queue may temporarily full
                // until enough waits are released.
                this.MaxQueueingActions = this.MaxQueueingActions;
            }
        }

        /// <summary>
        /// Gets or sets the max number of slots available for queuing actions for execution through the MutableBulkheadPolicy.
        /// </summary>
        public int MaxQueueingActions
        {
            get { return _maxQueuedActions; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(MaxQueueingActions));
                }

                this._maxQueuedActions = value;

                int maxParallelization = this._maxParallelization;
                var maxQueuingCompounded = this._maxQueuedActions <= int.MaxValue - maxParallelization
                                        ? this._maxQueuedActions + maxParallelization
                                        : int.MaxValue;
                this._maxQueuedActionsSemaphore.SetAvailableSlot(maxQueuingCompounded);
            }
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        protected override Task<TResult> ImplementationAsync<TResult>(Func<Context, CancellationToken, Task<TResult>> action, Context context, CancellationToken cancellationToken,
            bool continueOnCapturedContext)
        {
            return AsyncMutableBulkheadEngine.ImplementationAsync(action, context, _onBulkheadRejectedAsync, _maxParallelizationSemaphore, _maxQueuedActionsSemaphore, cancellationToken, continueOnCapturedContext);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _maxParallelizationSemaphore.Dispose();
            _maxQueuedActionsSemaphore.Dispose();
        }
    }

    /// <summary>
    /// A MutableBulkhead-isolation policy which can be applied to delegates.
    /// </summary>
    /// <typeparam name="TResult">The return type of delegates which may be executed through the policy.</typeparam>
    public class AsyncMutableBulkheadPolicy<TResult> : AsyncPolicy<TResult>, IMutableBulkheadPolicy<TResult>
    {
        private readonly SemaphoreSlimDynamic _maxParallelizationSemaphore;
        private readonly SemaphoreSlimDynamic _maxQueuedActionsSemaphore;
        private Func<Context, Task> _onBulkheadRejectedAsync;

        private int _maxParallelization;
        private int _maxQueuedActions;

        internal AsyncMutableBulkheadPolicy(
            int maxParallelization,
            int maxQueueingActions,
            SemaphoreSlimDynamic maxParallelizationSemaphore,
            SemaphoreSlimDynamic maxQueuedActionsSemaphore,
            Func<Context, Task> onBulkheadRejectedAsync)
        {
            _maxParallelization = maxParallelization;
            _maxQueuedActions = maxQueueingActions;
            _maxParallelizationSemaphore = maxParallelizationSemaphore;
            _maxQueuedActionsSemaphore = maxQueuedActionsSemaphore;
            _onBulkheadRejectedAsync = onBulkheadRejectedAsync ?? throw new ArgumentNullException(nameof(onBulkheadRejectedAsync));
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        protected override Task<TResult> ImplementationAsync(Func<Context, CancellationToken, Task<TResult>> action, Context context, CancellationToken cancellationToken, bool continueOnCapturedContext)
        {
            return AsyncMutableBulkheadEngine.ImplementationAsync(action, context, _onBulkheadRejectedAsync, _maxParallelizationSemaphore, _maxQueuedActionsSemaphore, cancellationToken, continueOnCapturedContext);
        }

        /// <summary>
        /// Gets the number of slots currently available for executing actions through the MutableBulkhead.
        /// </summary>
        public int BulkheadAvailableCount => _maxParallelizationSemaphore.CurrentCount;

        /// <summary>
        /// Gets the number of slots currently available for queuing actions for execution through the MutableBulkhead.
        /// </summary>
        public int QueueAvailableCount => Math.Min(_maxQueuedActionsSemaphore.CurrentCount, MaxQueueingActions);

        /// <summary>
        /// Gets or sets the max number of slots available for executing actions through the MutableBulkheadPolicy.
        /// </summary>
        public int MaxParallelization
        {
            get { return _maxParallelization; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(MaxParallelization));
                }

                this._maxParallelization = value;
                this._maxParallelizationSemaphore.SetAvailableSlot(this._maxParallelization);

                // Recalculate compound available slots for max queuing action semaphore as well
                // since it is dependent on the max parallelization number.
                //
                // If max parallelization has more tasks than available, then max queue may temporarily full
                // until enough waits are released.
                this.MaxQueueingActions = this.MaxQueueingActions;
            }
        }

        /// <summary>
        /// Gets or sets the max number of slots available for queuing actions for execution through the MutableBulkheadPolicy.
        /// </summary>
        public int MaxQueueingActions
        {
            get { return _maxQueuedActions; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(MaxQueueingActions));
                }

                this._maxQueuedActions = value;

                int maxParallelization = this._maxParallelization;
                var maxQueuingCompounded = this._maxQueuedActions <= int.MaxValue - maxParallelization
                                        ? this._maxQueuedActions + maxParallelization
                                        : int.MaxValue;
                this._maxQueuedActionsSemaphore.SetAvailableSlot(maxQueuingCompounded);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _maxParallelizationSemaphore.Dispose();
            _maxQueuedActionsSemaphore.Dispose();
        }
    }
}