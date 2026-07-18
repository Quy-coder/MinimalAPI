using FluentValidation;

namespace MinimalAPIs.Filters;

// Minimal API way to validate with FluentValidation: generic IEndpointFilter, resolves IValidator<T>
// via DI, runs before the handler. Applied via .AddEndpointFilter<ValidationEndpointFilter<T>>().
// Decoupled from the handler so it can be tested independently, without touching business logic (see corresponding test).
public class ValidationEndpointFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            return await next(context);
        }

        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
        var validationResult = await validator.ValidateAsync(argument);

        return validationResult.IsValid
            ? await next(context)
            : Results.ValidationProblem(validationResult.ToDictionary());
    }
}
