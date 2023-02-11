using Aquc.Configuration.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Aquc.Configuration;

public class ConfigurationDefaultManifest : IConfigurationManifest
{
    public ConfigurationDefaultManifest(int ver) => version = ver;
    public int version { get; set; }
}
public class ConfigurationBuilder<T> where T:IConfigurationStruct
{
    public string? filePath;
    public T? defaultValue;
    public T? currentValue;

    private ConfigurationBuilder() { }

    public static ConfigurationBuilder<T> Create() =>new ();
    public virtual async Task<ConfigurationBuilder<T>> BindJsonAsync(string fileName)
    {
        filePath = fileName;
        if (File.Exists(fileName))
        {
            var file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            currentValue = await JsonSerializer.DeserializeAsync<T>(file);
        }
        else
        {
            var file = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
            await WriteDefaultValue(file);
            currentValue = defaultValue;
        }
        return this;
    }
    protected virtual async Task WriteDefaultValue(FileStream file)
    {
        if (defaultValue == null)
            throw new NullReferenceException("Should call SetDefault() first when the configuration file is not existed.");
        await JsonSerializer.SerializeAsync(file, defaultValue);
    }
    public virtual ConfigurationBuilder<T> SetDefault(Func<T> func) 
    {
        defaultValue=func.Invoke();
        return this;
    }
    public virtual ConfigurationBuilder<T> SetDefault(T defaultValue)
    {
        this.defaultValue = defaultValue;
        return this;
    }
    public virtual IConfigurationSource<T> Build()
    {
        if (filePath == null)
            throw new NullReferenceException("Should call BindJson() first.");
        if (currentValue == null)
            throw new NullReferenceException("Should call BindJson() first.");
        return new ConfigurationDefaultSource<T>(filePath, currentValue);
    }
}

public class ConfigurationDefaultSource<T> : IConfigurationSource<T> where T:IConfigurationStruct
{
    protected string filePath;
    public ConfigurationDefaultSource(string filePath, T data) => (Data, this.filePath) = (data, filePath);
    public T Data { get; set; }

    public async Task SaveAsync()
    {
        using var file = new FileStream(filePath, FileMode.Open, FileAccess.Write);
        await JsonSerializer.SerializeAsync(file, Data);
    }

    public IConfigurationFlow<T> GetFlow()
    {
        throw new NotImplementedException();
    }
}
public class ConfigurationDefaultFlow<T> : IConfigurationFlow<T> where T : IConfigurationStruct
{
    public T Data => Source.Data;
    public ConfigurationDefaultFlow(IConfigurationSource<T> source) => Source = source;
    public IConfigurationSource<T> Source { get; set; }

    public void Dispose()
    {
        Save();
        GC.SuppressFinalize(this);
    }

    public void Save()
    {
        Source.SaveAsync();
    }
}