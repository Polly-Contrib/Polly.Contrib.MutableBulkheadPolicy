// Copyright (c) Microsoft Corporation.
// Licensed under the BSD-3-Clause.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Polly.Contrib.MutableBulkheadPolicy.Specs
{
    /// <summary>
    /// SemaphoreSlimDynamic unit tests. Adpoted from current CoreFX SemaphoreSlim unit tests
    /// 
    /// Reference: https://github.com/dotnet/corefx/blob/master/src/System.Threading/tests/SemaphoreSlimTests.cs
    /// </summary>
    public class SemaphoreSlimDynamicTests
    {
        protected ITestOutputHelper testOutputHelper;

        public SemaphoreSlimDynamicTests(ITestOutputHelper testOutputHelper)
        {
#if !DEBUG 
            testOutputHelper = new SilentOutput();
#endif
            this.testOutputHelper = testOutputHelper;
        }

        /// <summary>
        /// SemaphoreSlimDynamic public methods and properties to be tested
        /// </summary>
        public enum Actions
        {
            Constructor,
            Wait,
            WaitAsync,
            Release,
            Dispose,
            CurrentCount,
            AvailableWaitHandle,
            SetAvailableSlot
        }

        [Fact]
        public static void RunSemaphoreSlimDynamicTest0_Ctor()
        {
            RunSemaphoreSlimDynamicTest0_Ctor_Internal(0, 0, 10, null);
            RunSemaphoreSlimDynamicTest0_Ctor_Internal(1, 5, 10, null);
            RunSemaphoreSlimDynamicTest0_Ctor_Internal(1, 10, 10, null);
            RunSemaphoreSlimDynamicTest0_Ctor_Internal(10, 10, 10, null);
        }

        [Fact]
        public static void RunSemaphoreSlimDynamicTest0_Ctor_Negative()
        {
            RunSemaphoreSlimDynamicTest0_Ctor_Internal(0, 10, 0, typeof(ArgumentOutOfRangeException));
            RunSemaphoreSlimDynamicTest0_Ctor_Internal(10, 10, -1, typeof(ArgumentOutOfRangeException));
            RunSemaphoreSlimDynamicTest0_Ctor_Internal(-1, -1, 10, typeof(ArgumentOutOfRangeException));
        }

        [Fact]
        public static void RunSemaphoreSlimDynamicTest1_Wait()
        {
            // Infinite timeout
            RunSemaphoreSlimDynamicTest1_Wait_Internal(10, 10, -1, true, null);
            RunSemaphoreSlimDynamicTest1_Wait_Internal(1, 10, -1, true, null);

            // Zero timeout
            RunSemaphoreSlimDynamicTest1_Wait_Internal(10, 10, 0, true, null);
            RunSemaphoreSlimDynamicTest1_Wait_Internal(1, 10, 0, true, null);
            RunSemaphoreSlimDynamicTest1_Wait_Internal(0, 10, 0, false, null);

            // Positive timeout
            RunSemaphoreSlimDynamicTest1_Wait_Internal(10, 10, 10, true, null);
            RunSemaphoreSlimDynamicTest1_Wait_Internal(1, 10, 10, true, null);
            RunSemaphoreSlimDynamicTest1_Wait_Internal(0, 10, 10, false, null);
        }

        [Fact]
        public static void RunSemaphoreSlimDynamicTest1_Wait_Internal_NegativeCases()
        {
            // Invalid timeout
            RunSemaphoreSlimDynamicTest1_Wait_Internal(10, 10, -10, true, typeof(ArgumentOutOfRangeException));
            RunSemaphoreSlimDynamicTest1_Wait_Internal
               (10, 10, new TimeSpan(0, 0, int.MaxValue), true, typeof(ArgumentOutOfRangeException));
        }

        [Fact]
        public static void RunSemaphoreSlimDynamicTest1_Wait_Async()
        {
            // Infinite timeout
            RunSemaphoreSlimDynamicTest1_Wait_InternalAsync(10, 10, -1, true, null);
            RunSemaphoreSlimDynamicTest1_Wait_InternalAsync(1, 10, -1, true, null);

            // Zero timeout
            RunSemaphoreSlimDynamicTest1_Wait_InternalAsync(10, 10, 0, true, null);
            RunSemaphoreSlimDynamicTest1_Wait_InternalAsync(1, 10, 0, true, null);
            RunSemaphoreSlimDynamicTest1_Wait_InternalAsync(0, 10, 0, false, null);

            // Positive timeout
            RunSemaphoreSlimDynamicTest1_Wait_InternalAsync(10, 10, 10, true, null);
            RunSemaphoreSlimDynamicTest1_Wait_InternalAsync(1, 10, 10, true, null);
            RunSemaphoreSlimDynamicTest1_Wait_InternalAsync(0, 10, 10, false, null);
        }

        [Fact]
        public static void RunSemaphoreSlimDynamicTest1_Wait_InternalAsync_NegativeCases()
        {
            // Invalid timeout
            RunSemaphoreSlimDynamicTest1_Wait_InternalAsync(10, 10, -10, true, typeof(ArgumentOutOfRangeException));
            RunSemaphoreSlimDynamicTest1_Wait_InternalAsync
               (10, 10, new TimeSpan(0, 0, int.MaxValue), true, typeof(ArgumentOutOfRangeException));
            RunSemaphoreSlimDynamicTest1_Wait_InternalAsync2();
        }

        [Fact]
        public static void RunSemaphoreSlimDynamicTest2_Release()
        {
            // Valid release count
            RunSemaphoreSlimDynamicTest2_Release_Internal(5, 10, 1, null);
            RunSemaphoreSlimDynamicTest2_Release_Internal(0, 10, 1, null);
            RunSemaphoreSlimDynamicTest2_Release_Internal(5, 10, 5, null);
        }

        [Fact]
        public static void RunSemaphoreSlimDynamicTest2_Release_NegativeCases()
        {
            // Invalid release count
            RunSemaphoreSlimDynamicTest2_Release_Internal(5, 10, 0, typeof(ArgumentOutOfRangeException));
            RunSemaphoreSlimDynamicTest2_Release_Internal(5, 10, -1, typeof(ArgumentOutOfRangeException));

            // Semaphore Full
            RunSemaphoreSlimDynamicTest2_Release_Internal(10, 10, 1, typeof(SemaphoreFullException));
            RunSemaphoreSlimDynamicTest2_Release_Internal(5, 10, 6, typeof(SemaphoreFullException));
            RunSemaphoreSlimDynamicTest2_Release_Internal(int.MaxValue - 1, int.MaxValue, 10, typeof(SemaphoreFullException));
        }

        [Fact]
        public static void RunSemaphoreSlimDynamicTest4_Dispose()
        {
            RunSemaphoreSlimDynamicTest4_Dispose_Internal(5, 10, null, null);
            RunSemaphoreSlimDynamicTest4_Dispose_Internal(5, 10, Actions.CurrentCount, null);
            RunSemaphoreSlimDynamicTest4_Dispose_Internal
               (5, 10, Actions.Wait, typeof(ObjectDisposedException));
            RunSemaphoreSlimDynamicTest4_Dispose_Internal
               (5, 10, Actions.WaitAsync, typeof(ObjectDisposedException));
            RunSemaphoreSlimDynamicTest4_Dispose_Internal
              (5, 10, Actions.Release, typeof(ObjectDisposedException));
            RunSemaphoreSlimDynamicTest4_Dispose_Internal
              (5, 10, Actions.AvailableWaitHandle, typeof(ObjectDisposedException));
        }

        [Fact]
        public static void RunSemaphoreSlimDynamicTest5_CurrentCount()
        {
            RunSemaphoreSlimDynamicTest5_CurrentCount_Internal(5, 10, null);
            RunSemaphoreSlimDynamicTest5_CurrentCount_Internal(5, 10, Actions.Wait);
            RunSemaphoreSlimDynamicTest5_CurrentCount_Internal(5, 10, Actions.WaitAsync);
            RunSemaphoreSlimDynamicTest5_CurrentCount_Internal(5, 10, Actions.Release);
        }

        [Fact]
        public static void RunSemaphoreSlimDynamicTest7_AvailableWaitHandle()
        {
            RunSemaphoreSlimDynamicTest7_AvailableWaitHandle_Internal(5, 10, null, true);
            RunSemaphoreSlimDynamicTest7_AvailableWaitHandle_Internal(0, 10, null, false);

            RunSemaphoreSlimDynamicTest7_AvailableWaitHandle_Internal(5, 10, Actions.Wait, true);
            RunSemaphoreSlimDynamicTest7_AvailableWaitHandle_Internal(1, 10, Actions.Wait, false);
            RunSemaphoreSlimDynamicTest7_AvailableWaitHandle_Internal(5, 10, Actions.Wait, true);

            RunSemaphoreSlimDynamicTest7_AvailableWaitHandle_Internal(5, 10, Actions.WaitAsync, true);
            RunSemaphoreSlimDynamicTest7_AvailableWaitHandle_Internal(1, 10, Actions.WaitAsync, false);
            RunSemaphoreSlimDynamicTest7_AvailableWaitHandle_Internal(5, 10, Actions.WaitAsync, true);
            RunSemaphoreSlimDynamicTest7_AvailableWaitHandle_Internal(0, 10, Actions.Release, true);
        }

        /// <summary>
        /// Test SemaphoreSlimDynamic constructor
        /// </summary>
        /// <param name="min">The minimum semaphore count</param>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="exceptionType">The type of the thrown exception in case of invalid cases,
        /// null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunSemaphoreSlimDynamicTest0_Ctor_Internal(int min, int initial, int maximum, Type exceptionType)
        {
            string methodFailed = "RunSemaphoreSlimDynamicTest0_Ctor_Internal(" + min + "," + initial + "," + maximum + "):  FAILED.  ";
            Exception exception = null;
            try
            {
                SemaphoreSlimDynamic semaphore = new SemaphoreSlimDynamic(min, initial, maximum);
                Assert.Equal(initial, semaphore.CurrentCount);
            }
            catch (Exception ex)
            {
                Assert.NotNull(exceptionType);
                Assert.IsType(exceptionType, ex);
                exception = ex;
            }
        }

        /// <summary>
        /// Test SemaphoreSlimDynamic Wait
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="timeout">The timeout parameter for the wait method, it must be either int or TimeSpan</param>
        /// <param name="returnValue">The expected wait return value</param>
        /// <param name="exceptionType">The type of the thrown exception in case of invalid cases,
        /// null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunSemaphoreSlimDynamicTest1_Wait_Internal
            (int initial, int maximum, object timeout, bool returnValue, Type exceptionType)
        {
            SemaphoreSlimDynamic semaphore = new SemaphoreSlimDynamic(0, initial, maximum);
            try
            {
                bool result = false;
                if (timeout is TimeSpan)
                {
                    result = semaphore.Wait((TimeSpan)timeout);
                }
                else
                {
                    result = semaphore.Wait((int)timeout);
                }
                Assert.Equal(returnValue, result);
                if (result)
                {
                    Assert.Equal(initial - 1, semaphore.CurrentCount);
                }
            }
            catch (Exception ex)
            {
                Assert.NotNull(exceptionType);
                Assert.IsType(exceptionType, ex);
            }
        }

        /// <summary>
        /// Test SemaphoreSlimDynamic WaitAsync
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="timeout">The timeout parameter for the wait method, it must be either int or TimeSpan</param>
        /// <param name="returnValue">The expected wait return value</param>
        /// <param name="exceptionType">The type of the thrown exception in case of invalid cases,
        /// null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunSemaphoreSlimDynamicTest1_Wait_InternalAsync
            (int initial, int maximum, object timeout, bool returnValue, Type exceptionType)
        {
            SemaphoreSlimDynamic semaphore = new SemaphoreSlimDynamic(0, initial, maximum);
            try
            {
                bool result = false;
                if (timeout is TimeSpan)
                {
                    result = semaphore.WaitAsync((TimeSpan)timeout).Result;
                }
                else
                {
                    result = semaphore.WaitAsync((int)timeout).Result;
                }
                Assert.Equal(returnValue, result);
                if (result)
                {
                    Assert.Equal(initial - 1, semaphore.CurrentCount);
                }
            }
            catch (Exception ex)
            {
                Assert.NotNull(exceptionType);
                Assert.IsType(exceptionType, ex);
            }
        }

        /// <summary>
        /// Test SemaphoreSlimDynamic WaitAsync
        /// The test verifies that SemaphoreSlimDynamic.Release() does not execute any user code synchronously.
        /// </summary>
        private static void RunSemaphoreSlimDynamicTest1_Wait_InternalAsync2()
        {
            SemaphoreSlimDynamic semaphore = new SemaphoreSlimDynamic(1, 1, 1);
            ThreadLocal<int> counter = new ThreadLocal<int>(() => 0);
            bool nonZeroObserved = false;

            const int asyncActions = 20;
            int remAsyncActions = asyncActions;
            ManualResetEvent mre = new ManualResetEvent(false);

            Action<int> doWorkAsync = async delegate (int i)
            {
                await semaphore.WaitAsync();
                if (counter.Value > 0)
                {
                    nonZeroObserved = true;
                }

                counter.Value = counter.Value + 1;
                semaphore.Release();
                counter.Value = counter.Value - 1;

                if (Interlocked.Decrement(ref remAsyncActions) == 0) mre.Set();
            };

            semaphore.Wait();
            for (int i = 0; i < asyncActions; i++) doWorkAsync(i);
            semaphore.Release();

            mre.WaitOne();

            Assert.False(nonZeroObserved, "RunSemaphoreSlimDynamicTest1_Wait_InternalAsync2:  FAILED.  SemaphoreSlimDynamic.Release() seems to have synchronously invoked a continuation.");
        }

        /// <summary>
        /// Test SemaphoreSlimDynamic Release
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="releaseCount">The release count for the release method</param>
        /// <param name="exceptionType">The type of the thrown exception in case of invalid cases,
        /// null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunSemaphoreSlimDynamicTest2_Release_Internal
           (int initial, int maximum, int releaseCount, Type exceptionType)
        {
            SemaphoreSlimDynamic semaphore = new SemaphoreSlimDynamic(0, initial, maximum);
            try
            {
                int oldCount = semaphore.Release(releaseCount);
                Assert.Equal(initial, oldCount);
                Assert.Equal(initial + releaseCount, semaphore.CurrentCount);
            }
            catch (Exception ex)
            {
                Assert.NotNull(exceptionType);
                Assert.IsType(exceptionType, ex);
            }
        }

        /// <summary>
        /// Call specific SemaphoreSlimDynamic method or property
        /// </summary>
        /// <param name="semaphore">The SemaphoreSlimDynamic instance</param>
        /// <param name="action">The action name</param>
        /// <param name="param">The action parameter, null if it takes no parameters</param>
        /// <param name="output">The test output helper to use if available.</param>
        /// <param name="outputPrefix">Prefix to add to the debug output.</param>
        /// <returns>The action return value, null if the action returns void</returns>
        private static object CallSemaphoreAction
            (SemaphoreSlimDynamic semaphore, Actions? action, object param, ITestOutputHelper output = null, string outputPrefix = null)
        {
            output?.WriteLine($"{outputPrefix ?? string.Empty}Action {action?.ToString() ?? "Unknown"}: {param?.ToString() ?? "None"}" );

            if (action == Actions.Wait)
            {
                if (param is TimeSpan)
                {
                    return semaphore.Wait((TimeSpan)param);
                }
                else if (param is int)
                {
                    return semaphore.Wait((int)param);
                }
                semaphore.Wait();
                return null;
            }
            else if (action == Actions.WaitAsync)
            {
                if (param is TimeSpan)
                {
                    return semaphore.WaitAsync((TimeSpan)param).Result;
                }
                else if (param is int)
                {
                    return semaphore.WaitAsync((int)param).Result;
                }
                semaphore.WaitAsync().Wait();
                return null;
            }
            else if (action == Actions.Release)
            {
                if (param != null)
                {
                    return semaphore.Release((int)param);
                }
                return semaphore.Release();
            }
            else if (action == Actions.Dispose)
            {
                semaphore.Dispose();
                return null;
            }
            else if (action == Actions.CurrentCount)
            {
                return semaphore.CurrentCount;
            }
            else if (action == Actions.AvailableWaitHandle)
            {
                return semaphore.AvailableWaitHandle;
            }
            else if (action == Actions.SetAvailableSlot)
            {
                return semaphore.SetAvailableSlot((int)param);
            }

            return null;
        }

        /// <summary>
        /// Test SemaphoreSlimDynamic Dispose
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="action">SemaphoreSlimDynamic action to be called after Dispose</param>
        /// <param name="exceptionType">The type of the thrown exception in case of invalid cases,
        /// null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunSemaphoreSlimDynamicTest4_Dispose_Internal(int initial, int maximum, Actions? action, Type exceptionType)
        {
            SemaphoreSlimDynamic semaphore = new SemaphoreSlimDynamic(0, initial, maximum);
            try
            {
                semaphore.Dispose();
                CallSemaphoreAction(semaphore, action, null);
            }
            catch (Exception ex)
            {
                Assert.NotNull(exceptionType);
                Assert.IsType(exceptionType, ex);
            }
        }

        /// <summary>
        /// Test SemaphoreSlimDynamic CurrentCount property
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="action">SemaphoreSlimDynamic action to be called before CurrentCount</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunSemaphoreSlimDynamicTest5_CurrentCount_Internal(int initial, int maximum, Actions? action)
        {
            SemaphoreSlimDynamic semaphore = new SemaphoreSlimDynamic(0, initial, maximum);

            CallSemaphoreAction(semaphore, action, null);
            if (action == null)
            {
                Assert.Equal(initial, semaphore.CurrentCount);
            }
            else
            {
                Assert.Equal(initial + (action == Actions.Release ? 1 : -1), semaphore.CurrentCount);
            }
        }

        /// <summary>
        /// Test SemaphoreSlimDynamic AvailableWaitHandle property
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="action">SemaphoreSlimDynamic action to be called before WaitHandle</param>
        /// <param name="state">The expected wait handle state</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunSemaphoreSlimDynamicTest7_AvailableWaitHandle_Internal(int initial, int maximum, Actions? action, bool state)
        {
            SemaphoreSlimDynamic semaphore = new SemaphoreSlimDynamic(0, initial, maximum);

            CallSemaphoreAction(semaphore, action, null);
            Assert.NotNull(semaphore.AvailableWaitHandle);
            Assert.Equal(state, semaphore.AvailableWaitHandle.WaitOne(0));
        }

        /// <summary>
        /// Test SemaphoreSlimDynamic Wait and Release methods concurrently
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="waitThreads">Number of the threads that call Wait method</param>
        /// <param name="releaseThreads">Number of the threads that call Release method</param>
        /// <param name="succeededWait">Number of succeeded wait threads</param>
        /// <param name="failedWait">Number of failed wait threads</param>
        /// <param name="finalCount">The final semaphore count</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        [Theory]
        [InlineData(5, 1000, 50, 50, 50, 0, 5, 1000)]
        [InlineData(0, 1000, 50, 25, 25, 25, 0, 500)]
        [InlineData(0, 1000, 50, 0, 0, 50, 0, 100)]
        public static void RunSemaphoreSlimDynamicTest8_ConcWaitAndRelease(int initial, int maximum,
            int waitThreads, int releaseThreads, int succeededWait, int failedWait, int finalCount, int timeout)
        {
            SemaphoreSlimDynamic semaphore = new SemaphoreSlimDynamic(0, initial, maximum);
            Task[] threads = new Task[waitThreads + releaseThreads];
            int succeeded = 0;
            int failed = 0;
            ManualResetEvent mre = new ManualResetEvent(false);
            // launch threads
            for (int i = 0; i < threads.Length; i++)
            {
                if (i < waitThreads)
                {
                    // We are creating the Task using TaskCreationOptions.LongRunning to
                    // force usage of another thread (which will be the case on the default scheduler
                    // with its current implementation).  Without this, the release tasks will likely get
                    // queued behind the wait tasks in the pool, making it very likely that the wait tasks
                    // will starve the very tasks that when run would unblock them.
                    threads[i] = new Task(delegate ()
                    {
                        mre.WaitOne();
                        if (semaphore.Wait(timeout))
                        {
                            Interlocked.Increment(ref succeeded);
                        }
                        else
                        {
                            Interlocked.Increment(ref failed);
                        }
                    }, TaskCreationOptions.LongRunning);
                }
                else
                {
                    threads[i] = new Task(delegate ()
                    {
                        mre.WaitOne();
                        semaphore.Release();
                    });
                }
                threads[i].Start(TaskScheduler.Default);
            }

            mre.Set();
            //wait work to be done;
            Task.WaitAll(threads);
            //check the number of succeeded and failed wait
            Assert.Equal(succeededWait, succeeded);
            Assert.Equal(failedWait, failed);
            Assert.Equal(finalCount, semaphore.CurrentCount);
        }

        /// <summary>
        /// Test SemaphoreSlimDynamic WaitAsync and Release methods concurrently
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="waitThreads">Number of the threads that call Wait method</param>
        /// <param name="releaseThreads">Number of the threads that call Release method</param>
        /// <param name="succeededWait">Number of succeeded wait threads</param>
        /// <param name="failedWait">Number of failed wait threads</param>
        /// <param name="finalCount">The final semaphore count</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        [Theory]
        [InlineData(5, 1000, 50, 50, 50, 0, 5, 500)]
        [InlineData(0, 1000, 50, 25, 25, 25, 0, 500)]
        [InlineData(0, 1000, 50, 0, 0, 50, 0, 100)]
        public static void RunSemaphoreSlimDynamicTest8_ConcWaitAsyncAndRelease(int initial, int maximum,
            int waitThreads, int releaseThreads, int succeededWait, int failedWait, int finalCount, int timeout)
        {
            SemaphoreSlimDynamic semaphore = new SemaphoreSlimDynamic(0, initial, maximum);
            Task[] tasks = new Task[waitThreads + releaseThreads];
            int succeeded = 0;
            int failed = 0;
            ManualResetEvent mre = new ManualResetEvent(false);
            // launch threads
            for (int i = 0; i < tasks.Length; i++)
            {
                if (i < waitThreads)
                {
                    tasks[i] = Task.Run(async delegate
                    {
                        mre.WaitOne();
                        if (await semaphore.WaitAsync(timeout))
                        {
                            Interlocked.Increment(ref succeeded);
                        }
                        else
                        {
                            Interlocked.Increment(ref failed);
                        }
                    });
                }
                else
                {
                    tasks[i] = Task.Run(delegate
                    {
                        mre.WaitOne();
                        semaphore.Release();
                    });
                }
            }

            mre.Set();
            //wait work to be done;
            Task.WaitAll(tasks);

            Assert.Equal(succeededWait, succeeded);
            Assert.Equal(failedWait, failed);
            Assert.Equal(finalCount, semaphore.CurrentCount);
        }

        [Theory]
        [InlineData(10, 10)]
        [InlineData(1, 10)]
        [InlineData(10, 1)]
        public static void RunSemaphoreSlimDynamicTest9_ConcurrentWaitAndWaitAsync(int syncWaiters, int asyncWaiters)
        {
            int totalWaiters = syncWaiters + asyncWaiters;

            var semaphore = new SemaphoreSlimDynamic(0, 0, int.MaxValue);
            Task[] tasks = new Task[totalWaiters];

            const int ITERS = 10;
            int randSeed = unchecked((int)DateTime.Now.Ticks);
            for (int i = 0; i < syncWaiters; i++)
            {
                tasks[i] = Task.Run(delegate
                {
                    //Random rand = new Random(Interlocked.Increment(ref randSeed));
                    for (int iter = 0; iter < ITERS; iter++)
                    {
                        semaphore.Wait();
                        semaphore.Release();
                    }
                });
            }
            for (int i = syncWaiters; i < totalWaiters; i++)
            {
                tasks[i] = Task.Run(async delegate
                {
                    //Random rand = new Random(Interlocked.Increment(ref randSeed));
                    for (int iter = 0; iter < ITERS; iter++)
                    {
                        await semaphore.WaitAsync();
                        semaphore.Release();
                    }
                });
            }

            semaphore.Release(totalWaiters / 2);
            Task.WaitAll(tasks);
        }

        [Fact]
        public static void RunSemaphoreSlimDynamicTest10_SetAvailableSlot()
        {
            // Setting same slot count does nothing
            RunSemaphoreSlimDynamicTest10_Internal(0, 0, 1, 0, null,
                (Actions.SetAvailableSlot, 0, false, 0));

            // Increasing slot count & decrease slot count should work
            RunSemaphoreSlimDynamicTest10_Internal(0, 0, 1, 0, null,
                (Actions.SetAvailableSlot, 1, true, 1),
                (Actions.SetAvailableSlot, 0, true, 0));

            // Decrease slot count to 0. Wait should fail.
            RunSemaphoreSlimDynamicTest10_Internal(0, 1, 1, 1, null,
                (Actions.SetAvailableSlot, 0, true, 0),
                (Actions.Wait, 0, false, 0));

            // Increase slot count to max. Wait should work and reduce currCount
            RunSemaphoreSlimDynamicTest10_Internal(0, 1, 2, 1, null,
                (Actions.SetAvailableSlot, 2, true, 2),
                (Actions.WaitAsync, 0, true, 1));

            // Increase beyond maximum should fail with exception
            RunSemaphoreSlimDynamicTest10_Internal(0, 1, 2, 1, typeof(ArgumentOutOfRangeException),
                (Actions.SetAvailableSlot, 3, true, 2));

            // Decrease beyond minimum should fail with exception
            RunSemaphoreSlimDynamicTest10_Internal(1, 2, 2, 2, typeof(ArgumentOutOfRangeException),
                (Actions.SetAvailableSlot, 0, true, 1));

            // Start with 1 slot. Wait so currCount is 0, then decrease to 0 available slot should work
            RunSemaphoreSlimDynamicTest10_Internal(0, 1, 1, 1, null,
                (Actions.Wait, 0, true, 0),
                (Actions.SetAvailableSlot, 0, false, 0));

            // Start with 2 slots. Wait so currCount is 1, then decrease to 0 available slot should work
            RunSemaphoreSlimDynamicTest10_Internal(0, 2, 2, 2, null,
                (Actions.Wait, 0, true, 1),
                (Actions.SetAvailableSlot, 0, true, 0));

            // Occupy all slots, then decrease to 1 available s1ot. Subsequent release should allow up to 1 slot
            RunSemaphoreSlimDynamicTest10_Internal(0, 2, 2, 2, null,
                (Actions.Wait, 0, true, 1),
                (Actions.Wait, 0, true, 0),
                (Actions.SetAvailableSlot, 1, false, 0),
                (Actions.Release, null, 0, 0),
                (Actions.Release, null, 0, 1));

            // Occupy all slots, then decrease to 1 available s1ot. Subsequent release should allow up to 1 slot with Release(num)
            RunSemaphoreSlimDynamicTest10_Internal(0, 2, 2, 2, null,
                (Actions.Wait, 0, true, 1),
                (Actions.Wait, 0, true, 0),
                (Actions.SetAvailableSlot, 1, false, 0),
                (Actions.Release, 2, 0, 1));

            // Occupy all slots, then decrease to 1 available s1ot then back to 2. All slots should still be occupied.
            RunSemaphoreSlimDynamicTest10_Internal(0, 2, 2, 2, null,
                (Actions.Wait, 0, true, 1),
                (Actions.Wait, 0, true, 0),
                (Actions.SetAvailableSlot, 1, false, 0),
                (Actions.SetAvailableSlot, 2, false, 0),
                (Actions.Release, 2, 0, 2));

            // Occupy all slots, then increase to 4 available s1ot then back to 2. All slots should still be occupied.
            RunSemaphoreSlimDynamicTest10_Internal(0, 2, 4, 2, null,
                (Actions.Wait, 0, true, 1),
                (Actions.Wait, 0, true, 0),
                (Actions.SetAvailableSlot, 4, true, 2),
                (Actions.SetAvailableSlot, 2, true, 0),
                (Actions.Release, 2, 0, 2));

            // Setting slot count, release to gain 2 more slots then waits
            RunSemaphoreSlimDynamicTest10_Internal(0, 2, 5, 2, null,
                (Actions.SetAvailableSlot, 3, true, 3),
                (Actions.Release, 2, 3, 5),
                (Actions.Wait, 0, true, 4),
                (Actions.Wait, 0, true, 3));
        }

        private static void RunSemaphoreSlimDynamicTest10_Internal(
                int min, int initial, int max, int initCount, Type exceptionType,
                params (Actions action, object param, object result, int changedCount)[] actions)
        {
            var semaphore = new SemaphoreSlimDynamic(min, initial, max);

            Assert.Equal(min, semaphore.MinimumSlotsCount);
            Assert.Equal(initial, semaphore.AvailableSlotsCount);
            Assert.Equal(max, semaphore.MaximumSlotsCount);

            Assert.Equal(initCount, semaphore.CurrentCount);

            try
            {
                for (int i = 0; i < actions.Length; i++)
                {
                    // Action
                    Assert.Equal(actions[i].result, CallSemaphoreAction(semaphore, actions[i].action, actions[i].param));
                    if (actions[i].action == Actions.SetAvailableSlot)
                    {
                        Assert.Equal(actions[i].param, semaphore.AvailableSlotsCount);
                    }
                    Assert.Equal(actions[i].changedCount, semaphore.CurrentCount);
                }

                Assert.Null(exceptionType);
            }
            catch (Exception ex) when (ex.GetType() == exceptionType)
            {
                Assert.NotNull(exceptionType);
                Assert.IsType(exceptionType, ex);
            }
        }

        [Fact]
        public void RunSemaphoreSlimDynamicTest11_ConcurrentSetAvailableSlot()
        {
            this.testOutputHelper.WriteLine("Setting same slot count does nothing");
            RunSemaphoreSlimDynamicTest11_Internal(0, 0, 1, 0, 0,
                (Actions.SetAvailableSlot, 0));

            this.testOutputHelper.WriteLine("\nSetting same slot count multiple times should result the same");
            RunSemaphoreSlimDynamicTest11_Internal(0, 0, 10, 0, 5,
                (Actions.SetAvailableSlot, 5),
                (Actions.SetAvailableSlot, 5),
                (Actions.SetAvailableSlot, 5));

            this.testOutputHelper.WriteLine("\nSetting slot count and release at the same time");
            RunSemaphoreSlimDynamicTest11_Internal(0, 1, 3, 1, 3,
                (Actions.SetAvailableSlot, 2),
                (Actions.Release, null));

            this.testOutputHelper.WriteLine("\nSetting slot count and wait at the same time so final count should be 1");
            RunSemaphoreSlimDynamicTest11_Internal(0, 1, 2, 1, 1,
                (Actions.SetAvailableSlot, 2),
                (Actions.WaitAsync, null));

            this.testOutputHelper.WriteLine("\nSetting slot count, wait and release at the same time");
            RunSemaphoreSlimDynamicTest11_Internal(0, 1, 3, 1, 2,
                (Actions.SetAvailableSlot, 2),
                (Actions.Release, null),
                (Actions.Wait, 0));

            this.testOutputHelper.WriteLine("\nSetting slot count, wait and release at the same time");
            RunSemaphoreSlimDynamicTest11_Internal(0, 2, 5, 2, 3,
                (Actions.SetAvailableSlot, 3),
                (Actions.Wait, 0),
                (Actions.Wait, 0),
                (Actions.Release, 2));
        }

        private void RunSemaphoreSlimDynamicTest11_Internal(
            int min, int initial, int max, int initCount, int finalCount,
            params (Actions action, object param)[] actions)
        {
            var semaphore = new SemaphoreSlimDynamic(min, initial, max);

            Assert.Equal(min, semaphore.MinimumSlotsCount);
            Assert.Equal(initial, semaphore.AvailableSlotsCount);
            Assert.Equal(max, semaphore.MaximumSlotsCount);
            Assert.Equal(initCount, semaphore.CurrentCount);

            this.testOutputHelper.WriteLine($"initCount={semaphore.CurrentCount}");

            Task[] tasks = new Task[actions.Length];
            
            ManualResetEvent mre = new ManualResetEvent(false);
            // launch threads
            for (int i = 0; i < tasks.Length; i++)
            {
                int index = i;

                tasks[i] = Task.Run(delegate
                {
                    mre.WaitOne();

                    // Action
                    CallSemaphoreAction(semaphore, actions[index].action, actions[index].param, this.testOutputHelper, $"({index}) ");
                    if (actions[index].action == Actions.SetAvailableSlot)
                    {
                        Assert.Equal(actions[index].param, semaphore.AvailableSlotsCount);
                    }

                    this.testOutputHelper.WriteLine($"({index}) currentCount={semaphore.CurrentCount}");
                });
            }

            mre.Set();
            
            //wait work to be done;
            Task.WaitAll(tasks);

            this.testOutputHelper.WriteLine($"finalCount={finalCount} currentCount={semaphore.CurrentCount}");

            Assert.Equal(finalCount, semaphore.CurrentCount);
        }
    }
}