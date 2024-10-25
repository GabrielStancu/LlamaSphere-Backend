﻿using Azure.Storage.Blobs;
using DocumentFormat.OpenXml.Packaging;
using LlamaSphere.API.Business.TableStorage;
using LlamaSphere.API.Configuration;
using LlamaSphere.API.Entities;
using Microsoft.Extensions.Options;

namespace LlamaSphere.API.Services;

public class JobUploadService : IJobUploadService
{
    private readonly ITableStorageClient<JobEntity> _tableStorageClient;
    private readonly BlobStorageConfiguration _blobStorageConfiguration;

    public JobUploadService([FromKeyedServices("job")] ITableStorageClient<JobEntity> tableStorageClient,
        IOptions<BlobStorageConfiguration> blobStorageConfiguration)
    {
        _tableStorageClient = tableStorageClient;
        _blobStorageConfiguration = blobStorageConfiguration.Value;
    }

    public async Task UploadFileAsync(IFormFile file)
    {
        var content = await ExtractFileContentAsync(file);
        var id = await UploadFileContentToTableStorageAsync(content);
        await UploadFileToBlobStorageAsync(file, id);
    }

    private static async Task<string> ExtractFileContentAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file uploaded");
        }

        if (Path.GetExtension(file.FileName) != ".docx")
        {
            throw new ArgumentException("Bad file format uploaded. Only accepts '.docx'");
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        using var wordDoc = WordprocessingDocument.Open(stream, false);
        var body = wordDoc.MainDocumentPart?.Document.Body;
        var content = body?.InnerText ?? string.Empty;

        return content;
    }

    private async Task<string> UploadFileContentToTableStorageAsync(string content)
    {
        var id = Guid.NewGuid().ToString();
        var jobEntity = new JobEntity
        {
            PartitionKey = id,
            RowKey = id,
            Content = content
        };

        await _tableStorageClient.UpsertEntityAsync(jobEntity);

        return id;
    }

    private async Task UploadFileToBlobStorageAsync(IFormFile file, string id)
    {
        var blobClient = new BlobContainerClient(_blobStorageConfiguration.ConnectionString, _blobStorageConfiguration.JobsContainerName);

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        await blobClient.UploadBlobAsync($"{id}_{file.FileName}", stream);
    }
}