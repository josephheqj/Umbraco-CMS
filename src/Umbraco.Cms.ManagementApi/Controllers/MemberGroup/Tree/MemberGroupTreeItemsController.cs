﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.ManagementApi.ViewModels.Tree;

namespace Umbraco.Cms.ManagementApi.Controllers.MemberGroup.Tree;

public class MemberGroupTreeItemsController : MemberGroupTreeControllerBase
{
    public MemberGroupTreeItemsController(IEntityService entityService)
        : base(entityService)
    {
    }

    [HttpGet("items")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PagedResult<TreeItemViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TreeItemViewModel>>> Items([FromQuery(Name = "key")] Guid[] keys)
        => await GetItems(keys);
}
