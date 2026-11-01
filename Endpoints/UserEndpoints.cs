using System.Net.Mail;
using UserManagementAPI.Contracts;
using UserManagementAPI.Models;
using UserManagementAPI.Services;

namespace UserManagementAPI.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users")
            .WithTags("Users");

        group.MapGet("", GetAllUsers)
            .WithName("GetUsers")
            .Produces<IReadOnlyList<User>>();

        group.MapGet("/{id:int}", GetUserById)
            .WithName("GetUserById")
            .Produces<User>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("", CreateUser)
            .WithName("CreateUser")
            .Accepts<UserRequest>("application/json")
            .Produces<User>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:int}", UpdateUser)
            .WithName("UpdateUser")
            .Accepts<UserRequest>("application/json")
            .Produces<User>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:int}", DeleteUser)
            .WithName("DeleteUser")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static IResult GetAllUsers(UserStore store) =>
        Results.Ok(store.GetAll());

    private static IResult GetUserById(int id, UserStore store)
    {
        var user = store.GetById(id);

        return user is null
            ? UserNotFound(id)
            : Results.Ok(user);
    }

    private static IResult CreateUser(UserRequest? request, UserStore store)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var user = store.Create(request!.Name!.Trim(), request.Email!.Trim());

        return user is null
            ? EmailConflict(request.Email)
            : Results.Created($"/api/users/{user.Id}", user);
    }

    private static IResult UpdateUser(int id, UserRequest? request, UserStore store)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var result = store.Update(id, request!.Name!.Trim(), request.Email!.Trim());

        return result.Status switch
        {
            UserUpdateStatus.Success => Results.Ok(result.User),
            UserUpdateStatus.NotFound => UserNotFound(id),
            UserUpdateStatus.EmailConflict => EmailConflict(request.Email),
            _ => throw new InvalidOperationException("Unknown user update result.")
        };
    }

    private static IResult DeleteUser(int id, UserStore store) =>
        store.Delete(id)
            ? Results.NoContent()
            : UserNotFound(id);

    private static Dictionary<string, string[]> Validate(UserRequest? request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null)
        {
            errors["request"] = ["A JSON request body is required."];
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["Name is required."];
        }
        else if (request.Name.Trim().Length > 100)
        {
            errors["name"] = ["Name must not exceed 100 characters."];
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["email"] = ["Email is required."];
        }
        else
        {
            var email = request.Email.Trim();
            var isValidEmail = email.Length <= 254 &&
                MailAddress.TryCreate(email, out var parsedEmail) &&
                string.Equals(parsedEmail.Address, email, StringComparison.OrdinalIgnoreCase);

            if (!isValidEmail)
            {
                errors["email"] = ["Email must be a valid email address."];
            }
        }

        return errors;
    }

    private static IResult UserNotFound(int id) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "User not found",
            detail: $"No user with ID {id} exists.");

    private static IResult EmailConflict(string email) =>
        Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Email already in use",
            detail: $"A user with the email address '{email.Trim()}' already exists.");
}
