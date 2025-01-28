public class GenerateController
{
    private readonly string _modelPath;
    private readonly string _modelName;
    private readonly string _pluralName;
    private readonly string _idType;
    private readonly ModuleChoice _moduleChoice;
    private const string HTTP_FOLDER = "Bapsis.Api.Http";

    public GenerateController(string modelPath, string modelName, string pluralName, string idType, ModuleChoice moduleChoice)
    {
        _modelPath = modelPath;
        _modelName = modelName;
        _pluralName = pluralName;
        _idType = idType;
        _moduleChoice = moduleChoice;
    }

    public void Generate()
    {
        try
        {
            var basePath = GetHttpPath();
            var controllerPath = CreateControllerPath(basePath);
            GenerateControllerFile(controllerPath);

            Console.WriteLine($"Controller baþarýyla oluþturuldu.");
            Console.WriteLine($"Konum: {controllerPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Controller oluþturulurken hata: {ex.Message}");
        }
    }

    private string GetHttpPath()
    {
        var domainIndex = _modelPath.IndexOf("Bapsis.Api.Domain");
        if (domainIndex == -1)
            throw new Exception("Model dosyasý doðru konumda deðil.");

        var basePath = _modelPath.Substring(0, domainIndex);
        return Path.Combine(basePath, HTTP_FOLDER);
    }

    private string CreateControllerPath(string basePath)
    {
        string controllerPath;
        if (_moduleChoice.IsCommons)
        {
            controllerPath = Path.Combine(basePath, "Controllers", "Common", _pluralName);
        }
        else
        {
            controllerPath = Path.Combine(basePath, "Controllers", "Modules", _moduleChoice.SelectedModule.ToString());
        }

        Directory.CreateDirectory(controllerPath);
        return controllerPath;
    }

    private void GenerateControllerFile(string controllerPath)
    {
        var modulePrefix = _moduleChoice.IsCommons ? "Commons" : $"Modules.{_moduleChoice.SelectedModule}";
        var baseController = $"{_moduleChoice.SelectedModule}BaseController";

        var template = $@"using Abis.Core;
using Bapsis.Api.Application.Internal.{modulePrefix}.{_pluralName}.Commands.Handlers.Create;
using Bapsis.Api.Application.Internal.{modulePrefix}.{_pluralName}.Commands.Handlers.Delete;
using Bapsis.Api.Application.Internal.{modulePrefix}.{_pluralName}.Commands.Handlers.Edit;
using Bapsis.Api.Application.Internal.{modulePrefix}.{_pluralName}.Queries.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Bapsis.Api.Http.Controllers.{modulePrefix};

public class {_modelName}Controller : {baseController}
{{  
    #region queries

    [HttpGet(""{{id}}"")]
    public async Task<IActionResult> GetById([FromRoute] {_idType} id)
    {{
        var request = new {_modelName}ByIdQuery {{ Id = id }};
        var response = await Mediator.Send(request);
        
    return response == null ? NotFound() : Ok(response);
    }}

    #endregion

    #region commands

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] {_modelName}CreateCommand request)
    {{
        var response = await Mediator.Send(request);
        
    return response == null ? BadRequest() : Ok(response);
    }}

    [HttpPut(""{{id}}"")]
    public async Task<IActionResult> Edit([FromRoute] {_idType} id, [FromBody] {_modelName}EditCommand request)
    {{
        request.Id = id;
        var response = await Mediator.Send(request);
        
    return response == null ? BadRequest() : Ok(response);
    }}

    [HttpDelete(""{{id}}"")]
    public async Task<IActionResult> Delete([FromRoute] {_idType} id)
    {{
        var request = new {_modelName}DeleteCommand {{ Id = id }};
        var response = await Mediator.Send(request);
        
    return response == null ? BadRequest() : Ok(response);
    }}

    #endregion
}}";

        var filePath = Path.Combine(controllerPath, $"{_modelName}Controller.cs");
        File.WriteAllText(filePath, template);
    }
}