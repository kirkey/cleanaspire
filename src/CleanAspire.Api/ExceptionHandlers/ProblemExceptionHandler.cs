﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using EntityFramework.Exceptions.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CleanAspire.Api.ExceptionHandlers;

public class ProblemExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public ProblemExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException ex => new HttpValidationProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Validation Error",
                Detail = "One or more validation errors occurred.",
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
                Errors = ex.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    )
            },
            UniqueConstraintException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unique Constraint Violation",
                Detail = "A unique constraint violation occurred.",
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
            },
            CannotInsertNullException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Null Value Error",
                Detail = "A required field was null.",
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
            },
            MaxLengthExceededException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Max Length Exceeded",
                Detail = "A value exceeded the maximum allowed length.",
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
            },
            NumericOverflowException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Numeric Overflow",
                Detail = "A numeric value caused an overflow.",
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
            },
            ReferenceConstraintException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Reference Constraint Violation",
                Detail = "A foreign key reference constraint was violated.",
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
            },
            DbUpdateException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Database Update Error",
                Detail = "An error occurred while updating the database.",
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
            },
            KeyNotFoundException ex => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource Not Found",
                Detail = ex.Message,
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Unhandled Exception",
                Detail = "An unexpected error occurred. Please try again later.",
                Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
            }
        };

        if (problemDetails is null)
        {
            // Return true to continue processing if the exception type is not handled.
            return true;
        }
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status400BadRequest;
        // Write ProblemDetails to the response
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }

}