using System;

namespace Lib.Model
{
    public interface IImage
    {
        string PartitionKey { get; set; }
        string Id { get; set; }
        string Uid { get; set; }
        string Title { get; set; }
        string Url { get; set; }
        string Extension { get; set; }
        string BlobName { get; set; }
        string BlobUri { get; set; }
        string Text { get; set; }
        string Sentiment { get; set; }
        string Status { get; set; }
        DateTime CreatedDate { get; set; }
        string Color { get; set; }
        string Error { get; set; }
        bool Complete { get; set; }
        string GetBlobUri(bool proxy, string endpoint, string container);
    }
}
