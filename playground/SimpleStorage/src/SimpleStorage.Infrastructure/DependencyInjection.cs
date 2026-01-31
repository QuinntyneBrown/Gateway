// Copyright (c) Quinntyne Brown. All Rights Reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Couchbase;
using Couchbase.Core.IO.Serializers;
using Couchbase.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleStorage.Core.Interfaces;
using SimpleStorage.Infrastructure.Repositories;
using Gateway.Core.Extensions;

namespace SimpleStorage.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Couchbase
        services.AddCouchbase(options =>
        {
            options.ConnectionString = configuration["Couchbase:ConnectionString"] ?? "couchbase://localhost";
            options.UserName = configuration["Couchbase:UserName"] ?? "Administrator";
            options.Password = configuration["Couchbase:Password"] ?? "password";
            options.Serializer = SystemTextJsonSerializer.Create();
        });

        // Configure Gateway SimpleMapper
        services.AddCouchbaseSimpleMapper(opts =>
        {
            opts.DefaultBucket = configuration["Couchbase:DefaultBucket"] ?? "general";
            opts.DefaultScope = configuration["Couchbase:DefaultScope"] ?? "artifacts";
        });

        // Register bucket for easy access
        services.AddSingleton(sp =>
        {
            var bucketProvider = sp.GetRequiredService<IBucketProvider>();
            var bucketName = configuration["Couchbase:DefaultBucket"] ?? "general";
            return bucketProvider.GetBucketAsync(bucketName).GetAwaiter().GetResult();
        });

        // Register repository with factory pattern to get bucket and scope from configuration
        services.AddScoped<IMetadataRepository>(sp =>
        {
            var bucket = sp.GetRequiredService<IBucket>();
            var scopeName = configuration["Couchbase:DefaultScope"] ?? "artifacts";
            return new MetadataRepository(bucket, scopeName);
        });

        return services;
    }
}
