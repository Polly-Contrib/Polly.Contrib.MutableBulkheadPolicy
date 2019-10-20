// Copyright (c) Microsoft Corporation.
// Licensed under the BSD-3-Clause.

using System.Collections;
using System.Collections.Generic;

namespace Polly.Contrib.MutableBulkheadPolicy.Specs
{
    /// <summary>
    /// A set of test scenarios used in all MutableBulkheadPolicy tests.
    /// </summary>
    internal class MutableBulkheadScenarios : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new MutableBulkheadScenario(maxParallelization: 5, maxQueuingActions: 0, totalTestLoad: 3, cancelQueuing: false, cancelExecuting: false, updateMaxParalelizationDelta: 0, updateQueuingActionsDelta: 0, scenario: "A MutableBulkhead, with no queue, not even oversubscribed.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 20, maxQueuingActions: 0, totalTestLoad: 3, cancelQueuing: false, cancelExecuting: true, updateMaxParalelizationDelta: 0, updateQueuingActionsDelta: 0, scenario: "A high capacity MutableBulkhead, with no queue, not even oversubscribed; cancel some executing.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 3, maxQueuingActions: 0, totalTestLoad: 4, cancelQueuing: false, cancelExecuting: false, updateMaxParalelizationDelta: 0, updateQueuingActionsDelta: 0, scenario: "A MutableBulkhead, with no queue, oversubscribed.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 3, maxQueuingActions: 1, totalTestLoad: 5, cancelQueuing: false, cancelExecuting: false, updateMaxParalelizationDelta: 0, updateQueuingActionsDelta: 0, scenario: "A MutableBulkhead, with not enough queue to avoid rejections, oversubscribed.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 6, maxQueuingActions: 3, totalTestLoad: 9, cancelQueuing: true, cancelExecuting: true, updateMaxParalelizationDelta: 0, updateQueuingActionsDelta: 0, scenario: "A MutableBulkhead, with not enough queue to avoid rejections, oversubscribed; cancel some queuing, and some executing.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 5, maxQueuingActions: 3, totalTestLoad: 8, cancelQueuing: false, cancelExecuting: false, updateMaxParalelizationDelta: 0, updateQueuingActionsDelta: 0, scenario: "A MutableBulkhead, with enough queue to avoid rejections, oversubscribed.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 6, maxQueuingActions: 3, totalTestLoad: 9, cancelQueuing: true, cancelExecuting: true, updateMaxParalelizationDelta: 0, updateQueuingActionsDelta: 0, scenario: "A MutableBulkhead, with enough queue to avoid rejections, oversubscribed; cancel some queuing, and some executing.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 1, maxQueuingActions: 6, totalTestLoad: 5, cancelQueuing: true, cancelExecuting: true, updateMaxParalelizationDelta: 0, updateQueuingActionsDelta: 0, scenario: "A very tight capacity MutableBulkhead, but which allows a huge queue; enough for all actions to be gradually processed; cancel some queuing, and some executing.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 5, maxQueuingActions: 0, totalTestLoad: 3, cancelQueuing: false, cancelExecuting: false, updateMaxParalelizationDelta: 2, updateQueuingActionsDelta: 0, scenario: "Increase max parallelization for a MutableBulkhead, with no queue, not even oversubscribed.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 5, maxQueuingActions: 2, totalTestLoad: 7, cancelQueuing: false, cancelExecuting: false, updateMaxParalelizationDelta: 0, updateQueuingActionsDelta: 2, scenario: "Increase max queuing action for a MutableBulkhead, with queue, oversubscribed.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 5, maxQueuingActions: 0, totalTestLoad: 3, cancelQueuing: false, cancelExecuting: false, updateMaxParalelizationDelta: -2, updateQueuingActionsDelta: 0, scenario: "Decrease max parallelization for a MutableBulkhead, with no queue, not even oversubscribed.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 5, maxQueuingActions: 2, totalTestLoad: 7, cancelQueuing: false, cancelExecuting: false, updateMaxParalelizationDelta: 0, updateQueuingActionsDelta: -2, scenario: "Decrease max queuing action for a MutableBulkhead, with queue, oversubscribed.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 5, maxQueuingActions: 2, totalTestLoad: 7, cancelQueuing: false, cancelExecuting: false, updateMaxParalelizationDelta: -2, updateQueuingActionsDelta: 0, scenario: "Decrease max parallelization for a MutableBulkhead, with queue, oversubscribed.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 5, maxQueuingActions: 2, totalTestLoad: 7, cancelQueuing: false, cancelExecuting: false, updateMaxParalelizationDelta: 0, updateQueuingActionsDelta: -2, scenario: "Decrease max queuing action for a MutableBulkhead, with queue, oversubscribed.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 5, maxQueuingActions: 2, totalTestLoad: 7, cancelQueuing: false, cancelExecuting: false, updateMaxParalelizationDelta: -2, updateQueuingActionsDelta: -2, scenario: "Decrease max parallelization and queuing action for a MutableBulkhead, with queue, oversubscribed.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 5, maxQueuingActions: 2, totalTestLoad: 8, cancelQueuing: true, cancelExecuting: true, updateMaxParalelizationDelta: -2, updateQueuingActionsDelta: -2, scenario: "Decrease max parallelization and queuing action for a MutableBulkhead, with queue, oversubscribed with rejection. Cancel queuing and executing.").ToTheoryData();
            yield return new MutableBulkheadScenario(maxParallelization: 5, maxQueuingActions: 0, totalTestLoad: 8, cancelQueuing: false, cancelExecuting: true, updateMaxParalelizationDelta: -2, updateQueuingActionsDelta: 0, scenario: "Decrease max parallelization for a MutableBulkhead, without queue, oversubscribed with rejection. Cancel executing.").ToTheoryData();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
