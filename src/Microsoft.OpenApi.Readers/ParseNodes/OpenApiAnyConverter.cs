﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. 

using System;
using System.Globalization;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Exceptions;
using Microsoft.OpenApi.Models;

namespace Microsoft.OpenApi.Readers.ParseNodes
{
    internal static class OpenApiAnyConverter
    {
        /// <summary>
        /// Converts the <see cref="OpenApiString"/>s in the given <see cref="IOpenApiAny"/>
        /// into the most specific <see cref="IOpenApiPrimitive"/> type based on the value.
        /// </summary>
        public static IOpenApiAny GetSpecificOpenApiAny(IOpenApiAny openApiAny)
        {
            if (openApiAny is OpenApiArray openApiArray)
            {
                var newArray = new OpenApiArray();
                foreach (var element in openApiArray)
                {
                    newArray.Add(GetSpecificOpenApiAny(element));
                }

                return newArray;
            }

            if (openApiAny is OpenApiObject openApiObject)
            {
                var newObject = new OpenApiObject();

                foreach (var key in openApiObject.Keys.ToList())
                {
                    newObject[key] = GetSpecificOpenApiAny(openApiObject[key]);
                }

                return newObject;
            }

            if ( !(openApiAny is OpenApiString))
            {
                return openApiAny;
            }

            var value = ((OpenApiString)openApiAny).Value;

            if (value == null || value == "null")
            {
                return new OpenApiNull();
            }

            if (value == "true")
            {
                return new OpenApiBoolean(true);
            }

            if (value == "false")
            {
                return new OpenApiBoolean(false);
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            {
                return new OpenApiInteger(intValue);
            }

            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
            {
                return new OpenApiLong(longValue);
            }

            if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue))
            {
                return new OpenApiDouble(doubleValue);
            }

            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeValue))
            {
                return new OpenApiDateTime(dateTimeValue);
            }

            // if we can't identify the type of value, return it as string.
            return new OpenApiString(value);
        }

        /// <summary>
        /// Converts the <see cref="OpenApiString"/>s in the given <see cref="IOpenApiAny"/>
        /// into the appropriate <see cref="IOpenApiPrimitive"/> type based on the given <see cref="OpenApiSchema"/>.
        /// For those strings that the schema does not specify the type for, convert them into
        /// the most specific type based on the value.
        /// </summary>
        public static IOpenApiAny GetSpecificOpenApiAny(IOpenApiAny openApiAny, OpenApiSchema schema)
        {
            if (openApiAny is OpenApiArray openApiArray)
            {
                var newArray = new OpenApiArray();
                foreach (var element in openApiArray)
                {
                    newArray.Add(GetSpecificOpenApiAny(element, schema?.Items));
                }

                return newArray;
            }

            if (openApiAny is OpenApiObject openApiObject)
            {
                var newObject = new OpenApiObject();

                foreach (var key in openApiObject.Keys.ToList())
                {
                    if ( schema != null && schema.Properties != null && schema.Properties.ContainsKey(key) )
                    {
                        newObject[key] = GetSpecificOpenApiAny(openApiObject[key], schema.Properties[key]);
                    }
                    else
                    {
                        newObject[key] = GetSpecificOpenApiAny(openApiObject[key], schema?.AdditionalProperties);
                    }
                }

                return newObject;
            }

            if (!(openApiAny is OpenApiString))
            {
                return openApiAny;
            }

            if (schema?.Type == null)
            {
                return GetSpecificOpenApiAny(openApiAny);
            }

            var type = schema.Type;
            var format = schema.Format;

            var value = ((OpenApiString)openApiAny).Value;

            if (value == null || value == "null")
            {
                return new OpenApiNull();
            }

            if (type == "integer" && format == "int32")
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                {
                    return new OpenApiInteger(intValue);
                }
                else
                {
                    throw new OpenApiException("The value is not compatible with the given type and format.");
                }
            }

            if (type == "integer" && format == "int64")
            {
                if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
                {
                    return new OpenApiLong(longValue);
                }
                else
                {
                    throw new OpenApiException("The value is not compatible with the given type and format.");
                }
            }

            if (type == "integer")
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                {
                    return new OpenApiInteger(intValue);
                }
                else
                {
                    throw new OpenApiException("The value is not compatible with the given type and format.");
                }
            }

            if (type == "number" && format == "float")
            {
                if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var floatValue))
                {
                    return new OpenApiFloat(floatValue);
                }
                else
                {
                    throw new OpenApiException("The value is not compatible with the given type and format.");
                }
            }

            if (type == "number" && format == "double" )
            {
                if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue))
                {
                    return new OpenApiDouble(doubleValue);
                }
                else
                {
                    throw new OpenApiException("The value is not compatible with the given type and format.");
                }
            }

            if (type == "number")
            {
                if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var doubleValue))
                {
                    return new OpenApiDouble(doubleValue);
                }
                else
                {
                    throw new OpenApiException("The value is not compatible with the given type and format.");
                }
            }

            if (type == "string" && format == "byte")
            {
                if ( byte.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var byteValue))
                {
                    return new OpenApiByte(byteValue);
                }
                else
                {
                    throw new OpenApiException("The value is not compatible with the given type and format.");
                }
            }

            // TODO: Parse byte array to OpenApiBinary type.

            if (type == "string" && format == "date")
            {
                if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue))
                {
                    return new OpenApiDate(dateValue.Date);
                }
                else
                {
                    throw new OpenApiException("The value is not compatible with the given type and format.");
                }
            }

            if (type == "string" && format == "date-time")
            {
                if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeValue))
                {
                    return new OpenApiDateTime(dateTimeValue);
                }
                else
                {
                    throw new OpenApiException("The value is not compatible with the given type and format.");
                }
            }

            if (type == "string" && format == "password")
            {
                return new OpenApiPassword(value);
            }

            if (type == "string")
            {
                return new OpenApiString(value);
            }

            if (type == "boolean")
            {
                if (bool.TryParse(value, out var booleanValue))
                {
                    return new OpenApiBoolean(booleanValue);
                }
                else
                {
                    throw new OpenApiException("The value is not compatible with the given type and format.");
                }
            }

            throw new OpenApiException("The specified type is not supported.");
        }
    }
}