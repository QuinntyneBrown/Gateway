// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Couchbase;
using Couchbase.Extensions.DependencyInjection;
using Gateway.Core.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class ConfigureServices
{
    public static void AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options => options.AddPolicy("CorsPolicy",
            builder => builder
            .WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(isOriginAllowed: _ => true)
            .AllowCredentials()));

        // Add Couchbase
        services.AddCouchbase(options =>
        {
            options.ConnectionString = configuration["Couchbase:ConnectionString"];
            options.UserName = configuration["Couchbase:Username"];
            options.Password = configuration["Couchbase:Password"];
        });

        // Add Gateway SimpleMapper
        services.AddCouchbaseSimpleMapper(opts =>
        {
            opts.DefaultBucket = configuration["SimpleMapper:DefaultBucket"];
            opts.DefaultScope = configuration["SimpleMapper:DefaultScope"];
        });

        // Register bucket for easy access
        services.AddSingleton(sp =>
        {
            var bucketProvider = sp.GetRequiredService<IBucketProvider>();
            var bucketName = configuration["Couchbase:BucketName"] ?? "todos";
            return bucketProvider.GetBucketAsync(bucketName).GetAwaiter().GetResult();
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }
}