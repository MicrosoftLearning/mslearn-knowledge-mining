using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OptimizeDataIndexing
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            IConfigurationRoot configuration = builder.Build();

            string searchServiceUri = configuration["SearchServiceUri"];
            string adminApiKey = configuration["SearchServiceAdminApiKey"];
            string indexName = configuration["SearchIndexName"];

            SearchIndexClient indexClient = new SearchIndexClient(new Uri(searchServiceUri), new AzureKeyCredential(adminApiKey));
            SearchClient searchClient = indexClient.GetSearchClient(indexName);   

            Console.WriteLine("{0}", "Deleting index...\n");
            await DeleteIndexIfExistsAsync(indexName, indexClient);

            Console.WriteLine("{0}", "Creating index...\n");
            await CreateIndexAsync(indexName, indexClient);

            Console.WriteLine("{0}", "Finding optimal batch size...\n");
            await TestBatchSizesAsync(searchClient, numTries: 3);

            //long numDocuments = 100000;
            //DataGenerator dg = new DataGenerator();
            //List<Hotel> hotels = dg.GetHotels(numDocuments, "large");

            //Console.WriteLine("{0}", "Uploading using exponential backoff...\n");
            //await ExponentialBackoff.IndexDataAsync(searchClient, hotels, 1000, 8);

            //Console.WriteLine("{0}", "Validating all data was indexed...\n");
            //await ValidateIndexAsync(indexClient, indexName, numDocuments);

            Console.WriteLine("{0}", "Complete.  Press any key to end application...\n");
            Console.ReadKey();
        }

        // Delete an existing index to reuse its name
        private static async Task DeleteIndexIfExistsAsync(string indexName, SearchIndexClient indexClient)
        {
            try
            {
                await indexClient.GetIndexAsync(indexName);
                await indexClient.DeleteIndexAsync(indexName);
            }
            catch (RequestFailedException ex) when (ex.Status == 404) 
            {
                //if the specified index not exist, 404 will be thrown.
            }
        }

        private static async Task CreateIndexAsync(string indexName, SearchIndexClient indexClient)
        {
            // Create a new search index structure that matches the properties of the Hotel class.
            // The Address class is referenced from the Hotel class. The FieldBuilder
            // will enumerate these to create a complex data structure for the index.
            FieldBuilder builder = new FieldBuilder();
            var definition = new SearchIndex(indexName, builder.Build(typeof(Hotel)));

            await indexClient.CreateIndexAsync(definition);
        }

        public static async Task UploadDocumentsAsync(SearchClient searchClient, List<Hotel> hotels)
        {
            var batch = IndexDocumentsBatch.Upload(hotels);
            try
            {               
                await searchClient.IndexDocumentsAsync(batch).ConfigureAwait(false);
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine("Failed to index the documents: \n{0}",ex.Message);
            }
        }

        public static async Task TestBatchSizesAsync(SearchClient searchClient, int min = 100, int max = 1000, int step = 100, int numTries = 3)
        {
            DataGenerator dg = new DataGenerator();

            Console.WriteLine("Batch Size \t Size in MB \t MB / Doc \t Time (ms) \t MB / Second");
            for (int numDocs = min; numDocs <= max; numDocs += step)
            {
                List<TimeSpan> durations = new List<TimeSpan>();
                double sizeInMb = 0.0;
                for (int x = 0; x < numTries; x++)
                {
                    List<Hotel> hotels = dg.GetHotels(numDocs, "large");

                    DateTime startTime = DateTime.Now;
                    await UploadDocumentsAsync(searchClient, hotels).ConfigureAwait(false);
                    DateTime endTime = DateTime.Now;
                    durations.Add(endTime - startTime);

                    sizeInMb = EstimateObjectSize(hotels);
                }

                var avgDuration = durations.Average(timeSpan => timeSpan.TotalMilliseconds);
                var avgDurationInSeconds = avgDuration / 1000;
                var mbPerSecond = sizeInMb / avgDurationInSeconds;

                Console.WriteLine("{0} \t\t {1} \t\t {2} \t\t {3} \t {4}", numDocs, Math.Round(sizeInMb, 3), Math.Round(sizeInMb / numDocs, 3), Math.Round(avgDuration, 3), Math.Round(mbPerSecond, 3));

                // Pausing 2 seconds to let the search service catch its breath
                Thread.Sleep(2000);
            }

            Console.WriteLine();
        }

        // Returns size of object in MB
        public static double EstimateObjectSize(object data)
        {
            // converting object to byte[] to determine the size of the data
            MemoryStream ms = new MemoryStream();
            byte[] Array;

            // converting data to json for more accurate sizing
            var json = JsonSerializer.Serialize(data);

            
            Array = JsonSerializer.SerializeToUtf8Bytes(json);

            // converting from bytes to megabytes
            double sizeInMb = (double)Array.Length / 1000000;

            return sizeInMb;
        }

        public static async Task ValidateIndexAsync(SearchIndexClient indexClient, string indexName, long numDocsIndexed)
        {
            SearchClient searchClient = indexClient.GetSearchClient(indexName);

            long indexDocCount = await searchClient.GetDocumentCountAsync();
            while (indexDocCount != numDocsIndexed)
            {
                Console.WriteLine("Waiting for document count to update...\n");
                Thread.Sleep(2000);
                indexDocCount = await searchClient.GetDocumentCountAsync();
            }
            Console.WriteLine("Document Count is {0}\n", indexDocCount);


            var indexStats = await indexClient.GetIndexStatisticsAsync(indexName);
            while (indexStats.Value.DocumentCount != numDocsIndexed)
            {
                Console.WriteLine("Waiting for service statistics to update...\n");
                Thread.Sleep(10000);
                indexStats = await indexClient.GetIndexStatisticsAsync(indexName);
            }
            Console.WriteLine("Index Statistics: Document Count is {0}", indexStats.Value.DocumentCount);
            Console.WriteLine("Index Statistics: Storage Size is {0}\n", indexStats.Value.StorageSize);
        }

    }
}
