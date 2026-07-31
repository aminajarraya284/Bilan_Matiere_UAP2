using System.Diagnostics;
using BilanMatiereUAP2.Application.Common;
using BilanMatiereUAP2.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BilanMatiereUAP2.Api.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var (status, title) = exception switch
            {
                ValidationException => (StatusCodes.Status400BadRequest, "Erreur de validation"),
                DomainValidationException => (StatusCodes.Status400BadRequest, "Règle métier invalide"),
                NotFoundException => (StatusCodes.Status404NotFound, "Ressource introuvable"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflit métier"),
                ImportFileException => (StatusCodes.Status400BadRequest, "Fichier d'import invalide"),
                _ => (StatusCodes.Status500InternalServerError, "Une erreur interne est survenue.")
            };

            if (status >= 500)
            {
                logger.LogError(exception, "Une exception non gérée est survenue.");
            }
            else
            {
                logger.LogWarning(exception, "La requête a été rejetée avec le statut {StatusCode}.", status);
            }

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            ProblemDetails problem = exception is ValidationException validationException
                ? new ValidationProblemDetails(validationException.Errors
                    .GroupBy(error => ToCamelCase(error.PropertyName))
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.ErrorMessage).Distinct().ToArray()))
                : new ProblemDetails();

            problem.Status = status;
            problem.Title = title;
            problem.Instance = context.Request.Path;
            problem.Detail = status < 500 || environment.IsDevelopment() ? exception.Message : null;
            problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

            if (problem is ValidationProblemDetails validationProblem)
            {
                await context.Response.WriteAsJsonAsync(validationProblem);
            }
            else
            {
                await context.Response.WriteAsJsonAsync(problem);
            }
        }
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || char.IsLower(value[0]))
        {
            return value;
        }

        return string.Create(value.Length, value, static (span, source) =>
        {
            span[0] = char.ToLowerInvariant(source[0]);
            source.AsSpan(1).CopyTo(span[1..]);
        });
    }
}
