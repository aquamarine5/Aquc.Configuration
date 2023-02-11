namespace Aquc.Configuration.Abstractions;

public interface IConfigurationStruct
{
    public IConfigurationManifest ConfigurationManifest { get; set; }
}
public interface IConfigurationManifest
{
    public int version { get; set; }
}
public interface IConfigurationFlow<T>:IDisposable where T : IConfigurationStruct
{
    public IConfigurationSource<T> Source { get; set; }
    public T Data { get; }
    public void Save();
}
public interface IConfigurationSource<T> where T : IConfigurationStruct
{
    public IConfigurationFlow<T> GetFlow();
    public T Data { get; set; }
    public Task SaveAsync();
}