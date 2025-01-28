using System.Text.RegularExpressions;

public class GenerateApplication
{
    private readonly string _modelPath;
    private readonly string _modelName;
    private readonly string _pluralName;
    private readonly string _idType;
    private readonly ModuleChoice _moduleChoice;
    private readonly bool _hasMultiLanguageSupport;
    private const string APPLICATION_FOLDER = "Bapsis.Api.Application";
  

     public GenerateApplication(string modelPath, string modelName, string pluralName, ModuleChoice moduleChoice, string idType, bool hasMultiLanguageSupport)
    {
        _modelPath = modelPath;
        _modelName = modelName;
        _pluralName = pluralName;
        _moduleChoice = moduleChoice;
        _idType = idType;
        _hasMultiLanguageSupport = hasMultiLanguageSupport;
    }

    public void Generate()
    {
        try
        {
            string basePath;
            if (_moduleChoice.IsCommons)
            {
                basePath = GetApplicationPath("Commons");
            }
            else
            {
                basePath = GetApplicationPath($"Modules/{_moduleChoice.SelectedModule}");
            }

            // Ana klasörü oluştur
            var mainFolder = Path.Combine(basePath, _pluralName);
            CreateDirectoryStructure(mainFolder);
            GenerateFiles(mainFolder, !_moduleChoice.IsCommons);

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
        var modulePrefix = isModule ? $"Modules.{_moduleChoice.SelectedModule}" : "Commons";
        var namespacePath = $"Bapsis.Api.Application.Internal.{modulePrefix}.{_pluralName}";

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
    string dtoSuffix = operation switch
    {
        "Create" => "d",
        "Edit" => "ed",
        "Delete" => "d",
        _ => "d"
    };

    var template = $@"namespace {namespacePath}.Commands.Dtos.{operation};

public class {_modelName}{operation}{dtoSuffix}Dto
{{
    // TODO write props
}}";

    File.WriteAllText(
        Path.Combine(basePath, "Commands", "Dtos", operation, $"{_modelName}{operation}{dtoSuffix}Dto.cs"),
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
        await AbisCache.RemoveAsync(CacheConstants.{_pluralName.ToUpperInvariant() }, cancellationToken);

        var response = Mapper.Map<{_modelName}CreatedDto>(entity);

        return response;
    }}
}}";

    File.WriteAllText(
        Path.Combine(basePath, "Commands", "Handlers", "Create", $"{_modelName}CreateCommand.cs"),
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
        await AbisCache.RemoveAsync(CacheConstants.{_pluralName.ToUpperInvariant() }, cancellationToken);

        var response = Mapper.Map<{_modelName}EditedDto>(entity);

        return response;
    }}
}}";

    File.WriteAllText(
        Path.Combine(basePath, "Commands", "Handlers", "Edit", $"{_modelName}EditCommand.cs"),
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
        #region queries
        // TODO write queries maps
        #endregion
        
        #region command
        // TODO write commands maps
        #endregion
    }}
}}";

        File.WriteAllText(
            Path.Combine(basePath, "Profiles", "MapperProfiles.cs"),
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
            .GetFromCacheByIdAsync<{_modelName}, {_idType}>(CacheConstants.{_pluralName.ToUpperInvariant() }, 
                CacheIncludeConstants.{_pluralName.ToUpperInvariant() }, request.Id);

        {_modelName}DomainService.CheckNull(entity);
        {_modelName}DomainService.SetIsDeleted(entity, true);

        {_modelName}CommandRepository.Update(entity);
        Db.Commit();
        await AbisCache.RemoveAsync(CacheConstants.{_pluralName.ToUpperInvariant() }, cancellationToken);

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
            .GetFromCacheByIdAsync<{_modelName}, {_idType}>(CacheConstants.{_pluralName.ToUpperInvariant() },
                CacheIncludeConstants.{_pluralName.ToUpperInvariant() }, request.Id);
        
        var response = Mapper.Map<{_modelName}ByIdQueryDto>(entity);

        return response;
    }}
}}";

        File.WriteAllText(
            Path.Combine(basePath, "Queries", "Handlers", $"{_modelName}ByIdQuery.cs"),
            template);
    }
}