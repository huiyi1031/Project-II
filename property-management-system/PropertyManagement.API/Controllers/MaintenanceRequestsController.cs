using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyManagement.API.Models.DTOs.MaintenanceRequests;
using PropertyManagement.API.Services;

namespace PropertyManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MaintenanceRequestsController : ControllerBase
    {
        private const long MaxImageBytes = 10 * 1024 * 1024;
        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png"
        };

        private readonly IMaintenanceRequestService _service;
        private readonly IWebHostEnvironment _environment;

        public MaintenanceRequestsController(IMaintenanceRequestService service, IWebHostEnvironment environment)
        {
            _service = service;
            _environment = environment;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<MaintenanceRequestListItemResponse>>> GetRequests([FromQuery] MaintenanceRequestFilterRequest filter, CancellationToken cancellationToken)
        {
            return Ok(await _service.GetPagedAsync(filter, cancellationToken));
        }

        [HttpGet("my")]
        public async Task<ActionResult<PagedResponse<MaintenanceRequestListItemResponse>>> GetMyRequests([FromQuery] MaintenanceRequestFilterRequest filter, CancellationToken cancellationToken)
        {
            return Ok(await _service.GetPagedAsync(filter, cancellationToken));
        }

        [HttpGet("requesters")]
        public async Task<ActionResult<IReadOnlyList<MaintenanceRequesterResponse>>> GetRequesters(CancellationToken cancellationToken)
        {
            return Ok(await _service.GetRequestersAsync(cancellationToken));
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<MaintenanceRequestDetailResponse>> GetRequest(long id, CancellationToken cancellationToken)
        {
            var response = await _service.GetByIdAsync(id, cancellationToken);
            return response is null ? NotFound(new { message = "Maintenance request was not found." }) : Ok(response);
        }

        [HttpGet("{id:long}/history")]
        public async Task<ActionResult<IReadOnlyList<MaintenanceRequestHistoryResponse>>> GetHistory(long id, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _service.GetHistoryAsync(id, cancellationToken));
            }
            catch (MaintenanceRequestBusinessException exception)
            {
                return NotFound(new { message = exception.Message });
            }
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<ActionResult<MaintenanceRequestDetailResponse>> Create([FromBody] CreateMaintenanceRequestRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _service.CreateAsync(request, GetPerformedBy(), cancellationToken);
                return Created($"/api/MaintenanceRequests/{response.RequestID}", response);
            }
            catch (MaintenanceRequestValidationException exception)
            {
                return BadRequest(new ValidationProblemDetails(ToValidationDictionary(exception.Errors)));
            }
            catch (MaintenanceRequestBusinessException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<MaintenanceRequestDetailResponse>> CreateFromForm([FromForm] CreateMaintenanceRequestFormRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var imagePath = await SaveMaintenanceImageAsync(request.Image, cancellationToken);
                var response = await _service.CreateAsync(request.ToCreateRequest(imagePath), GetPerformedBy(), cancellationToken);
                return Created($"/api/MaintenanceRequests/{response.RequestID}", response);
            }
            catch (MaintenanceRequestValidationException exception)
            {
                return BadRequest(new ValidationProblemDetails(ToValidationDictionary(exception.Errors)));
            }
            catch (MaintenanceRequestBusinessException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }

        [HttpPut("{id:long}")]
        public async Task<ActionResult<MaintenanceRequestDetailResponse>> Update(long id, [FromBody] UpdateMaintenanceRequestRequest request, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _service.UpdateAsync(id, request, GetPerformedBy(), cancellationToken));
            }
            catch (MaintenanceRequestValidationException exception)
            {
                return BadRequest(new ValidationProblemDetails(ToValidationDictionary(exception.Errors)));
            }
            catch (MaintenanceRequestBusinessException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }

        [HttpPost("{id:long}/approve")]
        public async Task<IActionResult> Approve(long id, CancellationToken cancellationToken)
        {
            try
            {
                await _service.ApproveAsync(id, GetPerformedBy(), cancellationToken);
                return NoContent();
            }
            catch (MaintenanceRequestBusinessException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }

        [HttpPost("{id:long}/reject")]
        public async Task<IActionResult> Reject(long id, [FromBody] RejectMaintenanceRequestRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await _service.RejectAsync(id, request, GetPerformedBy(), cancellationToken);
                return NoContent();
            }
            catch (MaintenanceRequestValidationException exception)
            {
                return BadRequest(new ValidationProblemDetails(ToValidationDictionary(exception.Errors)));
            }
            catch (MaintenanceRequestBusinessException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }

        [HttpPost("{id:long}/cancel")]
        public async Task<IActionResult> Cancel(long id, [FromBody] CancelMaintenanceRequestRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await _service.CancelAsync(id, request, GetPerformedBy(), cancellationToken);
                return NoContent();
            }
            catch (MaintenanceRequestValidationException exception)
            {
                return BadRequest(new ValidationProblemDetails(ToValidationDictionary(exception.Errors)));
            }
            catch (MaintenanceRequestBusinessException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }

        private async Task<string?> SaveMaintenanceImageAsync(IFormFile? image, CancellationToken cancellationToken)
        {
            if (image is null || image.Length == 0) return null;

            var extension = Path.GetExtension(image.FileName);
            if (!AllowedImageExtensions.Contains(extension))
            {
                throw new MaintenanceRequestValidationException(new Dictionary<string, string[]>
                {
                    ["image"] = new[] { "Attachment must be a JPG or PNG image." }
                });
            }

            if (image.Length > MaxImageBytes)
            {
                throw new MaintenanceRequestValidationException(new Dictionary<string, string[]>
                {
                    ["image"] = new[] { "Attachment must not exceed 10MB." }
                });
            }

            if (!string.IsNullOrWhiteSpace(image.ContentType) && !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new MaintenanceRequestValidationException(new Dictionary<string, string[]>
                {
                    ["image"] = new[] { "Attachment must be an image file." }
                });
            }

            var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadFolder = Path.Combine(webRoot, "uploads", "maintenance");
            Directory.CreateDirectory(uploadFolder);

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var filePath = Path.Combine(uploadFolder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream, cancellationToken);
            }

            return $"{Request.Scheme}://{Request.Host}{Request.PathBase}/uploads/maintenance/{fileName}";
        }

        private string GetPerformedBy()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? "System";
        }

        private static Dictionary<string, string[]> ToValidationDictionary(IReadOnlyDictionary<string, string[]> errors)
        {
            return errors.ToDictionary(error => error.Key, error => error.Value);
        }
    }
}
