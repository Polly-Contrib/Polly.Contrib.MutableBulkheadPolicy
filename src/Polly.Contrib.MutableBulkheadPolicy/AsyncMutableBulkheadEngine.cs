// Copyright (c) Microsoft Corporation.
// Licensed under the BSD-3-Clause.

using Polly.Bulkhead;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Polly.Contrib.MutableBulkheadPolicy
{
   internal static class AsyncMutableBulkheadEngine
    {
       internal static async Task<TResult> ImplementationAsync<TResult>(
            Func<Context, CancellationToken, Task<TResult>> action,
            Context context,
            Func<Context, Task> onBulkheadRejectedAsync,
            SemaphoreSlimDynamic maxParallelizationSemaphore,
            SemaphoreSlimDynamic maxQueuedActionsSemaphore,
            CancellationToken cancellationToken, 
            bool continueOnCapturedContext)
        {
            if (!await maxQueuedActionsSemaphore.WaitAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(continueOnCapturedContext))
            {
                await onBulkheadRejectedAsync(context).ConfigureAwait(continueOnCapturedContext);
                throw new BulkheadRejectedException();
            }
            try
            {
                await maxParallelizationSemaphore.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext);

                try 
                {
                    return await action(context, cancellationToken).ConfigureAwait(continueOnCapturedContext);
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
