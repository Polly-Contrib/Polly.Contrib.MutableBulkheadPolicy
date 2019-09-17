// Copyright (c) Microsoft Corporation.
// Licensed under the BSD-3-Clause.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Polly.Contrib.MutableBulkheadPolicy.Specs.Helpers;
using Polly.Contrib.MutableBulkheadPolicy.Specs.Helpers.Bulkhead;
using FluentAssertions;
using Polly.Utilities;
using Xunit;
using Xunit.Abstractions;
using Polly.Bulkhead;

namespace Polly.Contrib.MutableBulkheadPolicy.Specs
{
    public class MutableBulkheadTResultAsyncSpecs : MutableBulkheadSpecsHelper
    {
        public MutableBulkheadTResultAsyncSpecs(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        #region Configuration

        [Fact]
        public void Should_throw_when_maxparallelization_less_or_equal_to_zero()
        {
            Action policy = () => AsyncMutableBulkheadPolicy
                .Create<int>(0, 1);

            policy.ShouldThrow<ArgumentOutOfRangeException>().And
                .ParamName.Should().Be("maxParallelization");
        }

        [Fact]
        public void Should_throw_when_maxQueuingActions_less_than_zero()
        {
            Action policy = () => AsyncMutableBulkheadPolicy
                .Create<int>(1, -1);

            policy.ShouldThrow<ArgumentOutOfRangeException>().And
                .ParamName.Should().Be("maxQueuingActions");
        }

        [Fact]
        public void Should_throw_when_onBulkheadRejected_is_null()
        {
            Action policy = () => AsyncMutableBulkheadPolicy
                .Create<int>(1, 0, null);

            policy.ShouldThrow<ArgumentNullException>().And
                .ParamName.Should().Be("onBulkheadRejectedAsync");
        }

        #endregion

        #region onBulkheadRejected delegate

        [Fact]
        public void Should_call_onBulkheadRejected_with_passed_context()
        {
            string operationKey = "SomeKey";
            Context contextPassedToExecute = new Context(operationKey);

            Context contextPassedToOnRejected = null;
            Func<Context, Task> onRejectedAsync = async ctx => { contextPassedToOnRejected = ctx; await TaskHelper.EmptyTask.ConfigureAwait(false); };

            var MutableBulkhead = AsyncMutableBulkheadPolicy.Create<int>(1, onRejectedAsync);

            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                Task.Run(() => {
                    MutableBulkhead.ExecuteAsync(async () =>
                    {
                        await tcs.Task.ConfigureAwait(false);
                        return 0;
                    });
                });

                Within(shimTimeSpan, () => MutableBulkhead.BulkheadAvailableCount.Should().Be(0)); // Time for the other thread to kick up and take the MutableBulkhead.

                MutableBulkhead.Awaiting(async b => await b.ExecuteAsync(ctx => Task.FromResult(1), contextPassedToExecute)).ShouldThrow<BulkheadRejectedException>();

                cancellationSource.Cancel();
                tcs.SetCanceled();
            }

            contextPassedToOnRejected.Should().NotBeNull();
            contextPassedToOnRejected.OperationKey.Should().Be(operationKey);
            contextPassedToOnRejected.Should().BeSameAs(contextPassedToExecute);
        }

        #endregion


        #region Mutable bulkhead behaviour

        [Theory, ClassData(typeof(MutableBulkheadScenarios))]
        public void Should_control_executions_queuing_and_rejections_per_specification_with_cancellations(
            int maxParallelization, int maxQueuingActions, int totalActions, bool cancelQueuing,
            bool cancelExecuting, int updateMaxParalelizationDelta, int updateQueuingActionsDelta, string scenario)
        {
            if (totalActions < 0) throw new ArgumentOutOfRangeException(nameof(totalActions));
            scenario = String.Format("MaxParallelization {0}; MaxQueuing {1}; TotalActions {2}; CancelQueuing {3}; CancelExecuting {4}; UpdateMaxParalelizationDelta {5}; UpdateQueuingActionsDelta {6}: {7}", maxParallelization, maxQueuingActions, totalActions, cancelQueuing, cancelExecuting, updateMaxParalelizationDelta, updateQueuingActionsDelta, scenario);

            var bulkhead = AsyncMutableBulkheadPolicy.Create<ResultPrimitive>(maxParallelization, maxQueuingActions);

            // Set up delegates which we can track whether they've started; and control when we allow them to complete (to release their semaphore slot).
            actions = new TraceableAction[totalActions];
            for (int i = 0; i < totalActions; i++) { actions[i] = new TraceableAction(i, statusChanged, testOutputHelper); }

            // Throw all the delegates at the MutableBulkhead simultaneously.
            Task<ResultPrimitive>[] tasks = new Task<ResultPrimitive>[totalActions];
            for (int i = 0; i < totalActions; i++) { tasks[i] = actions[i].ExecuteOnBulkheadAsync<ResultPrimitive>(bulkhead); }

            testOutputHelper.WriteLine("Immediately after queueing...");
            testOutputHelper.WriteLine("MutableBulkhead: {0} slots out of {1} available.", bulkhead.BulkheadAvailableCount, maxParallelization);
            testOutputHelper.WriteLine("MutableBulkhead queue: {0} slots out of {1} available.", bulkhead.QueueAvailableCount, maxQueuingActions);
            OutputActionStatuses();

            // Assert the expected distributions of executing, queuing, rejected and completed - when all delegates thrown at MutableBulkhead.
            int expectedCompleted = 0;
            int expectedCancelled = 0;
            int expectedExecuting = Math.Min(totalActions, maxParallelization);
            int expectedRejects = Math.Max(0, totalActions - maxParallelization - maxQueuingActions);
            int expectedQueuing = Math.Min(maxQueuingActions, Math.Max(0, totalActions - maxParallelization));
            int expectedMutableBulkheadFree = maxParallelization - expectedExecuting;
            int expectedQueueFree = maxQueuingActions - expectedQueuing;

            try
            {
                actions.Count(a => a.Status == TraceableActionStatus.Faulted).Should().Be(0);
                Within(shimTimeSpan, () => actions.Count(a => a.Status == TraceableActionStatus.Executing).Should().Be(expectedExecuting, scenario + ", when checking expectedExecuting"));
                Within(shimTimeSpan, () => actions.Count(a => a.Status == TraceableActionStatus.QueueingForSemaphore).Should().Be(expectedQueuing, scenario + ", when checking expectedQueuing"));
                Within(shimTimeSpan, () => actions.Count(a => a.Status == TraceableActionStatus.Rejected).Should().Be(expectedRejects, scenario + ", when checking expectedRejects"));
                actions.Count(a => a.Status == TraceableActionStatus.Completed).Should().Be(expectedCompleted, scenario + ", when checking expectedCompleted");
                actions.Count(a => a.Status == TraceableActionStatus.Canceled).Should().Be(expectedCancelled, scenario + ", when checking expectedCancelled");
                Within(shimTimeSpan, () => bulkhead.BulkheadAvailableCount.Should().Be(expectedMutableBulkheadFree, scenario + ", when checking expectedMutableBulkheadFree"));
                Within(shimTimeSpan, () => bulkhead.QueueAvailableCount.Should().Be(expectedQueueFree, scenario + ", when checking expectedQueueFree"));
            }
            finally
            {
                testOutputHelper.WriteLine("Expected initial state verified...");
                testOutputHelper.WriteLine("MutableBulkhead: {0} slots out of {1} available.", bulkhead.BulkheadAvailableCount, maxParallelization);
                testOutputHelper.WriteLine("MutableBulkhead queue: {0} slots out of {1} available.", bulkhead.QueueAvailableCount, maxQueuingActions);
                OutputActionStatuses();
            }

            // Complete or cancel delegates one by one, and expect others to take their place (if a slot released and others remain queueing); until all work is done.
            while (expectedExecuting > 0)
            {
                if (cancelQueuing)
                {
                    testOutputHelper.WriteLine("Cancelling a queueing task...");

                    actions.First(a => a.Status == TraceableActionStatus.QueueingForSemaphore).Cancel();

                    expectedCancelled++;
                    if (expectedQueuing > bulkhead.MaxQueueingActions)
                    {
                        expectedQueuing--;
                    }
                    else
                    {
                        expectedQueuing--;
                        expectedQueueFree = Math.Min(bulkhead.MaxQueueingActions, expectedQueueFree + 1);
                    }

                    cancelQueuing = false;
                }
                else if (cancelExecuting)
                {
                    testOutputHelper.WriteLine("Cancelling an executing task...");

                    actions.First(a => a.Status == TraceableActionStatus.Executing).Cancel();

                    expectedCancelled++;
                    if (expectedExecuting > bulkhead.MaxParallelization)
                    {
                        expectedExecuting--;
                    }
                    else if (expectedQueuing > bulkhead.MaxQueueingActions)
                    {
                        expectedQueuing--;
                    }
                    else if (expectedQueuing > 0)
                    {
                        expectedQueuing--;
                        expectedQueueFree = Math.Min(bulkhead.MaxQueueingActions, expectedQueueFree + 1);
                    }
                    else
                    {
                        expectedExecuting--;
                        expectedMutableBulkheadFree = Math.Min(bulkhead.MaxParallelization, expectedMutableBulkheadFree + 1);
                    }

                    cancelExecuting = false;
                }
                else if (updateMaxParalelizationDelta != 0)
                {
                    testOutputHelper.WriteLine("Updating max parallelization...");

                    bulkhead.MaxParallelization += updateMaxParalelizationDelta;
                    expectedMutableBulkheadFree = Math.Max(0, expectedMutableBulkheadFree + updateMaxParalelizationDelta);

                    // Check if max paralelization will have more tasks than available. If yes, queue size is temporarily affected.
                    if (expectedMutableBulkheadFree + updateMaxParalelizationDelta < 0)
                    {
                        expectedQueueFree = Math.Max(0, expectedQueueFree + expectedMutableBulkheadFree + updateMaxParalelizationDelta);
                    }
                    updateMaxParalelizationDelta = 0;
                }
                else if (updateQueuingActionsDelta != 0)
                {
                    testOutputHelper.WriteLine("Updating max queuing actions...");

                    bulkhead.MaxQueueingActions += updateQueuingActionsDelta;
                    expectedQueueFree = Math.Max(0, expectedQueueFree + updateQueuingActionsDelta);
                    updateQueuingActionsDelta = 0;
                }
                else // Complete an executing delegate.
                {
                    testOutputHelper.WriteLine("Completing a task...");

                    actions.First(a => a.Status == TraceableActionStatus.Executing).AllowCompletion();

                    expectedCompleted++;

                    if (expectedExecuting > bulkhead.MaxParallelization)
                    {
                        expectedExecuting--;
                    }
                    else if (expectedQueuing > bulkhead.MaxQueueingActions)
                    {
                        expectedQueuing--;
                    }
                    else if (expectedQueuing > 0)
                    {
                        expectedQueuing--;
                        expectedQueueFree = Math.Min(bulkhead.MaxQueueingActions, expectedQueueFree + 1);
                    }
                    else
                    {
                        expectedExecuting--;
                        expectedMutableBulkheadFree = Math.Min(bulkhead.MaxParallelization, expectedMutableBulkheadFree + 1);
                    }

                }

                try
                {
                    actions.Count(a => a.Status == TraceableActionStatus.Faulted).Should().Be(0);
                    Within(shimTimeSpan, () => actions.Count(a => a.Status == TraceableActionStatus.Executing).Should().Be(expectedExecuting, scenario + ", when checking expectedExecuting"));
                    Within(shimTimeSpan, () => actions.Count(a => a.Status == TraceableActionStatus.QueueingForSemaphore).Should().Be(expectedQueuing, scenario + ", when checking expectedQueuing"));
                    Within(shimTimeSpan, () => actions.Count(a => a.Status == TraceableActionStatus.Completed).Should().Be(expectedCompleted, scenario + ", when checking expectedCompleted"));
                    Within(shimTimeSpan, () => actions.Count(a => a.Status == TraceableActionStatus.Canceled).Should().Be(expectedCancelled, scenario + ", when checking expectedCancelled"));
                    actions.Count(a => a.Status == TraceableActionStatus.Rejected).Should().Be(expectedRejects, scenario + ", when checking expectedRejects");
                    Within(shimTimeSpan, () => bulkhead.BulkheadAvailableCount.Should().Be(expectedMutableBulkheadFree, scenario + ", when checking expectedMutableBulkheadFree"));
                    Within(shimTimeSpan, () => bulkhead.QueueAvailableCount.Should().Be(expectedQueueFree, scenario + ", when checking expectedQueueFree"));
                }
                finally
                {
                    testOutputHelper.WriteLine("End of next loop iteration...");
                    testOutputHelper.WriteLine("MutableBulkhead: {0} slots out of {1} available.", bulkhead.BulkheadAvailableCount, maxParallelization);
                    testOutputHelper.WriteLine("MutableBulkhead queue: {0} slots out of {1} available.", bulkhead.QueueAvailableCount, maxQueuingActions);
                    OutputActionStatuses();
                }
            }

            EnsureNoUnbservedTaskExceptions(tasks); 
            testOutputHelper.WriteLine("Verifying all tasks completed...");
            Within(shimTimeSpan, () => tasks.All(t => t.IsCompleted).Should().BeTrue());
        }

        #endregion

    }
}
