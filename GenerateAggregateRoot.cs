using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

public class GenerateAggregateRoot
{
    private readonly string _baseOutputPath;
    private readonly string _modelName;
    private readonly string _pluralName;  // Yeni eklendi
    private readonly List<PropertyInfo> _properties;
    private readonly List<RelationshipInfo> _relationships;
    private readonly List<string> _interfaces;

    public class PropertyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string AccessModifier { get; set; }
        public bool IsPrivateSet { get; set; }
    }

    public class RelationshipInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsReadOnly { get; set; }
        public string RelatedType { get; set; }
    }

    public GenerateAggregateRoot(string modelName, string baseOutputPath, string pluralName)  // Constructor güncellendi
    {
        _modelName = modelName;
        _baseOutputPath = baseOutputPath;
        _pluralName = pluralName;
        _properties = new List<PropertyInfo>();
        _relationships = new List<RelationshipInfo>();
        _interfaces = new List<string>();
    }

    public void ParseModel(string modelContent)
    {
        // Parse interfaces
        var interfaceRegex = new Regex(@":\s*(.*?)\s*{");
        var interfaceMatch = interfaceRegex.Match(modelContent);
        if (interfaceMatch.Success)
        {
            _interfaces.AddRange(interfaceMatch.Groups[1].Value.Split(',').Select(i => i.Trim()));
        }

        // Parse properties
        var propertyRegex = new Regex(@"public\s+(\w+[\?]?)\s+(\w+)\s*{\s*get;\s*(private\s*)?set;\s*}");
        var propertyMatches = propertyRegex.Matches(modelContent);
        foreach (Match match in propertyMatches)
        {
            _properties.Add(new PropertyInfo
            {
                Type = match.Groups[1].Value,
                Name = match.Groups[2].Value,
                AccessModifier = "public",
                IsPrivateSet = match.Groups[3].Success
            });
        }

        // Parse relationships
        var relationshipRegex = new Regex(@"(public|internal)\s+(virtual\s+)?(ICollection|IReadOnlyCollection|List)<(\w+)>\s+(\w+)\s*{\s*get;");
        var relationshipMatches = relationshipRegex.Matches(modelContent);
        foreach (Match match in relationshipMatches)
        {
            _relationships.Add(new RelationshipInfo
            {
                Type = match.Groups[3].Value,
                RelatedType = match.Groups[4].Value,
                Name = match.Groups[5].Value,
                IsReadOnly = match.Groups[3].Value == "IReadOnlyCollection"
            });
        }
    }

    public void Generate()
    {
        CreateDirectoryStructure();
        GenerateMainClass();
        GenerateLanguageClass();
        GenerateInterfaces();
        GenerateImplementations();
        Console.WriteLine("Domain sınıfları başarıyla oluşturuldu.");
        Console.WriteLine($"Konum: {_baseOutputPath}");
    }

    private void CreateDirectoryStructure()
    {
        // Model klasörünün içine Contacts ve Implementations klasörlerini oluştur
        Directory.CreateDirectory(Path.Combine(_baseOutputPath, "Contacts"));
        Directory.CreateDirectory(Path.Combine(_baseOutputPath, "Implementations"));
    }

    private void GenerateMainClass()
    {
        // Ana model sınıfı zaten var, bu metodu atlayalım
        return;
    }

    private void GenerateLanguageClass()
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Abis.Core.DataAccess;");
        sb.AppendLine();
        sb.AppendLine($"namespace Bapsis.Api.Domain.AggregateRoots.{_pluralName};");  
        sb.AppendLine();
        sb.AppendLine($"public class {_modelName}Language : AuditEntity, IEntityTranslation<{_modelName}>");
        sb.AppendLine("{");
        sb.AppendLine("    public int CoreId { get; set; }");
        sb.AppendLine("    public string Language { get; set; }");
        sb.AppendLine($"    public virtual {_modelName} Core {{ get; set; }}");
        sb.AppendLine("    public string Name { get; set; }");
        sb.AppendLine("    public string Description { get; set; }");
        sb.AppendLine("}");

        File.WriteAllText(Path.Combine(_baseOutputPath, $"{_modelName}Language.cs"), sb.ToString());
    }

    private void GenerateInterfaces()
    {
        GenerateCommandRepository();
        GenerateQueryRepository();
        GenerateSpecification();
        GenerateDomainService();
    }

    private void GenerateImplementations()
    {
        GenerateDomainServiceImplementation();
        GenerateSpecificationImplementation();
    }

    private void GenerateCommandRepository()
    {
        var content = $@"using Bapsis.Api.Domain.Repositories;

namespace Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Contacts;  

public interface I{_modelName}CommandRepository : ICommandRepository<{_modelName}> {{ }}";

        File.WriteAllText(Path.Combine(_baseOutputPath, "Contacts", $"I{_modelName}CommandRepository.cs"), content);
    }

    private void GenerateQueryRepository()
    {
        var content = $@"using Bapsis.Api.Domain.Repositories;

namespace Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Contacts;  

public interface I{_modelName}QueryRepository : IQueryRepository<{_modelName}> {{ }}";

        File.WriteAllText(Path.Combine(_baseOutputPath, "Contacts", $"I{_modelName}QueryRepository.cs"), content);
    }

    private void GenerateSpecification()
    {
        var content = $@"using System.Linq.Expressions;
using Bapsis.Api.Domain.Specifications.Contacts;

namespace Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Contacts;  

public interface I{_modelName}Specification : IBaseSpecification<{_modelName}>
{{
    Expression<Func<{_modelName}, bool>> ById(int id);
}}";

        File.WriteAllText(Path.Combine(_baseOutputPath, "Contacts", $"I{_modelName}Specification.cs"), content);
    }

    private void GenerateDomainService()
    {
        var sb = new StringBuilder();
        sb.AppendLine($@"using Bapsis.Api.Domain.DomainServices.Contacts;
using Bapsis.Api.Domain.Models;

namespace Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Contacts;  

public interface I{_modelName}DomainService : IBaseDomainService
{{
    #region setters
");

        foreach (var prop in _properties)
        {
            sb.AppendLine($"    public void Set{prop.Name}({_modelName} {_modelName.ToCamelCase()}, {prop.Type} {prop.Name.ToCamelCase()});");
        }
        
        sb.AppendLine($@"    public void SetNameTranslations({_modelName} {_modelName.ToCamelCase()}, ICollection<TranslationModel> names);
    
    #endregion");
        sb.AppendLine();
        sb.AppendLine($@"    #region create

    {_modelName} Create(int id, int order, ICollection<TranslationModel> names);

    #endregion
}}");

        File.WriteAllText(Path.Combine(_baseOutputPath, "Contacts", $"I{_modelName}DomainService.cs"), sb.ToString());
    }

    private void GenerateDomainServiceImplementation()
    {
        var sb = new StringBuilder();
        sb.AppendLine($@"using Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Contacts;  
using Bapsis.Api.Domain.DomainServices.Implementations;
using Bapsis.Api.Domain.Extensions;
using Bapsis.Api.Domain.Models;
using Abis.Core.Specifications;

namespace Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Implementations; 

public class {_modelName}DomainService : BaseDomainService, I{_modelName}DomainService
{{
    #region injections

    public I{_modelName}QueryRepository {_modelName}QueryRepository {{ get; set; }}
    public I{_modelName}Specification {_modelName}Specifications {{ get; set; }}

    #endregion

    #region setters
");

        foreach (var prop in _properties)
        {
            sb.AppendLine($"    public void Set{prop.Name}({_modelName} {_modelName.ToCamelCase()}, {prop.Type} {prop.Name.ToCamelCase()}) => {_modelName.ToCamelCase()}.Set{prop.Name}({prop.Name.ToCamelCase()});");
        }

        sb.AppendLine($@"    public void SetNameTranslations({_modelName} {_modelName.ToCamelCase()}, ICollection<TranslationModel> names)
    {{
        {_modelName.ToCamelCase()}.Translations = {_modelName.ToCamelCase()}.Translations.SetTranslations(
            {_modelName.ToCamelCase()}.Id,
            names,
            t => t.CoreId,
            t => t.Language,
            nameof({_modelName}Language.Name),
            (id, language, value) => new {_modelName}Language {{ CoreId = id, Language = language, Name = value }}
        );
    }}

    #endregion

    #region create

    public {_modelName} Create(int id, int order, ICollection<TranslationModel> names)
    {{
        var {_modelName.ToCamelCase()} = {_modelName}.Create();
        SetId({_modelName.ToCamelCase()}, id);
        SetOrder({_modelName.ToCamelCase()}, order);
        SetNameTranslations({_modelName.ToCamelCase()}, names);
        return {_modelName.ToCamelCase()};
    }}

    #endregion
}}");

        File.WriteAllText(Path.Combine(_baseOutputPath, "Implementations", $"{_modelName}DomainService.cs"), sb.ToString());
    }

    private void GenerateSpecificationImplementation()
    {
        var content = $@"using System.Linq.Expressions;
using Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Contacts;  
using Bapsis.Api.Domain.Specifications.Implementations;

namespace Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Implementations;  

public class {_modelName}Specification : BaseSpecification<{_modelName}>, I{_modelName}Specification
{{
    public Expression<Func<{_modelName}, bool>> ById(int id) => GenericSpecification.ById<{_modelName}, int>(id);
}}";

        File.WriteAllText(Path.Combine(_baseOutputPath, "Implementations", $"{_modelName}Specification.cs"), content);
    }
}

public static class StringExtensions
{
    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }
}
