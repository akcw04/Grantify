using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Grantify.Services;

// What the processing microservice read out of one uploaded document.
// Confidence is Textract's own average score for the lines it found, 0-100.
public record DocumentExtraction(
    int DocumentId,
    string Text,
    decimal Confidence,
    int LineCount,
    DateTime? ProcessedOn);

// OWNER: Member B (Officer role). Task 2 service.
//
// Reads back what the document-processing microservice wrote. The Lambda puts
// its findings in an Amazon DynamoDB table; this class is the only thing in the
// web application that reads them.
//
// HOW IT IS SWITCHED ON
//   Documents__ExtractionTable set -> read the findings from that table
//   not set                        -> return nothing, and the officer page
//                                     simply shows no suggestion
//
// WHY DYNAMODB AND NOT OUR SQL DATABASE
// The RDS instance sits inside the VPC. A Lambda that needed to reach it would
// have to run inside the VPC too, and a VPC Lambda has no route to the public
// AWS APIs (S3, Textract) without a NAT gateway — which costs more per month
// than our whole lab budget. DynamoDB is a public AWS API, so the pipeline can
// write its results without any of that.
//
// The useful consequence is architectural, not just financial: the extraction
// pipeline never touches the transactional database. The web application joins
// the two at read time, and the officer's confirmation is what finally gets
// written to the system of record in RDS. The machine suggests; the human decides.
public class DocumentExtractionReader
{
    // DynamoDB accepts at most 100 keys in one BatchGetItem call.
    private const int BatchLimit = 100;

    private readonly ILogger<DocumentExtractionReader> _logger;
    private readonly string? _tableName;

    public DocumentExtractionReader(IConfiguration configuration, ILogger<DocumentExtractionReader> logger)
    {
        _logger = logger;

        var name = configuration["Documents:ExtractionTable"];
        _tableName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    // Looks up several documents at once, because the verification page shows a
    // whole queue and one call per row would be wasteful.
    //
    // NEVER throws. A missing or unreachable table means the officer sees the
    // page exactly as it looked in Task 1 — they open the file and read it
    // themselves. Losing a convenience must never cost them the page.
    public async Task<Dictionary<int, DocumentExtraction>> GetManyAsync(IEnumerable<int> documentIds)
    {
        var found = new Dictionary<int, DocumentExtraction>();

        if (_tableName is null)
        {
            // Logged rather than returned silently: a missing property and a
            // table with no matching rows look identical on the page, and that
            // is exactly the kind of thing that costs an afternoon to diagnose.
            _logger.LogInformation(
                "Document extractions are switched off (Documents__ExtractionTable is not set), " +
                "so no machine readings will be shown.");
            return found;
        }

        var ids = documentIds.Distinct().ToList();
        if (ids.Count == 0) return found;

        try
        {
            // On Elastic Beanstalk the client picks up the instance's role
            // automatically, so there are no keys in our code or config.
            using var db = new AmazonDynamoDBClient();

            foreach (var chunk in ids.Chunk(BatchLimit))
            {
                var request = new BatchGetItemRequest
                {
                    RequestItems = new Dictionary<string, KeysAndAttributes>
                    {
                        [_tableName] = new KeysAndAttributes
                        {
                            Keys = chunk
                                .Select(id => new Dictionary<string, AttributeValue>
                                {
                                    ["DocumentId"] = new AttributeValue { N = id.ToString() }
                                })
                                .ToList()
                        }
                    }
                };

                var response = await db.BatchGetItemAsync(request);

                if (!response.Responses.TryGetValue(_tableName, out var items)) continue;

                foreach (var item in items)
                {
                    var extraction = Read(item);
                    if (extraction is not null) found[extraction.DocumentId] = extraction;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Document extractions could not be read; the verification page will show none.");
        }

        return found;
    }

    // Turns one DynamoDB item into our own type. Every field is optional on
    // purpose — a half-written item should degrade to "no suggestion", never
    // to an exception on a page an officer is trying to work through.
    private static DocumentExtraction? Read(Dictionary<string, AttributeValue> item)
    {
        if (!item.TryGetValue("DocumentId", out var idValue)) return null;
        if (!int.TryParse(idValue.N, out var documentId)) return null;

        var text = item.TryGetValue("ExtractedText", out var t) ? t.S ?? string.Empty : string.Empty;

        decimal confidence = 0m;
        if (item.TryGetValue("Confidence", out var c))
            decimal.TryParse(c.N, NumberStyles.Any, CultureInfo.InvariantCulture, out confidence);

        var lineCount = 0;
        if (item.TryGetValue("LineCount", out var l))
            int.TryParse(l.N, out lineCount);

        DateTime? processedOn = null;
        if (item.TryGetValue("ProcessedOn", out var p) &&
            DateTime.TryParse(p.S, CultureInfo.InvariantCulture,
                              DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                              out var parsed))
        {
            processedOn = parsed;
        }

        return new DocumentExtraction(documentId, text, confidence, lineCount, processedOn);
    }
}
