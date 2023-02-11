using Aquc.Configuration.Abstractions;
using Newtonsoft.Json;
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
            using var file = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            currentValue = await System.Text.Json.JsonSerializer.DeserializeAsync<T>(file);
        }
        else
        {
            using var file = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
            await WriteDefaultValue(file);
            currentValue = defaultValue;
        }
        return this;
    }
    protected virtual async Task WriteDefaultValue(FileStream file)
    {
        if (defaultValue == null)
            throw new NullReferenceException("Should call SetDefault() first when the configuration file is not existed.");
        await System.Text.Json.JsonSerializer.SerializeAsync(file, defaultValue);
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
    public virtual ConfigurationDefaultSource<T> BuildDefault()
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

    public void Save()
    {
        using var file = new FileStream(filePath, FileMode.Truncate, FileAccess.Write);
        using var writer = new StreamWriter(file);
        writer.Write(JsonConvert.SerializeObject(Data,Formatting.Indented));
        //JsonSerializer.Serialize(file, Data,new JsonSerializerOptions
        //{
        //    ReferenceHandler=ReferenceHandler.IgnoreCycles
        //});
    }

    public IConfigurationFlow<T> GetFlow()
    {
        return new ConfigurationDefaultFlow<T>(this);
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
        Source.Save();
    }
}