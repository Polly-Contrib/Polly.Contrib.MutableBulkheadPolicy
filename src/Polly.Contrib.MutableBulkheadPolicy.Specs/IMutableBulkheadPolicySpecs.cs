// Copyright (c) Microsoft Corporation.
// Licensed under the BSD-3-Clause.

using FluentAssertions;
using Xunit;

namespace Polly.Contrib.MutableBulkheadPolicy.Specs
{
    public class IMutableBulkheadPolicySpecs
    {
        [Fact]
        public void Should_be_able_to_use_BulkheadAvailableCount_via_interface()
        {
            IMutableBulkheadPolicy MutableBulkhead = MutableBulkheadPolicy.Create(20, 10);

            MutableBulkhead.BulkheadAvailableCount.Should().Be(20);
        }

        [Fact]
        public void Should_be_able_to_use_QueueAvailableCount_via_interface()
        {
            IMutableBulkheadPolicy MutableBulkhead = MutableBulkheadPolicy.Create(20, 10);

            MutableBulkhead.QueueAvailableCount.Should().Be(10);
        }

        [Fact]
        public void Should_be_able_to_set_MaxParallelization_via_interface()
        {
            IMutableBulkheadPolicy MutableBulkhead = MutableBulkheadPolicy.Create(20, 10);

            MutableBulkhead.MaxParallelization = 30;
            MutableBulkhead.MaxParallelization.Should().Be(30);
            MutableBulkhead.BulkheadAvailableCount.Should().Be(30);
        }

        [Fact]
        public void Should_be_able_to_set_MaxQueueingActions_via_interface()
        {
            IMutableBulkheadPolicy MutableBulkhead = MutableBulkheadPolicy.Create(20, 10);

            MutableBulkhead.MaxQueueingActions = 30;
            MutableBulkhead.MaxQueueingActions.Should().Be(30);
            MutableBulkhead.QueueAvailableCount.Should().Be(30);
        }
    }
}
