using System.Text.RegularExpressions;
using System.Text;

public class GenerateUnitTest
{
    private readonly string _modelPath;
    private readonly string _modelName;
    private readonly string _pluralName;
    private readonly string _domainServicePath;
    private readonly string _idType;
    private readonly bool _hasMultiLanguageSupport;
    private List<MethodInfo> _setterMethods;
    private MethodInfo _createMethod;

    public class MethodInfo
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public List<ParameterInfo> Parameters { get; set; } = new();
    }

    public class ParameterInfo
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }

    public GenerateUnitTest(string modelPath, string modelName, string pluralName, string idType, bool hasMultiLanguageSupport)
    {
        _modelPath = modelPath;
        _modelName = modelName;
        _pluralName = pluralName;
        _setterMethods = new List<MethodInfo>();
        _domainServicePath = GetDomainServicePath();
        _idType = idType;
        _hasMultiLanguageSupport = hasMultiLanguageSupport;
    }

    private string GetDomainServicePath()
    {
        var basePath = Path.GetDirectoryName(_modelPath);
        return Path.Combine(basePath, "Implementations", $"{_modelName}DomainService.cs");
    }

    public void Generate()
    {
        try
        {
            // Domain Service'i analiz et
            AnalyzeDomainService();

            // Test klasör yapısını oluştur
            var testBasePath = CreateTestDirectoryStructure();

            // Test sınıfını oluştur
            GenerateTestClass(testBasePath);

            Console.WriteLine("Unit test sınıfı başarıyla oluşturuldu.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unit test oluşturulurken hata: {ex.Message}");
        }
    }

    private void AnalyzeDomainService()
    {
        var content = File.ReadAllText(_domainServicePath);

        // Setter metodlarını bul
        var methodRegex = new Regex(@"public\s+(async\s+Task\s*<?\s*void\s*>?|void)\s+Set(\w+)\s*\(([\s\S]*?)\)\s*=>");
        var matches = methodRegex.Matches(content);

        foreach (Match match in matches)
        {
            var methodName = "Set" + match.Groups[2].Value;
            var returnType = match.Groups[1].Value.Contains("Task") ? "Task" : "void";
            var parameters = match.Groups[3].Value.Split(',')
                .Select(p => p.Trim().Split(' '))
                .Select(parts => new ParameterInfo
                {
                    Type = parts[0],
                    Name = parts[^1]
                })
                .ToList();

            _setterMethods.Add(new MethodInfo
            {
                Name = methodName,
                ReturnType = returnType,
                Parameters = parameters
            });
        }

        // Create metodunu bul
        var createRegex = new Regex(@"public\s+[^void].*?\s+Create\s*\((.*?)\)");
        var createMatch = createRegex.Match(content);
        if (createMatch.Success)
        {
            var parameters = createMatch.Groups[1].Value.Split(',')
                .Select(p => p.Trim().Split(' '))
                .Select(parts => new ParameterInfo
                {
                    Type = parts[0],
                    Name = parts[^1]
                })
                .ToList();

            _createMethod = new MethodInfo
            {
                Name = "Create",
                Parameters = parameters
            };
        }
    }

    private string CreateTestDirectoryStructure()
    {
        var solutionDir = _modelPath.Substring(0, _modelPath.IndexOf(@"\src\"));
        var testPath = Path.Combine(solutionDir, "test", "Unit", "Bapsis.Domain.Unit.Test", "AggregateRoots", _pluralName, "Implementations");
        Directory.CreateDirectory(testPath);
        return testPath;
    }

    private void GenerateTestClass(string testPath)
    {
        var template = $@"using System.Linq.Expressions;
using Bapsis.Api.Domain.AggregateRoots.{_pluralName};
using Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Contacts;
using Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Implementations;
using Bapsis.Api.Domain.Models;
using Bapsis.Api.Domain.Repositories;
using Bapsis.Api.Domain.Specifications.Contacts;
using Moq;
using NUnit.Framework;

namespace Bapsis.Domain.Unit.Test.AggregateRoots.{_pluralName}.Implementations;

[TestFixture]
public class {_modelName}DomainServiceTest
{{
    private I{_modelName}DomainService _{_modelName.ToCamelCase()}DomainService;
    private Mock<I{_modelName}Specification> _mock{_modelName}Specification;
    private Mock<IGenericQueryRepository> _mockGenericQueryRepo;
    private Mock<IGenericSpecification> _mockGenericSpecification;

    [SetUp]
    public void SetUp()
    {{
        _{_modelName.ToCamelCase()}DomainService = new {_modelName}DomainService();
        _mock{_modelName}Specification = new Mock<I{_modelName}Specification>();
        _mockGenericQueryRepo = new Mock<IGenericQueryRepository>();
        _mockGenericSpecification = new Mock<IGenericSpecification>();
    }}

    {GenerateTestMethods()}
}}";

        File.WriteAllText(
            Path.Combine(testPath, $"{_modelName}DomainServiceTest.cs"),
            template);
    }

    private string GenerateTestMethods()
    {
        var sb = new StringBuilder();

        // Setter testleri
        foreach (var method in _setterMethods)
        {
            // Translation metodlarını ayrı ele alacağız
            if (!method.Name.Contains("Translation") || _hasMultiLanguageSupport)
            {
                var isAsync = method.ReturnType == "Task";
                var testMethodName = method.Name.Replace("Set", "");
                var parameterName = testMethodName.ToCamelCase(); // Property adını camelCase'e çevir
                var parameterValue = GetDefaultValueForType(method.Parameters[1].Type);
                var camelCaseModelName = _modelName.ToCamelCase();

                sb.AppendLine($@"
        [Test]
        public{(isAsync ? " async Task" : " void")} {method.Name}_WhenCalled_SetThe{testMethodName}()
        {{
            var {camelCaseModelName} = {_modelName}.CreateTest();
            var {parameterName} = {parameterValue};
            {(isAsync ? "await" : "")} _{camelCaseModelName}DomainService.{method.Name}({camelCaseModelName}, {parameterName});
            Assert.That({camelCaseModelName}.{testMethodName}, Is.EqualTo({parameterName}));
        }}");
            }
        }

        // Translation testi - artık sadece bir kere oluşturulacak
        if (_hasMultiLanguageSupport)
        {
            var nameMethod = _setterMethods.FirstOrDefault(m => m.Name == "SetNameTranslations");
            if (nameMethod != null)
            {
                sb.Append(GenerateTranslationTest(nameMethod, "NameTranslations"));
            }
        }

        // Create test
        if (_createMethod != null)
        {
            // Her parametre tanımını ayrı satıra al ve noktalı virgülleri düzgün yerleştir
            var parameters = _createMethod.Parameters
                .Select(p => $"var {p.Name} = {GetDefaultValueForType(p.Type)};")
                .ToList();

            sb.AppendLine($@"
        [Test]
        public void Create_WhenCalled_ReturnsNewEntity()
        {{
            {string.Join("\n        ", parameters)}

            var result = _{_modelName.ToCamelCase()}DomainService.Create({string.Join(", ", _createMethod.Parameters.Select(p => p.Name))});

            Assert.Multiple(() =>
            {{
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
                {string.Join("\n            ", _createMethod.Parameters.Select(p => 
                    $"Assert.That(result.{char.ToUpperInvariant(p.Name[0]) + p.Name.Substring(1)}, Is.EqualTo({p.Name}));"))}
            }});
        }}");
        }

        return sb.ToString();
    }

    private string GenerateTranslationTest(MethodInfo method, string testMethodName)
    {
        var isAsync = method.ReturnType == "Task";
        var propertyName = testMethodName.Replace("Translations", "");
        var methodName = isAsync ? $"{method.Name}Async" : method.Name;

        return $@"
        [Test]
        public{(isAsync ? " async Task" : " void")} {methodName}_WhenCalled_SetThe{testMethodName}()
        {{
            var {_modelName.ToCamelCase()} = {_modelName}.CreateTest();
            var {propertyName.ToLower()}s = new List<TranslationModel>
            {{
                new TranslationModel
                {{
                    Value = ""Test-1"",
                    Language = ""tr"",
                }}
            }};{(isAsync ? GenerateAsyncMockSetup() : GenerateSyncMockSetup())}

            {(isAsync ? "await" : "")} _{_modelName.ToCamelCase()}DomainService.{methodName}({_modelName.ToCamelCase()}, {propertyName.ToLower()}s);

            Assert.That({_modelName.ToCamelCase()}.Translations.Select(t => $""{{t.Language}}-{{t.{propertyName}}}""),
                Is.EquivalentTo({propertyName.ToLower()}s.Select(name => $""{{name.Language}}-{{name.Value}}"")));
        }}";
    }

    private string GenerateAsyncMockSetup()
    {
        return $@"

                _mock{_modelName}Specification
                    .Setup(x => x.ExistsWithSameNameExcludingId(It.IsAny<{_idType}>(), It.IsAny<ICollection<TranslationModel>>()))
                    .Returns(It.IsAny<Expression<Func<{_modelName}, bool>>>());
                _mockGenericQueryRepo
                    .Setup(x => x.GetFromCacheAsync(It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Expression<Func<{_modelName}, bool>>>()))
                    .ReturnsAsync(new List<{_modelName}>());
                _mockGenericSpecification
                    .Setup(x => x.IsUndeleted<{_modelName}>())
                    .Returns(x => x.IsDeleted);
                _{_modelName.ToCamelCase()}DomainService = new {_modelName}DomainService
                {{
                    {_modelName}Specifications = new {_modelName}Specification {{ GenericSpecification = _mockGenericSpecification.Object }},
                    GenericQueryRepository = _mockGenericQueryRepo.Object
                }}";
    }

    private string GenerateSyncMockSetup()
    {
        return "";
    }  

    private string GetDefaultValueForType(string type)
    {
        return type switch
        {
            "bool" => "true",
            "int" => "1",
            "int?" => "1",
            "Guid" => "Guid.NewGuid()",
            "Guid?" => "Guid.NewGuid()",
            "string" => "\"Test\"",
            "Parameters" => "new Parameters()",
            "ICollection<TranslationModel>" => @"new List<TranslationModel>
            {
                new()
                {
                    Value = ""Test"",
                    Language = ""tr"",
                }
            }",
            _ => "null"
        };
    }
}