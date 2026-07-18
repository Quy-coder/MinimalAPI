using FluentValidation;

namespace MinimalAPIs.Filters;

// Minimal API cách validate bằng FluentValidation: generic IEndpointFilter, resolve IValidator<T>
// qua DI, chạy trước khi vào handler. Áp dụng qua .AddEndpointFilter<ValidationEndpointFilter<T>>().
// Tách rời khỏi handler nên test được độc lập, không đụng vào logic nghiệp vụ (xem test tương ứng).
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
