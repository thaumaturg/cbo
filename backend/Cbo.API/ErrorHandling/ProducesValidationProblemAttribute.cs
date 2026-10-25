using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.ErrorHandling;

/// <summary>
/// Declares a validation problem details response in the OpenAPI document: the automatic model-validation 400,
/// or a failure with field errors.
/// </summary>
public sealed class ProducesValidationProblemAttribute(int statusCode = StatusCodes.Status400BadRequest)
    : ProducesResponseTypeAttribute(typeof(ValidationProblemDetails), statusCode, MediaTypeNames.Application.ProblemJson);
