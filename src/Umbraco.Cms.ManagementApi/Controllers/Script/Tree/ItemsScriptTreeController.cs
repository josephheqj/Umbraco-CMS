﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.ManagementApi.ViewModels.Pagination;
using Umbraco.Cms.ManagementApi.ViewModels.Tree;

namespace Umbraco.Cms.ManagementApi.Controllers.Script.Tree;

public class ItemsScriptTreeController : ScriptTreeControllerBase
{
    public ItemsScriptTreeController(FileSystems fileSystems)
        : base(fileSystems)
    {
    }

    [HttpGet("items")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<FileSystemTreeItemViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedViewModel<FileSystemTreeItemViewModel>>> Items([FromQuery(Name = "path")] string[] paths)
        => await GetItems(paths);
}
