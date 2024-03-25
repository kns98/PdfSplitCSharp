using System;
using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: Program <filename> [maxSizeInMB]");
            Environment.Exit(1);
        }

        string filename = args[0];
        int maxSizeInMb = args.Length > 1 ? int.Parse(args[1]) : 20; // Default size is 20MB
        long maxSizeInBytes = ConvertMbToBytes(maxSizeInMb);

        if (GetFileSize(filename) > maxSizeInBytes)
        {
            try
            {
                SplitPdf(filename, maxSizeInBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("The file size is already within the specified limit.");
        }
    }

    static long ConvertMbToBytes(int mb)
    {
        return mb * 1024L * 1024L;
    }

    static long GetFileSize(string filename)
    {
        return new FileInfo(filename).Length;
    }

    static void SplitPdf(string filename, long maxSizeBytes)
    {
        PdfDocument document = PdfReader.Open(filename, PdfDocumentOpenMode.Import);
        List<PdfDocument> documents = new List<PdfDocument>();
        int pageCount = document.PageCount;
        int startPage = 0;
        bool splitSuccessful = false;

        while (!splitSuccessful && startPage < pageCount)
        {
            for (int endPage = startPage + 1; endPage <= pageCount; endPage++)
            {
                PdfDocument newDoc = new PdfDocument();
                for (int pageIndex = startPage; pageIndex < endPage && pageIndex < pageCount; pageIndex++)
                {
                    newDoc.AddPage(document.Pages[pageIndex]);
                }

                // Check the tentative file size by saving to a memory stream
                MemoryStream ms = new MemoryStream();
                newDoc.Save(ms, false);
                if (ms.Length > maxSizeBytes || endPage == pageCount)
                {
                    if (ms.Length > maxSizeBytes && newDoc.PageCount == 1)
                    {
                        throw new InvalidOperationException("A single page exceeds the maximum file size, unable to split.");
                    }

                    documents.Add(newDoc); // Add the last successfully created document before exceeding the limit
                    startPage = endPage; // Update startPage for the next document
                    break; // Break the inner loop to start a new document
                }

                if (endPage == pageCount - 1) // If we're on the last page and haven't exceeded size
                {
                    documents.Add(newDoc); // This ensures the last document is added if we're under size
                    splitSuccessful = true;
                }
            }
        }

        SaveDocuments(documents, filename);
    }

    static void SaveDocuments(List<PdfDocument> documents, string originalFilename)
    {
        string folder = Path.GetDirectoryName(originalFilename);
        string filenameWithoutExt = Path.GetFileNameWithoutExtension(originalFilename);
        string extension = Path.GetExtension(originalFilename);

        for (int i = 0; i < documents.Count; i++)
        {
            string newFilename = Path.Combine(folder, $"{filenameWithoutExt}_part{i + 1}{extension}");
            documents[i].Save(newFilename);
            Console.WriteLine($"Saved: {newFilename}");
        }
    }
}
