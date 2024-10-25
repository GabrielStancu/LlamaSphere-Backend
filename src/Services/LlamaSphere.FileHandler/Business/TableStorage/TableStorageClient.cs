﻿using Azure.Data.Tables;

namespace LlamaSphere.API.Business.TableStorage;

public abstract class TableStorageClient<T> : ITableStorageClient<T> where T : class, ITableEntity
{
    private readonly ILogger _logger;
    protected TableClient TableClient;

    protected TableStorageClient(ILogger<TableStorageClient<T>> logger)
    {
        _logger = logger;
    }

    public async Task<T> GetEntityAsync(string partitionKey, string rowKey)
    {
        await InitTableClientAsync();

        try
        {
            return await TableClient.GetEntityAsync<T>(partitionKey, rowKey);
        }
        catch (Exception ex)
        {
            _logger.LogError("Could not find entity with partition key {NotFoundPartitionKey} and row key {NotFoundRowKey}", partitionKey, rowKey);
            _logger.LogError("Exception while querying table storage: {TableStorageException} @ {TableStorageExceptionStackTrace}", ex.Message, ex.StackTrace);

            return null;
        }
    }

    public async Task UpsertEntityAsync(T entity)
    {
        await InitTableClientAsync();

        await TableClient.UpsertEntityAsync(entity);
    }

    protected abstract Task InitTableClientAsync();
}
