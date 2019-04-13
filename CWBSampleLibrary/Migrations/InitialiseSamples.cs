using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using CWBSampleLibrary.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace CWBSampleLibrary.Migrations
{
    public class InitialiseSamples
    {

        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
        private static CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        private static CloudTable table = tableClient.GetTableReference("Samples");
        private const String partitionName = "samples_Partition_1";

        public static void go()
        {




            // If table doesn't already exist in storage then create and populate it with some initial values, otherwise do nothing
            if (!table.Exists())
            {
                // Create table if it doesn't exist already
                table.CreateIfNotExists();

                // Create the titles and artists to seed initially
                string[] Titles =
                {
                    "Song One",
                    "Song Two",
                    "Song Three",
                    "Song Four"
                };
                string[] Artists =
                {
                    "Artist One",
                    "Artist Two",
                    "Artist Three",
                    "Artist Four"
                };

                // Loop through and add the entities
                for (int i = 0; i < Titles.Length; i++)
                {
                    SampleEntity sample = new SampleEntity(partitionName, getNewMaxRowKeyValue());
                    sample.Title = Titles[i];
                    sample.Artist = Artists[i];
                    sample.CreatedDate = DateTime.Now;
                    sample.Mp3Blob = null;
                    sample.SampleMp3Blob = null;
                    sample.SampleMp3Url = null;
                    sample.SampleDate = DateTime.Now;

                    var insertSample = TableOperation.Insert(sample);
                    table.Execute(insertSample);

                }

            }



        }

        /// <summary>
        /// Get the next logical ID from the database based on the previous highest number
        /// </summary>
        /// <returns>The next ID in the sequence</returns>
        private static String getNewMaxRowKeyValue()
        {
            TableQuery<SampleEntity> query = new TableQuery<SampleEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));

            int maxRowKeyValue = 0;
            foreach (SampleEntity entity in table.ExecuteQuery(query))
            {
                int entityRowKeyValue = Int32.Parse(entity.RowKey);
                if (entityRowKeyValue > maxRowKeyValue) maxRowKeyValue = entityRowKeyValue;
            }
            maxRowKeyValue++;
            return maxRowKeyValue.ToString();
        }



    }
}