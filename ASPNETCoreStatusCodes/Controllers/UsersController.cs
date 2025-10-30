using ASPNETCoreStatusCodes.Models;
using ASPNETCoreStatusCodes.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ASPNETCoreStatusCodes.Controllers;
// ReSharper disable All

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/users - Retrieve all active users
    /// Returns: 200 OK with user list, 500 on server error, and 429 Too Many Requests if rate limit exceeded.
    /// </summary>
    [HttpGet]
    [EnableRateLimiting("fixed-rate-limit")] // Apply rate limiting to this endpoint - 429 Too Many Requests if limit exceeded
    public async Task<ActionResult<ApiResponse<IEnumerable<User>>>> GetAllUsers()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();

            // 200 OK - Request succeeded, returning data
            return Ok(new ApiResponse<IEnumerable<User>>
            {
                Success = true,
                Message = "Users retrieved successfully",
                Data = users
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");

            // 500 Internal Server Error - Unexpected server error
            return StatusCode(500, new ApiResponse<IEnumerable<User>>
            {
                Success = false,
                Message = "An internal server error occurred",
                Errors = new List<string> { "Unable to retrieve users at this time" }
            });
        }
    }

    /// <summary>
    /// GET /api/users/{id} - Retrieve a specific user by ID
    /// Returns: 200 OK with user, 404 if not found, 400 for invalid ID, 500 on server error
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<User>>> GetUserById(int id)
    {
        try
        {
            // 400 Bad Request - Invalid request data/format
            if (id <= 0)
            {
                return BadRequest(new ApiResponse<User>
                {
                    Success = false,
                    Message = "Invalid user ID",
                    Errors = new List<string> { "User ID must be a positive integer" }
                });
            }

            var user = await _userService.GetUserByIdAsync(id);

            if (user == null)
            {
                // 404 Not Found - Resource doesn't exist
                return NotFound(new ApiResponse<User>
                {
                    Success = false,
                    Message = "User not found",
                    Errors = new List<string> { $"User with ID {id} does not exist" }
                });
            }

            // 200 OK - Request succeeded, returning data
            return Ok(new ApiResponse<User>
            {
                Success = true,
                Message = "User retrieved successfully",
                Data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID {UserId}", id);

            // 500 Internal Server Error - Unexpected server error
            return StatusCode(500, new ApiResponse<User>
            {
                Success = false,
                Message = "An internal server error occurred",
                Errors = new List<string> { "Unable to retrieve user at this time" }
            });
        }
    }

    /// <summary>
    /// POST /api/users - Create a new user
    /// Returns: 201 Created with new user, 400 for validation errors, 409 for conflicts, 422 for validation, 500 on server error
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<User>>> CreateUser([FromBody] User user)
    {
        try
        {
            // 400 Bad Request - Invalid request data/format
            if (user == null)
            {
                return BadRequest(new ApiResponse<User>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = new List<string> { "User data is required" }
                });
            }

            // 422 Unprocessable Entity - Validation failed
            // You can also return 400 Bad Request here, but Unprocessable Entity is more specific for validation issues
            // ASP.NET Core's built-in model validation typically returns 400 Bad Request by default
            // https://github.com/dotnet/aspnetcore/issues/6145
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .SelectMany(x => x.Value!.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();

                // You can use BadRequest here if you prefer
                return UnprocessableEntity(new ApiResponse<User>
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = validationErrors
                });
            }

            // 409 Conflict - Resource conflict (duplicate email)
            // also possible to use for concurrency issues
            if (await _userService.EmailExistsAsync(user.Email))
            {
                return Conflict(new ApiResponse<User>
                {
                    Success = false,
                    Message = "Email already exists",
                    Errors = new List<string> { $"A user with email '{user.Email}' already exists" }
                });
            }

            var createdUser = await _userService.CreateUserAsync(user);

            // 201 Created - Resource created successfully
            return CreatedAtAction(nameof(GetUserById),
                new { id = createdUser.Id },
                new ApiResponse<User>
                {
                    Success = true,
                    Message = "User created successfully",
                    Data = createdUser
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");

            // 500 Internal Server Error - Unexpected server error
            return StatusCode(500, new ApiResponse<User>
            {
                Success = false,
                Message = "An internal server error occurred",
                Errors = new List<string> { "Unable to create user at this time" }
            });
        }
    }

    /// <summary>
    /// PUT /api/users/{id} - Update an entire user resource
    /// Returns: 200 OK with updated user, 404 if not found, 400/422 for validation, 409 for conflicts, 500 on server error
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<User>>> UpdateUser(int id, [FromBody] User user)
    {
        try
        {
            // 400 Bad Request - Invalid request data/format
            if (id <= 0)
            {
                return BadRequest(new ApiResponse<User>
                {
                    Success = false,
                    Message = "Invalid user ID",
                    Errors = new List<string> { "User ID must be a positive integer" }
                });
            }

            if (user == null)
            {
                return BadRequest(new ApiResponse<User>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = new List<string> { "User data is required" }
                });
            }

            // 422 Unprocessable Entity - Validation failed
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .SelectMany(x => x.Value!.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();

                // You can use BadRequest here if you prefer
                return UnprocessableEntity(new ApiResponse<User>
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = validationErrors
                });
            }

            // 404 Not Found - Resource doesn't exist
            if (!await _userService.UserExistsAsync(id))
            {
                return NotFound(new ApiResponse<User>
                {
                    Success = false,
                    Message = "User not found",
                    Errors = new List<string> { $"User with ID {id} does not exist" }
                });
            }

            // 409 Conflict - Resource conflict (duplicate email)
            if (await _userService.EmailExistsAsync(user.Email, id))
            {
                return Conflict(new ApiResponse<User>
                {
                    Success = false,
                    Message = "Email already exists",
                    Errors = new List<string> { $"Another user with email '{user.Email}' already exists" }
                });
            }

            var updatedUser = await _userService.UpdateUserAsync(id, user);

            // 200 OK - Request succeeded, returning data
            return Ok(new ApiResponse<User>
            {
                Success = true,
                Message = "User updated successfully",
                Data = updatedUser
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID {UserId}", id);

            // 500 Internal Server Error - Unexpected server error
            return StatusCode(500, new ApiResponse<User>
            {
                Success = false,
                Message = "An internal server error occurred",
                Errors = new List<string> { "Unable to update user at this time" }
            });
        }
    }

    /// <summary>
    /// PATCH /api/users/{id} - Partially update a user resource
    /// Returns: 200 OK with updated user, 204 No Content, 404 if not found, 400/422 for validation, 409 for conflicts, 500 on server error
    /// </summary>
    [HttpPatch("{id:int}")]
    public async Task<ActionResult<ApiResponse<User>>> PatchUser(int id, [FromBody] UserPatchDto patchDto)
    {
        try
        {
            // 400 Bad Request - Invalid request data/format
            if (id <= 0)
            {
                return BadRequest(new ApiResponse<User>
                {
                    Success = false,
                    Message = "Invalid user ID",
                    Errors = new List<string> { "User ID must be a positive integer" }
                });
            }

            // 400 Bad Request - Invalid request data/format
            if (patchDto == null)
            {
                return BadRequest(new ApiResponse<User>
                {
                    Success = false,
                    Message = "Invalid request data",
                    Errors = new List<string> { "Patch data is required" }
                });
            }

            // 422 Unprocessable Entity - Validation failed
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .SelectMany(x => x.Value!.Errors)
                    .Select(x => x.ErrorMessage)
                    .ToList();

                // You can use BadRequest here if you prefer
                return UnprocessableEntity(new ApiResponse<User>
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = validationErrors
                });
            }

            // 404 Not Found - Resource doesn't exist
            if (!await _userService.UserExistsAsync(id))
            {
                return NotFound(new ApiResponse<User>
                {
                    Success = false,
                    Message = "User not found",
                    Errors = new List<string> { $"User with ID {id} does not exist" }
                });
            }

            // 409 Conflict - Resource conflict (duplicate email)
            if (!string.IsNullOrEmpty(patchDto.Email) &&
                await _userService.EmailExistsAsync(patchDto.Email, id))
            {
                return Conflict(new ApiResponse<User>
                {
                    Success = false,
                    Message = "Email already exists",
                    Errors = new List<string> { $"Another user with email '{patchDto.Email}' already exists" }
                });
            }

            var updatedUser = await _userService.PatchUserAsync(id, patchDto);

            // Check if no meaningful updates were made
            if (updatedUser != null && IsEmptyPatch(patchDto))
            {
                // 204 No Content - Request succeeded, no content to return
                return NoContent();
            }

            // 200 OK - Request succeeded, returning data
            return Ok(new ApiResponse<User>
            {
                Success = true,
                Message = "User updated successfully",
                Data = updatedUser
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching user with ID {UserId}", id);

            // 500 Internal Server Error - Unexpected server error
            return StatusCode(500, new ApiResponse<User>
            {
                Success = false,
                Message = "An internal server error occurred",
                Errors = new List<string> { "Unable to update user at this time" }
            });
        }
    }

    /// <summary>
    /// DELETE /api/users/{id} - Delete a user (soft delete)
    /// Returns: 204 No Content on success, 404 if not found, 400 for invalid ID, 500 on server error
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(int id)
    {
        try
        {
            // 400 Bad Request - Invalid request data/format
            if (id <= 0)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid user ID",
                    Errors = new List<string> { "User ID must be a positive integer" }
                });
            }

            // 404 Not Found - Resource doesn't exist
            if (!await _userService.UserExistsAsync(id))
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found",
                    Errors = new List<string> { $"User with ID {id} does not exist" }
                });
            }

            // 403 Forbidden - Authorization check - only allow Admin role to delete users
            var user = await _userService.GetUserByIdAsync(id);
            if (user != null && !user.IsSystemAccount)
            {
                // return Forbid(); // Just returns 403 with no custom content
                
                return StatusCode(403, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Operation not permitted",
                    Errors = new List<string> { "System accounts cannot be deleted" }
                });
                
            }

            await _userService.DeleteUserAsync(id);

            // 204 No Content - Request succeeded, no content to return
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user with ID {UserId}", id);

            // 500 Internal Server Error - Unexpected server error
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An internal server error occurred",
                Errors = new List<string> { "Unable to delete user at this time" }
            });
        }
    }

    private static bool IsEmptyPatch(UserPatchDto patchDto)
    {
        return string.IsNullOrEmpty(patchDto.Email) &&
               string.IsNullOrEmpty(patchDto.FirstName) &&
               string.IsNullOrEmpty(patchDto.LastName) &&
               !patchDto.Age.HasValue &&
               !patchDto.IsActive.HasValue;
    }
}