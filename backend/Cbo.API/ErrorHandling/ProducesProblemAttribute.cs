using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace Cbo.API.ErrorHandling;

/// <summary>
/// Declares a problem details response in the OpenAPI document.
/// </summary>
public sealed class ProducesProblemAttribute(int statusCode)
    : ProducesResponseTypeAttribute(typeof(ProblemDetails), statusCode, MediaTypeNames.Application.ProblemJson);
