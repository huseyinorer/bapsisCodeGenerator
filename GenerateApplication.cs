using System.Text.RegularExpressions;

public class GenerateApplication
{
    private readonly string _modelPath;
    private readonly string _modelName;
    private readonly string _pluralName;
    private readonly string _idType; // aggregate root'taki id tipi

    private const string APPLICATION_FOLDER = "Bapsis.Api.Application";
    private enum ModuleType
    {
        Commons,
        Admin,
        Commission,
        Coordinator,
        Management,
        OpenApi,
        ProjectOffice,
        Researcher,
        SpendingOffice,
        SystemManagement
    }

    public GenerateApplication(string modelPath, string modelName, string pluralName)
    {
        _modelPath = modelPath;
        _modelName = modelName;
        _pluralName = pluralName;
        _idType = DetectIdType();
    }

    private string DetectIdType()
    {
        try
        {
            var modelContent = File.ReadAllText(_modelPath);
            
            // Regex ile Id property'sini ve tipini bul
            var regex = new Regex(@"public\s+([a-zA-Z0-9_<>]+)\s+Id\s*{\s*get\s*;\s*set\s*;\s*}");
            var match = regex.Match(modelContent);
            
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // Eğer property bulunamazsa base class'ta olabilir
            regex = new Regex(@":\s*Entity<([a-zA-Z0-9_<>]+)>");
            match = regex.Match(modelContent);
            
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // Tip bulunamazsa varsayılan olarak int döndür
            return "int";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ID tipi belirlenirken hata oluştu: {ex.Message}");
            Console.WriteLine("Varsayılan olarak 'int' kullanılacak.");
            return "int";
        }
    }

    public void Generate()
    {
        try
        {
            Console.WriteLine("Lütfen modül tipini seçin:");
            Console.WriteLine("1- Commons");
            Console.WriteLine("2- Modules");
            
            var choice = Console.ReadLine();
            string basePath;
            
            if (choice == "1")
            {
                basePath = GetApplicationPath("Commons");
            }
            else if (choice == "2")
            {
                Console.WriteLine("\nLütfen modül seçin:");
                Console.WriteLine("1- Admin");
                Console.WriteLine("2- Commission");
                Console.WriteLine("3- Coordinator");
                Console.WriteLine("4- Management");
                Console.WriteLine("5- OpenApi");
                Console.WriteLine("6- ProjectOffice");
                Console.WriteLine("7- Researcher");
                Console.WriteLine("8- SpendingOffice");
                Console.WriteLine("9- SystemManagement");

                var moduleChoice = Console.ReadLine();
                var selectedModule = (ModuleType)Enum.Parse(typeof(ModuleType), 
                    ((int.Parse(moduleChoice))).ToString());
                
                basePath = GetApplicationPath($"Modules/{selectedModule}");
            }
            else
            {
                throw new Exception("Geçersiz seçim!");
            }

            // Ana klasörü oluştur
            var mainFolder = Path.Combine(basePath, _pluralName);
            CreateDirectoryStructure(mainFolder);
            GenerateFiles(mainFolder, choice == "2");

            Console.WriteLine($"Application katmanı başarıyla oluşturuldu.");
            Console.WriteLine($"Konum: {mainFolder}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Application katmanı oluşturulurken hata: {ex.Message}");
        }
    }

    private string GetApplicationPath(string subPath)
    {
        var domainIndex = _modelPath.IndexOf("Bapsis.Api.Domain");
        if (domainIndex == -1)
            throw new Exception("Model dosyası doğru konumda değil.");

        var basePath = _modelPath.Substring(0, domainIndex);
        return Path.Combine(basePath, APPLICATION_FOLDER, "Internal", subPath);
    }

    private void CreateDirectoryStructure(string basePath)
    {
        // Commands structure
        var commandsPath = Path.Combine(basePath, "Commands");
        CreateDirectories(commandsPath, new[]
        {
            "Dtos/Create",
            "Dtos/Edit",
            "Dtos/Delete",
            "Handlers/Create",
            "Handlers/Edit",
            "Handlers/Delete"
        });

        // Queries structure
        var queriesPath = Path.Combine(basePath, "Queries");
        CreateDirectories(queriesPath, new[]
        {
            "Dtos",
            "Handlers"
        });

        // Profiles
        Directory.CreateDirectory(Path.Combine(basePath, "Profiles"));
    }

    private void CreateDirectories(string basePath, string[] subPaths)
    {
        foreach (var subPath in subPaths)
        {
            Directory.CreateDirectory(Path.Combine(basePath, subPath));
        }
    }

    private void GenerateFiles(string basePath, bool isModule)
    {
        var modulePrefix = isModule ? "Modules." : "Commons.";
        var namespacePath = $"Bapsis.Api.Application.Internal.{modulePrefix}{_pluralName}";

        // Generate DTOs
        GenerateDto(basePath, "Create", namespacePath);
        GenerateDto(basePath, "Edit", namespacePath);
        GenerateDto(basePath, "Delete", namespacePath);

        // Generate Command Handlers
        GenerateCreateCommand(basePath, namespacePath);
        GenerateEditCommand(basePath, namespacePath);
        GenerateDeleteCommand(basePath, namespacePath);

        // Generate Query Files
        GenerateQueryDto(basePath, namespacePath);
        GenerateQueryHandler(basePath, namespacePath);

        // Generate Mapper Profile
        GenerateMapperProfile(basePath, namespacePath);
    }

    private void GenerateDto(string basePath, string operation, string namespacePath)
    {
        var template = $@"namespace {namespacePath}.Commands.Dtos.{operation};
public class {_modelName}{operation}dDto
{{
    // TODO write props
}}";

        File.WriteAllText(
            Path.Combine(basePath, "Commands", "Dtos", operation, $"{_modelName}{operation}dDto.cs"),
            template);
    }

    private void GenerateCreateCommand(string basePath, string namespacePath)
    {
        var template = $@"using {namespacePath}.Commands.Dtos.Create;
using Bapsis.Api.Domain.AggregateRoots.{_pluralName};
using Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Contacts;
using Bapsis.Api.Domain.Constants;
using Bapsis.Api.Domain.Models;
using MediatR;

namespace {namespacePath}.Commands.Handlers.Create;

public class {_modelName}CreateCommand : IRequest<{_modelName}CreatedDto>
{{
    // TODO write props
}}

public class {_modelName}CreateCommandHandler : BaseInternalService,
    IRequestHandler<{_modelName}CreateCommand, {_modelName}CreatedDto>
{{
    #region injections
    public I{_modelName}CommandRepository {_modelName}CommandRepository {{ get; set; }}
    public I{_modelName}DomainService {_modelName}DomainService {{ get; set; }}
    #endregion

    public async Task<{_modelName}CreatedDto> Handle({_modelName}CreateCommand request, CancellationToken cancellationToken)
    {{
        // TODO write setters
        var entity = {_modelName}DomainService.Create(
            // TODO write props
         );

        {_modelName}CommandRepository.Insert(entity);
        Db.Commit();
        await AbisCache.RemoveAsync(CacheConstants.{_pluralName.ToUpper()}, cancellationToken);
        var response = Mapper.Map<{_modelName}CreatedDto>(entity);
        return response;
    }}
}}";

        File.WriteAllText(
            Path.Combine(basePath, "Commands", "Handlers", "Create", $"{_modelName}CreateCommand.cs"),
            template);
    }

    // Diğer Generate metodları benzer şekilde implement edilecek...

    private void GenerateMapperProfile(string basePath, string namespacePath)
    {
        var template = $@"using AutoMapper;
using {namespacePath}.Commands.Dtos.Create;
using {namespacePath}.Commands.Dtos.Delete;
using {namespacePath}.Commands.Dtos.Edit;
using {namespacePath}.Queries.Dtos;
using Bapsis.Api.Application.Shared.Extensions;
using Bapsis.Api.Domain.AggregateRoots.{_pluralName};
using Bapsis.Api.Domain.Extensions;

namespace {namespacePath}.Profiles;

public class MapperProfiles : Profile
{{
    public MapperProfiles()
    {{
        #region command
        // TODO write commands maps
        #endregion

        #region queries
        // TODO write queries maps
        #endregion
    }}
}}";

        File.WriteAllText(
            Path.Combine(basePath, "Profiles", "MapperProfiles.cs"),
            template);
    }

    private void GenerateEditCommand(string basePath, string namespacePath)
    {
        var template = $@"using {namespacePath}.Commands.Dtos.Edit;
using {namespacePath}.Commands.Dtos.Delete;
using Bapsis.Api.Domain.AggregateRoots.{_pluralName};
using Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Contacts;
using Bapsis.Api.Domain.Constants;
using Bapsis.Api.Domain.Models;
using MediatR;

namespace {namespacePath}.Commands.Handlers.Edit;

public class {_modelName}EditCommand : IRequest<{_modelName}EditedDto>
{{
    // TODO write props
}}

public class {_modelName}EditCommandHandler : BaseInternalService,
    IRequestHandler<{_modelName}EditCommand, {_modelName}EditedDto>
{{
    #region injections
    public I{_modelName}CommandRepository {_modelName}CommandRepository {{ get; set; }}
    public I{_modelName}DomainService {_modelName}DomainService {{ get; set; }}
    #endregion

    public async Task<{_modelName}EditedDto> Handle({_modelName}EditCommand request, CancellationToken cancellationToken)
    {{
        // TODO write setters
        
        {_modelName}CommandRepository.Update(entity);
        Db.Commit();
        await AbisCache.RemoveAsync(CacheConstants.{_pluralName.ToUpper()}, cancellationToken);
        var response = Mapper.Map<{_modelName}EditedDto>(entity);
        return response;
    }}
}}";

        File.WriteAllText(
            Path.Combine(basePath, "Commands", "Handlers", "Edit", $"{_modelName}EditCommand.cs"),
            template);
    }

    private void GenerateDeleteCommand(string basePath, string namespacePath)
    {
        var template = $@"using {namespacePath}.Commands.Dtos.Delete;
using Bapsis.Api.Domain.AggregateRoots.{_pluralName};
using Bapsis.Api.Domain.AggregateRoots.{_pluralName}.Contacts;
using Bapsis.Api.Domain.Constants;
using MediatR;

namespace {namespacePath}.Commands.Handlers.Delete;

public class {_modelName}DeleteCommand : IRequest<{_modelName}DeletedDto>
{{
    public {_idType} Id {{ get; set; }}
}}

public class {_modelName}DeleteCommandHandler : BaseInternalService,
    IRequestHandler<{_modelName}DeleteCommand, {_modelName}DeletedDto>
{{
    #region injections
    public I{_modelName}CommandRepository {_modelName}CommandRepository {{ get; set; }}
    public I{_modelName}DomainService {_modelName}DomainService {{ get; set; }}
    #endregion

    public async Task<{_modelName}DeletedDto> Handle({_modelName}DeleteCommand request, CancellationToken cancellationToken)
    {{
        var entity = await QueryService
            .GetFromCacheByIdAsync<{_modelName}, {_idType}>(CacheConstants.{_pluralName.ToUpper()}, 
                CacheIncludeConstants.{_pluralName.ToUpper()}, request.Id);

        {_modelName}DomainService.CheckNull(entity);
        {_modelName}DomainService.SetIsDeleted(entity, true);
        {_modelName}CommandRepository.Update(entity);
        Db.Commit();
        await AbisCache.RemoveAsync(CacheConstants.{_pluralName.ToUpper()}, cancellationToken);
        var response = Mapper.Map<{_modelName}DeletedDto>(entity);
        return response;
    }}
}}";

        File.WriteAllText(
            Path.Combine(basePath, "Commands", "Handlers", "Delete", $"{_modelName}DeleteCommand.cs"),
            template);
    }

    private void GenerateQueryDto(string basePath, string namespacePath)
    {
        var template = $@"using Bapsis.Api.Domain.Models;

namespace {namespacePath}.Queries.Dtos;

public class {_modelName}ByIdQueryDto
{{
    // TODO write props
}}";

        File.WriteAllText(
            Path.Combine(basePath, "Queries", "Dtos", $"{_modelName}ByIdQueryDto.cs"),
            template);
    }

    private void GenerateQueryHandler(string basePath, string namespacePath)
    {
        var template = $@"using {namespacePath}.Queries.Dtos;
using Bapsis.Api.Domain.AggregateRoots.{_pluralName};
using Bapsis.Api.Domain.Constants;
using MediatR;

namespace {namespacePath}.Queries.Handlers;

public class {_modelName}ByIdQuery : IRequest<{_modelName}ByIdQueryDto>
{{
    public {_idType} Id {{ get; set; }}
}}

public class {_modelName}ByIdQueryHandler : BaseInternalService,
    IRequestHandler<{_modelName}ByIdQuery, {_modelName}ByIdQueryDto>
{{
    public async Task<{_modelName}ByIdQueryDto> Handle({_modelName}ByIdQuery request, CancellationToken cancellationToken)
    {{
        var entity = await QueryService
            .GetFromCacheByIdAsync<{_modelName}, {_idType}>(CacheConstants.{_pluralName.ToUpper()},
                CacheIncludeConstants.{_pluralName.ToUpper()}, request.Id);
        
        var response = Mapper.Map<{_modelName}ByIdQueryDto>(entity);
        return response;
    }}
}}";

        File.WriteAllText(
            Path.Combine(basePath, "Queries", "Handlers", $"{_modelName}ByIdQuery.cs"),
            template);
    }
}