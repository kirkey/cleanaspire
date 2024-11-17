﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Bogus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace CleanAspire.Api;

public static class OpenApiTransformersExtensions
{
    public static OpenApiOptions UseCookieAuthentication(this OpenApiOptions options)
    {
        var scheme = new OpenApiSecurityScheme
        {
            Name = IdentityConstants.ApplicationScheme,
            Type = SecuritySchemeType.Http,
            Description = "Use this cookie to authenticate the user.",
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = IdentityConstants.ApplicationScheme
            }
        };
        options.AddDocumentTransformer((document, context, ct) =>
        {
            document.Components ??= new();
            document.Components.SecuritySchemes.Add(IdentityConstants.ApplicationScheme, scheme);
            return Task.CompletedTask;
        });
        options.AddOperationTransformer((operation, context, ct) =>
        {
            if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
            {
                operation.Security = [new() { [scheme] = [] }];
            }
            return Task.CompletedTask;
        });
        return options;
    }
    public static OpenApiOptions UseExamples(this OpenApiOptions options)
    {
        options.AddSchemaTransformer(new ExampleChemaTransformer());
        return options;
    }



    private class ExampleChemaTransformer : IOpenApiSchemaTransformer
    {
        private static readonly Faker _faker = new();
        private static readonly Dictionary<Type, IOpenApiAny> _examples = [];
  
        public ExampleChemaTransformer()
        {
            _examples[typeof(LoginRequest)]=new OpenApiObject
            {
                ["email"] = new OpenApiString("administrator"),
                ["password"] = new OpenApiString("P@ssw0rd!")
            };
            _examples[typeof(RegisterRequest)] = new OpenApiObject
            {
                ["email"] = new OpenApiString(_faker.Internet.Email()),
                ["password"] = new OpenApiString("P@ssw0rd!")
            };

        }
        public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        {
            var type = context.JsonTypeInfo.Type;
            if (_examples.ContainsKey(type))
            {
                schema.Example = _examples[type];
            }
            return Task.CompletedTask;
        }
    }
}