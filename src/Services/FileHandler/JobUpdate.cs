using DocumentFormat.OpenXml.Packaging;
using FileHandler.DTOs;
using FileHandler.Models;
using FileHandler.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FileHandler
{
    public class JobUpdate
    {
        private readonly IFileParserService _fileParserService;
        private readonly IJobsTableStorageService _jobsTableStorageService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<JobUpdate> _logger;

        public JobUpdate(IFileParserService fileParserService,
            IJobsTableStorageService jobsTableStorageService,
            [FromKeyedServices("alert")] IEmailSender emailSender,
            ILogger<JobUpdate> logger)
        {
            _fileParserService = fileParserService;
            _jobsTableStorageService = jobsTableStorageService;
            _emailSender = emailSender;
            _logger = logger;
        }

        [Function(nameof(JobUpdate))]
        public async Task Run([BlobTrigger("jobs/{file}", Connection = "StorageAccount:ConnectionString")] Stream stream, string file)
        {
            try
            {
                _logger.LogInformation("Parsing file {File}", file);

                var fileContent = ExtractFileContentAsync(stream);
                //var parsedJob = await _fileParserService.ParseJobFileAsync(fileContent);
                var parsedJob = new ParsedJob
                {
                    Id = file.Substring(0, file.IndexOf("_", StringComparison.Ordinal)),
                    Title = file.Substring(file.IndexOf("_", StringComparison.Ordinal) + 1)
                };

                await _jobsTableStorageService.UpdateTableStorageContent(parsedJob);
                await _emailSender.SendEmailAlertAsync(new NewJobEmailModel
                {
                    Name = parsedJob.Title
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing blob. Exception: {ex.Message}");
            }
        }

        private static string ExtractFileContentAsync(Stream stream)
        {
            using var wordDoc = WordprocessingDocument.Open(stream, false);
            var body = wordDoc.MainDocumentPart?.Document.Body;
            var content = body?.InnerText ?? string.Empty;

            return content;
        }
    }
}
