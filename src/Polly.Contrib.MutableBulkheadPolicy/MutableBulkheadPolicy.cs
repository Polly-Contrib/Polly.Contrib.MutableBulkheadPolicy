// Copyright (c) Microsoft Corporation.
// Licensed under the BSD-3-Clause.

using System;
using System.Diagnostics;
using System.Threading;

namespace Polly.Contrib.MutableBulkheadPolicy
{
    /// <summary>
    /// A mutable bulkhead-isolation policy which can be applied to delegates.
    /// </summary>
    public partial class MutableBulkheadPolicy : Policy, IMutableBulkheadPolicy
    {
        private readonly SemaphoreSlimDynamic _maxParallelizationSemaphore;
        private readonly SemaphoreSlimDynamic _maxQueuedActionsSemaphore;

        private readonly Action<Context> _onBulkheadRejected;

        private int _maxParallelization;
        private int _maxQueuedActions;

        internal MutableBulkheadPolicy(
            int maxParallelization,
            int maxQueueingActions,
            SemaphoreSlimDynamic maxParallelizationSemaphore,
            SemaphoreSlimDynamic maxQueuedActionsSemaphore,
            Action<Context> onBulkheadRejected)
        {
            _maxParallelization = maxParallelization;
            _maxQueuedActions = maxQueueingActions;
            _maxParallelizationSemaphore = maxParallelizationSemaphore ?? throw new ArgumentNullException(nameof(maxParallelizationSemaphore));
            _maxQueuedActionsSemaphore = maxQueuedActionsSemaphore ?? throw new ArgumentNullException(nameof(maxQueuedActionsSemaphore));
            _onBulkheadRejected = onBulkheadRejected ?? throw new ArgumentNullException(nameof(onBulkheadRejected));
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
            get { return _maxQueuedActions;  }
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

        /// <summary>
        /// Disposes of the <see cref="MutableBulkheadPolicy"/>, allowing it to dispose its internal resources.  
        /// <remarks>Only call <see cref="Dispose()"/> on a <see cref="MutableBulkheadPolicy"/> after all actions executed through the policy have completed.  If actions are still executing through the policy when <see cref="Dispose()"/> is called, an <see cref="ObjectDisposedException"/> may be thrown on the actions' threads when those actions complete.</remarks>
        /// </summary>
        public void Dispose()
        {
            _maxParallelizationSemaphore.Dispose();
            _maxQueuedActionsSemaphore.Dispose();
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        protected override TResult Implementation<TResult>(
            Func<Context, CancellationToken, TResult> action,
            Context context,
            CancellationToken cancellationToken)
        {
            return MutableBulkheadEngine.Implementation(action, context, _onBulkheadRejected, _maxParallelizationSemaphore, _maxQueuedActionsSemaphore, cancellationToken);
        }
    }

    /// <summary>
    /// A MutableBulkhead-isolation policy which can be applied to delegates returning a value of type <typeparamref name="TResult"/>.
    /// </summary>
    public class MutableBulkheadPolicy<TResult> : Policy<TResult>, IMutableBulkheadPolicy<TResult>
    {
        private readonly SemaphoreSlimDynamic _maxParallelizationSemaphore;
        private readonly SemaphoreSlimDynamic _maxQueuedActionsSemaphore;
        private readonly Action<Context> _onBulkheadRejected;

        private int _maxParallelization;
        private int _maxQueuedActions;

        /// <inheritdoc/>
        internal MutableBulkheadPolicy(
            int maxParallelization,
            int maxQueueingActions,
            SemaphoreSlimDynamic maxParallelizationSemaphore,
            SemaphoreSlimDynamic maxQueuedActionsSemaphore,
            Action<Context> onBulkheadRejected)
        {
            _maxParallelization = maxParallelization;
            _maxQueuedActions = maxQueueingActions;
            _maxParallelizationSemaphore = maxParallelizationSemaphore ?? throw new ArgumentNullException(nameof(maxParallelizationSemaphore));
            _maxQueuedActionsSemaphore = maxQueuedActionsSemaphore ?? throw new ArgumentNullException(nameof(maxQueuedActionsSemaphore));
            _onBulkheadRejected = onBulkheadRejected ?? throw new ArgumentNullException(nameof(onBulkheadRejected));
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        protected override TResult Implementation(Func<Context, CancellationToken, TResult> action, Context context, CancellationToken cancellationToken)
            => MutableBulkheadEngine.Implementation(action, context, _onBulkheadRejected, _maxParallelizationSemaphore, _maxQueuedActionsSemaphore, cancellationToken);

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

        /// <summary>
        /// Disposes of the <see cref="MutableBulkheadPolicy{TResult}"/>, allowing it to dispose its internal resources.  
        /// <remarks>Only call <see cref="Dispose()"/> on a <see cref="MutableBulkheadPolicy"/> after all actions executed through the policy have completed.  If actions are still executing through the policy when <see cref="Dispose()"/> is called, an <see cref="ObjectDisposedException"/> may be thrown on the actions' threads when those actions complete.</remarks>
        /// </summary>
        public void Dispose()
        {
            _maxParallelizationSemaphore.Dispose();
            _maxQueuedActionsSemaphore.Dispose();
        }
    }
}

