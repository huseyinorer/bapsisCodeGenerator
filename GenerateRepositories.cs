public class GenerateRepositories
{
    private readonly string _modelPath;
    private readonly string _modelName;
    private readonly string _pluralName;

    public GenerateRepositories(string modelPath, string modelName, string pluralName)
    {
        _modelPath = modelPath;
        _modelName = modelName;
        _pluralName = pluralName;
    }

    public void Generate()
    {
        try
        {
            // Domain projesinin bulunduğu yolu al
            var domainProjectPath = GetDomainProjectPath();
            
            // Data projesinin yolunu hesapla
            var dataProjectPath = domainProjectPath.Replace("Domain", "Data");
            
            // Repository'lerin oluşturulacağı klasör yolu
            var repositoryPath = Path.Combine(dataProjectPath, "Repositories", _pluralName);
            
            // Klasörü oluştur
            Directory.CreateDirectory(repositoryPath);

            // Command ve Query repository'leri oluştur
            GenerateCommandRepository(repositoryPath);
            GenerateQueryRepository(repositoryPath);

            Console.WriteLine($"Repository sınıfları başarıyla oluşturuldu.");
            Console.WriteLine($"Konum: {repositoryPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Repository oluşturulurken hata oluştu: {ex.Message}");
        }
    }

    private string GetDomainProjectPath()
    {
        // AggregateRoots'a kadar olan yolu al
        var index = _modelPath.IndexOf("AggregateRoots");
        if (index == -1)
            throw new Exception("Model dosyası AggregateRoots klasörü altında değil.");

        return _modelPath.Substring(0, index);
    }

    private void GenerateCommandRepository(string repositoryPath)
    {
        var template = $@"using Bapsis.Api.Domain.AggregateRoots.{_pluralName};
using Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Contacts;

namespace Bapsis.Api.Data.Repositories.{_pluralName};

public class {_modelName}CommandRepository : CommandRepositoryBase<BapsisContext, {_modelName}>, I{_modelName}CommandRepository
{{
    #region ctor
    public {_modelName}CommandRepository(BapsisContext dbContext) : base(dbContext) {{ }}
    #endregion
}}";

        var filePath = Path.Combine(repositoryPath, $"{_modelName}CommandRepository.cs");
        File.WriteAllText(filePath, template);
    }

    private void GenerateQueryRepository(string repositoryPath)
    {
        var template = $@"using Bapsis.Api.Domain.AggregateRoots.{_pluralName};
using Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Contacts;

namespace Bapsis.Api.Data.Repositories.{_pluralName};

public class {_modelName}QueryRepository : QueryRepositoryBase<BapsisContext, {_modelName}>, I{_modelName}QueryRepository
{{
    #region ctor
    public {_modelName}QueryRepository(BapsisContext dbContext) : base(dbContext) {{ }}
    #endregion
}}";

        var filePath = Path.Combine(repositoryPath, $"{_modelName}QueryRepository.cs");
        File.WriteAllText(filePath, template);
    }
}