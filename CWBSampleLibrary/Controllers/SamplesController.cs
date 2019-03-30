using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using CWBSampleLibrary.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace CWBSampleLibrary.Controllers
{
    public class SamplesController : ApiController
    {
        //        private const String partitionName = "Samples_Partition_1";
        private const String partitionName = "samples_Partition_1";

        // CONSTANT FOR REPRESENT TABLE NAME
        public const String TABLE_NAME = "Samples";

        private CloudStorageAccount storageAccount;
        private CloudTableClient tableClient;
        private CloudTable table;

        public SamplesController()
        {
            storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
            tableClient = storageAccount.CreateCloudTableClient();
            table = tableClient.GetTableReference(TABLE_NAME);
        }

        /// <summary>
        /// Get all samples
        /// </summary>
        /// <returns></returns>
        // GET: api/Samples
        public IEnumerable<Sample> Get()
        {
            TableQuery<SampleEntity> query = new TableQuery<SampleEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));
            List<SampleEntity> entityList = new List<SampleEntity>(table.ExecuteQuery(query));

            // Basically create a list of Sample from the list of SampleEntity with a 1:1 object relationship, filtering data as needed
            IEnumerable<Sample> sampleList = from e in entityList
                                             select new Sample()
                                             {
                                                 SampleID = e.RowKey,
                                                 Title = e.Title,
                                                 Artist = e.Artist,
                                                 SampleMp3Url = e.SampleMp3Url
                                             };
            return sampleList;
        }

        // GET: api/Samples/5
        /// <summary>
        /// Get a sample
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(Sample))]
        public IHttpActionResult GetSample(string id)
        {
            // Create a retrieve operation that takes a sample entity.
            TableOperation getOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the retrieve operation.
            TableResult getOperationResult = table.Execute(getOperation);

            // Construct response including a new DTO as apprporiatte
            if (getOperationResult.Result == null) return NotFound();
            else
            {
                SampleEntity sampleEntity = (SampleEntity)getOperationResult.Result;
                Sample sample = new Sample()
                {
                    SampleID = sampleEntity.RowKey,
                    Title = sampleEntity.Title,
                    Artist = sampleEntity.Artist

                };
                return Ok(sample);
            }
        }

        // POST: api/Samples
        /// <summary>
        /// Create a new sample
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        //[SwaggerResponse(HttpStatusCode.Created)]
        [ResponseType(typeof(Sample))]
        public IHttpActionResult PostSample(Sample sample)
        {

            if (sample == null)
            {
                return BadRequest("Bad Request. Please ensure your request is properly formatted");
            }

            // GET THE ID FOR THE NEW ENTITY
            String sId = getNewMaxRowKeyValue();

            SampleEntity sampleEntity = new SampleEntity()
            {
                RowKey = sId,
                PartitionKey = partitionName,
                Title = sample.Title,
                Artist = sample.Artist,
                CreatedDate = DateTime.Now,
                Mp3Blob = null,
                SampleMp3Blob = null,
                SampleMp3Url = null,
                SampleDate = DateTime.Now

            };

            // Create the TableOperation that inserts the sample entity.
            var insertOperation = TableOperation.Insert(sampleEntity);

            // Execute the insert operation.
            table.Execute(insertOperation);

            // Update the ID on the Sample to return to the caller
            sample.SampleID = sId;

            // RETURN A 201 CREATED
            return CreatedAtRoute("DefaultApi", new { id = sampleEntity.RowKey }, sample);
        }

        // PUT: api/Samples/5
        /// <summary>
        /// Update a sample
        /// </summary>
        /// <param name="id"></param>
        /// <param name="sample"></param>
        /// <returns></returns>
        //[SwaggerResponse(HttpStatusCode.NoContent)]
        [ResponseType(typeof(void))]
        public IHttpActionResult PutSample(string id, Sample sample)
        {
            // CHECK TO MAKE SURE THERE IS A SAMPLE SUPPLIED
            if (sample == null)
            {
                return BadRequest("You must supply a sample to update");
            }

            // CHECK TO MAKE SURE ID MATCHES THE SAMPLE
            if (id != sample.SampleID)
            {
                return BadRequest("Your ID's do not match");
            }

            // Create a retrieve operation that takes a sample entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);

            // IF THERE IS NO ENTITY, RETURN NOT FOUND
            if (retrievedResult.Result == null) return NotFound();


            // Assign the result to a SampleEntity object.
            SampleEntity updateEntity = (SampleEntity)retrievedResult.Result;

            updateEntity.Title = sample.Title;
            updateEntity.Artist = sample.Artist;
            //            updateEntity.Mp3Blob = sample.Mp3Blob;
            //            updateEntity.SampleMp3Blob = sample.SampleMp3Blob;
            updateEntity.SampleMp3Url = sample.SampleMp3Url;

            // Create the TableOperation that inserts the sample entity.
            // Note semantics of InsertOrReplace() which are consistent with PUT
            // See: https://stackoverflow.com/questions/14685907/difference-between-insert-or-merge-entity-and-insert-or-replace-entity
            var updateOperation = TableOperation.InsertOrReplace(updateEntity);

            // Execute the insert operation.
            table.Execute(updateOperation);

            return StatusCode(HttpStatusCode.NoContent);
        }

        // DELETE: api/Samples/5
        /// <summary>
        /// Delete a sample
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ResponseType(typeof(Sample))]
        public IHttpActionResult DeleteSample(string id)
        {
            // Create a retrieve operation that takes a sample entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);
            if (retrievedResult.Result == null)
            {
                return NotFound();
            }
            else
            {
                SampleEntity deleteEntity = (SampleEntity)retrievedResult.Result;
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                table.Execute(deleteOperation);

                // JUST RETURN A 204
                return StatusCode(HttpStatusCode.NoContent);
            }
        }

        private String getNewMaxRowKeyValue()
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
