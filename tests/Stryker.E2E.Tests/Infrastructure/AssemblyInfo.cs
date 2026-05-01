using Xunit;

// Sprint 21 D3: every E2E test runs sequentially because each Stryker subprocess
// writes into the shared samples/StrykerOutput/ tree. Parallel runs would race
// on Sample.Library obj/bin during compile and on report output paths.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
