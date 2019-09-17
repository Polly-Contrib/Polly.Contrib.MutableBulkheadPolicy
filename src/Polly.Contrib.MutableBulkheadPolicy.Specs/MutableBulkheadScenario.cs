// Copyright (c) Microsoft Corporation.
// Licensed under the BSD-3-Clause.

namespace Polly.Contrib.MutableBulkheadPolicy.Specs
{
    internal struct MutableBulkheadScenario
    {
        readonly int _maxParallelization;
        readonly int _maxQueuingActions;
        readonly int _totalTestLoad;
        readonly string _scenario;
        readonly bool _cancelQueuing;
        readonly bool _cancelExecuting;
        readonly int _updateMaxParalelizationDelta;
        readonly int _updateQueuingActionsDelta;

        public MutableBulkheadScenario(int maxParallelization, int maxQueuingActions, int totalTestLoad, bool cancelQueuing, bool cancelExecuting, int updateMaxParalelizationDelta, int updateQueuingActionsDelta, string scenario)
        {
            _maxParallelization = maxParallelization;
            _maxQueuingActions = maxQueuingActions;
            _totalTestLoad = totalTestLoad;
            _scenario = scenario;
            _cancelQueuing = cancelQueuing;
            _cancelExecuting = cancelExecuting;
            _updateMaxParalelizationDelta = updateMaxParalelizationDelta;
            _updateQueuingActionsDelta = updateQueuingActionsDelta;
        }

        public object[] ToTheoryData()
        {
            return new object[] { _maxParallelization, _maxQueuingActions, _totalTestLoad, _cancelQueuing, _cancelExecuting, _updateMaxParalelizationDelta, _updateQueuingActionsDelta, _scenario };
        }
    }
}
