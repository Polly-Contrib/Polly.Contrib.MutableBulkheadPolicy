# Polly.Contrib.MutableBulkheadPolicy

Provides a BulkheadPolicy (parallel throttle) for which the capacity can be adjusted dynamically. This is the mutable version of the the bulkhead policy that came with [Polly](https://github.com/App-vNext/Polly).

For more information on Bulkhead Policy see the [Bulkhead doc](https://github.com/App-vNext/Polly/wiki/Bulkhead).

For more background on Polly see the [main Polly repo](https://github.com/App-vNext/Polly).

## Usage

### Asynchronous executions

```csharp
// To create:
AsyncMutableBulkheadPolicy bulkhead = AsyncMutableBulkheadPolicy
  .Create(int maxParallelization,
         [, int maxQueuingActions]
         [, Action<Context> onBulkheadRejected]);

// To adjust capacity at a later time:
bulkhead.MaxParallelization = 10;
bulkhead.MaxQueueingActions = 2;
```

### Synchronous executions

```csharp
// To create:
MutableBulkheadPolicy bulkhead = MutableBulkheadPolicy
  .Create(int maxParallelization,
         [, int maxQueuingActions]
         [, Action<Context> onBulkheadRejected]);

// To adjust capacity at a later time:
bulkhead.MaxParallelization = 10;
bulkhead.MaxQueueingActions = 2;
```

### Adjusting the bulkhead capacity

The bulkhead capacity can be adjusted any time after creation, by setting the properties `MaxParallelization` or `MaxQueueingActions`, either together or individually: 

```csharp
bulkhead.MaxParallelization = 6;
bulkhead.MaxQueueingActions = 1;
```

**If the adjustment increases capacity**, the increased capacity will be granted immediately.

**If the adjustment decreases capacity:** 

+ where the capacity to be removed is currently unused (no actions executing through those bulkhead slots), it will be removed immediately.
+ where capacity to be removed is currently in use (actions are executing through those bulkhead slots), the capacity will be removed when actions complete; actions in progress will not be terminated.

For example, consider a scenario where the capacity prior to adjustment is 10, of which 8 slots are currently occupied with executions. On a request to reduce capacity to 6, two slots will be removed from capacity immediately, and a further two slots will be removed from capacity as and when two further actions complete.

While a bulkhead is temporarily acting over-capacity (eg 8 executions are in progress but the bulkhead is pending reducing capacity to 6), incoming actions will be load-shedded until the bulkhead utilisation reduces to the desired capacity level.

## Code of Conduct

We ask our contributors to abide by the [Code of Conduct of the .NET Foundation](https://www.dotnetfoundation.org/code-of-conduct).

## License

Copyright (c) Microsoft Corporation. All rights reserved.

Licensed under the [BSD 3-Clause](LICENSE.txt) license.