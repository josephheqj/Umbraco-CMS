﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.ManagementApi.ViewModels.Pagination;
using Umbraco.Cms.ManagementApi.ViewModels.Tree;

namespace Umbraco.Cms.ManagementApi.Controllers.DataType.Tree;

public class ItemsDataTypeTreeController : DataTypeTreeControllerBase
{
    public ItemsDataTypeTreeController(IEntityService entityService, IDataTypeService dataTypeService)
        : base(entityService, dataTypeService)
    {
    }

    [HttpGet("items")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PagedViewModel<FolderTreeItemViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedViewModel<FolderTreeItemViewModel>>> Items([FromQuery(Name = "key")] Guid[] keys)
        => await GetItems(keys);
}
