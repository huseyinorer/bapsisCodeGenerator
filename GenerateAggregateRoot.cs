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
    private readonly string _pluralName;
    private readonly List<PropertyInfo> _properties;
    private readonly List<RelationshipInfo> _relationships;
    private readonly List<string> _interfaces;
    private readonly string _idType;
    private readonly bool _hasMultiLanguageSupport;

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

    public GenerateAggregateRoot(string modelName, string baseOutputPath, string pluralName, string idType, bool hasMultiLanguageSupport)
    {
        _modelName = modelName;
        _baseOutputPath = baseOutputPath;
        _pluralName = pluralName;
        _idType = idType;
        _hasMultiLanguageSupport = hasMultiLanguageSupport;
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
        if (_hasMultiLanguageSupport)
        {
            GenerateLanguageClass();
        }
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
        sb.AppendLine();
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
    Expression<Func<{_modelName}, bool>> ById({_idType} id);
}}";

        File.WriteAllText(Path.Combine(_baseOutputPath, "Contacts", $"I{_modelName}Specification.cs"), content);
    }

    private void GenerateDomainService()
    {
        var sb = new StringBuilder();
        sb.AppendLine($@"using Bapsis.Api.Domain.DomainServices.Contacts;
    using Bapsis.Api.Domain.Models;
    using System;

    namespace Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Contacts;

    public interface I{_modelName}DomainService : IBaseDomainService
    {{
        #region setters");

        // Her property için setter metod tanımı
        foreach (var prop in _properties)
        {
            sb.AppendLine($@"
        public void Set{prop.Name}({_modelName} {_modelName.ToCamelCase()}, {prop.Type} {prop.Name.ToCamelCase()});");
        }

        if (_hasMultiLanguageSupport)
        {
            sb.AppendLine($@"
        public void SetNameTranslations({_modelName} {_modelName.ToCamelCase()}, ICollection<TranslationModel> names);");
        }


        sb.AppendLine($@"
        #endregion

        #region create

        {_modelName} Create({string.Join(", ", _properties.Select(p => $"{p.Type} {p.Name.ToCamelCase()}"))}{(_interfaces.Any(i => i.Contains("IMultiLanguageEntity")) ? ", ICollection<TranslationModel> names" : "")});

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
    using System;

    namespace Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Implementations; 

    public class {_modelName}DomainService : BaseDomainService, I{_modelName}DomainService
    {{
        #region injections

        public I{_modelName}QueryRepository {_modelName}QueryRepository {{ get; set; }}
        public I{_modelName}Specification {_modelName}Specifications {{ get; set; }}

        #endregion

        #region setters");

        // Her property için setter implementasyonu
        foreach (var prop in _properties)
        {
            sb.AppendLine($@"
        public void Set{prop.Name}({_modelName} {_modelName.ToCamelCase()}, {prop.Type} {prop.Name.ToCamelCase()}) => {_modelName.ToCamelCase()}.Set{prop.Name}({prop.Name.ToCamelCase()});");
        }

        // Eğer çoklu dil desteği varsa
        if (_interfaces.Any(i => i.Contains("IMultiLanguageEntity")))
        {
            sb.AppendLine($@"
        public void SetNameTranslations({_modelName} {_modelName.ToCamelCase()}, ICollection<TranslationModel> names)
        {{
            {_modelName.ToCamelCase()}.Translations = {_modelName.ToCamelCase()}.Translations.SetTranslations(
                {_modelName.ToCamelCase()}.Id,
                names,
                t => t.CoreId,
                t => t.Language,
                nameof({_modelName}Language.Name),
                (id, language, value) => new {_modelName}Language {{ CoreId = id, Language = language, Name = value }}
            );
        }}");
        }

        sb.AppendLine($@"
        #endregion

        #region create

        public {_modelName} Create({string.Join(", ", _properties.Select(p => $"{p.Type} {p.Name.ToCamelCase()}"))}{(_interfaces.Any(i => i.Contains("IMultiLanguageEntity")) ? ", ICollection<TranslationModel> names" : "")})
        {{
            var {_modelName.ToCamelCase()} = {_modelName}.Create();");

        // Her property için setter çağrısı
        foreach (var prop in _properties)
        {
            sb.AppendLine($"        Set{prop.Name}({_modelName.ToCamelCase()}, {prop.Name.ToCamelCase()});");
        }

        // Eğer çoklu dil desteği varsa
        if (_interfaces.Any(i => i.Contains("IMultiLanguageEntity")))
        {
            sb.AppendLine($"        SetNameTranslations({_modelName.ToCamelCase()}, names);");
        }

        sb.AppendLine($@"        return {_modelName.ToCamelCase()};
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
    public Expression<Func<{_modelName}, bool>> ById({_idType} id) => GenericSpecification.ById<{_modelName}, {_idType}>(id);
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
