namespace Stryker.Configuration.Options;

public interface IInput<T> : IInput
{
    T SuppliedInput { get; set; }
}
