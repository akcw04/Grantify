using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Grantify.Services;

// OWNER: Member B (Officer role). Task 2 service.
//
// Hands a freshly uploaded document to the processing microservice by putting a
// small job message on an Amazon SQS queue. The Lambda on the other end reads
// the file from S3, extracts the text, and announces the result on SNS.
//
// HOW IT IS SWITCHED ON
//   Documents__ProcessingQueueUrl set -> send the job to that queue
//   not set                           -> do nothing, just write a line in the log
//
// The second mode is what keeps local development working: teammates press F5
// with no AWS credentials and the upload behaves exactly as it did in Task 1 —
// the document simply stays Pending for an officer to check by hand.
//
// WHY A QUEUE RATHER THAN CALLING THE EXTRACTION DIRECTLY
// Reading a document takes seconds. A student's upload must not wait for it.
// The queue lets the web app answer immediately and lets the extraction happen
// on its own schedule, and it absorbs a burst of uploads at intake time without
// the web server having to grow to match.
public class DocumentQueuePublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<DocumentQueuePublisher> _logger;
    private readonly string? _queueUrl;
    private readonly string? _bucketName;

    public DocumentQueuePublisher(IConfiguration configuration, ILogger<DocumentQueuePublisher> logger)
    {
        _logger = logger;

        var url = configuration["Documents:ProcessingQueueUrl"];
        _queueUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();

        var bucket = configuration["Storage:BucketName"];
        _bucketName = string.IsNullOrWhiteSpace(bucket) ? null : bucket.Trim();
    }

    // What the web app sends to the microservice: only the three facts it needs
    // to find and identify the file. No student name, no personal detail.
    //
    // The property names must stay camelCase on the wire (documentId, bucket,
    // key) because the Lambda reads them by those exact names. JsonOptions above
    // is what does that — do not change it to the default serializer.
    private record ProcessingJob(int DocumentId, string Bucket, string Key);

    // Queues one document for processing.
    //
    // NEVER throws. The file is already in S3 and the row is already in the
    // database — a queueing problem must not fail an upload the student
    // completed successfully. We log it and carry on, and the officer can still
    // check that document by hand exactly as they did in Task 1.
    public async Task QueueAsync(int documentId, string storagePath)
    {
        if (_queueUrl is null || _bucketName is null)
        {
            _logger.LogInformation(
                "Document processing is switched off, so document {DocumentId} was not queued.",
                documentId);
            return;
        }

        try
        {
            // storagePath is already the S3 object key ("7/abc123.png") —
            // DocumentStorageService builds it in that shape for both modes.
            var job = new ProcessingJob(documentId, _bucketName, storagePath);

            // On Elastic Beanstalk the client picks up the instance's role
            // automatically, so there are no keys in our code or config.
            using var sqs = new AmazonSQSClient();
            await sqs.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = _queueUrl,
                MessageBody = JsonSerializer.Serialize(job, JsonOptions)
            });

            _logger.LogInformation("Document {DocumentId} queued for processing.", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Document {DocumentId} could not be queued; it stays Pending for a manual check.",
                documentId);
        }
    }
}
