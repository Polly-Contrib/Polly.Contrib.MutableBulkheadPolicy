# Polly.Contrib.MutableBulkheadPolicy

Provides a BulkheadPolicy (parallel throttle) for which the capacity can be adjusted dynamically. This is the mutable version of the the bulkhead policy that came with [Polly](https://github.com/App-vNext/Polly).

For more information on Bulkhead Policy see the [Bulkhead doc](https://github.com/App-vNext/Polly/wiki/Bulkhead).

For more background on Polly see the [main Polly repo](https://github.com/App-vNext/Polly).

### Usage

#### Asynchronous executions

```csharp
AsyncMutableBulkheadPolicy bulkhead = AsyncMutableBulkheadPolicy
  .Create(int maxParallelization,
         [, int maxQueuingActions]
         [, Action<Context> onBulkheadRejected]);

bulkhead.MaxParallelization = 10;
bulkhead.MaxQueueingActions = 2;
```

#### Synchronous executions

```csharp
MutableBulkheadPolicy bulkhead = MutableBulkheadPolicy
  .Create(int maxParallelization,
         [, int maxQueuingActions]
         [, Action<Context> onBulkheadRejected]);

bulkhead.MaxParallelization = 10;
bulkhead.MaxQueueingActions = 2;
```

If updated `MaxParallelization` is lower and the current bulkhead is full, any new tasks will be load shredded until the current count is lower than the new `MaxParallelization`.

## Code of Conduct

We ask our contributors to abide by the [Code of Conduct of the .NET Foundation](https://www.dotnetfoundation.org/code-of-conduct).

## License

Copyright (c) Microsoft Corporation. All rights reserved.

Licensed under the [BSD 3-Clause](LICENSE.txt) license.