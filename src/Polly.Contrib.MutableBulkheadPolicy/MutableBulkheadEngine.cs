// Copyright (c) Microsoft Corporation.
// Licensed under the BSD-3-Clause.

using Polly.Bulkhead;
using System;
using System.Threading;

namespace Polly.Contrib.MutableBulkheadPolicy
{
    internal static class MutableBulkheadEngine
    {
        internal static TResult Implementation<TResult>(
            Func<Context, CancellationToken, TResult> action,
            Context context,
            Action<Context> onBulkheadRejected,
            SemaphoreSlimDynamic maxParallelizationSemaphore,
            SemaphoreSlimDynamic maxQueuedActionsSemaphore,
            CancellationToken cancellationToken)
        {
            if (!maxQueuedActionsSemaphore.Wait(TimeSpan.Zero, cancellationToken))
            {
                onBulkheadRejected(context);
                throw new BulkheadRejectedException();
            }
            
            try
            {
                maxParallelizationSemaphore.Wait(cancellationToken);
                try
                {
                    return action(context, cancellationToken);
                }
                finally
                {
                    maxParallelizationSemaphore.Release();
                }
            }
            finally
            {
                maxQueuedActionsSemaphore.Release();
            }
        }
    }
}
