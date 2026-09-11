using FinCore.Api.Contracts.Categories;
using FinCore.Application.Categories;
using Microsoft.AspNetCore.Mvc;

namespace FinCore.Api.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController
    : ControllerBase
{
    private readonly CategoryService _categoryService;

    public CategoriesController(
        CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category =
                await _categoryService.CreateAsync(
                    new CreateCategoryCommand(request.Name),
                    cancellationToken);

            return Created(
                $"/api/categories/{category.Id}",
                category);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken)
    {
        var categories =
            await _categoryService.GetAllAsync(
                includeArchived,
                cancellationToken);

        return Ok(categories);
    }

    [HttpPatch("{id:guid}/name")]
    public async Task<ActionResult<CategoryDto>> Rename(
        Guid id,
        RenameCategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category =
                await _categoryService.RenameAsync(
                    id,
                    request.Name,
                    cancellationToken);

            return category is null
                ? NotFound()
                : Ok(category);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                error = exception.Message
            });
        }
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult<CategoryDto>> Archive(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var category =
                await _categoryService.ArchiveAsync(
                    id,
                    cancellationToken);

            return category is null
                ? NotFound()
                : Ok(category);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new
            {
                error = exception.Message
            });
        }
    }
}
