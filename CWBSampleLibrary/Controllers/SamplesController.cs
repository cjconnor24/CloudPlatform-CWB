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
    public class SamplesController : BaseController
    {

        public SamplesController() : base()
        {

        }

        /// <summary>
        /// Return a list of all samples
        /// </summary>
        /// <returns>An IEnumerable list of samples</returns>
        // GET: api/Samples
        public IEnumerable<Sample> Get()
        {
            try
            {

                // GET THE SAMPLES FROM THE DB
                TableQuery<SampleEntity> query = new TableQuery<SampleEntity>()
                    .Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));
                List<SampleEntity> entityList = new List<SampleEntity>(table.ExecuteQuery(query));

                // TODO: REFACTOR THIS - REMOVE THE TRY AND CATCH

                // CREATE A LIST OF Sample() Objects to be returned from the API
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
            catch (Exception e)
            {
                // IF there are none - return an empty list
                IEnumerable<Sample> s = new List<Sample>();
                return s;
            }
        }

        // GET: api/Samples/5
        /// <summary>
        /// Retrieve a specific sample resource based on the ID
        /// </summary>
        /// <param name="id">The id of the sample to be retrieve</param>
        /// <returns>A sample if it exists</returns>
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
                    Artist = sampleEntity.Artist,
                    SampleMp3Url = sampleEntity.SampleMp3Url

                };
                return Ok(sample);
            }
        }

        // POST: api/Samples
        /// <summary>
        /// Create a new sample entity in the DB
        /// </summary>
        /// <param name="sample">The sample to be stored in the DB</param>
        /// <returns>The sample object created</returns>
        [ResponseType(typeof(Sample))]
        public IHttpActionResult PostSample(Sample sample)
        {

            if (sample == null)
            {
                return BadRequest("Bad Request. Please ensure your request is properly formatted");
            }

            // GET THE ID FOR THE NEW ENTITY
            String sId = getNewMaxRowKeyValue();

            // CREATE THE NEW ENTITY
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
        /// Update an existing sample in the DB
        /// </summary>
        /// <param name="id">The id of the sample to be updated</param>
        /// <param name="sample">The sample object data to be updated</param>
        /// <returns></returns>
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

            // Delete any existing blobs in storage
            Mp3sController mp3s = new Mp3sController();
            mp3s.Delete(updateEntity);

            // Create the updates on the object
            updateEntity.Title = sample.Title;
            updateEntity.Artist = sample.Artist;
            updateEntity.Mp3Blob = null;
            updateEntity.SampleMp3Blob = null;
            updateEntity.SampleMp3Url = null;

            // CREATE THE UPDATE OPERATION
            var updateOperation = TableOperation.InsertOrReplace(updateEntity);

            // EXECUTE THE UPDATE OPERATION
            table.Execute(updateOperation);

            return StatusCode(HttpStatusCode.NoContent);
        }

        // DELETE: api/Samples/5
        /// <summary>
        /// Delete an existing sample
        /// </summary>
        /// <param name="id">The ID of the sample to be deleted</param>
        /// <returns>201 status code if successful</returns>
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

                // GET THE RESULT TO DELETE
                SampleEntity deleteEntity = (SampleEntity)retrievedResult.Result;

                // REMOVE ANY RELATED MP3 IN BLOB STORAGE
                Mp3sController mp3 = new Mp3sController();
                mp3.Delete(deleteEntity);


                // CREATE THE DELETE OPERATION
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // EXECUTE THE DELETE OPERATION
                table.Execute(deleteOperation);

                // JUST RETURN A 204
                return StatusCode(HttpStatusCode.NoContent);
            }
        }

        /// <summary>
        /// Get the next ID in the database
        /// </summary>
        /// <returns>The next sequencial ID in the database</returns>
        private String getNewMaxRowKeyValue()
        {

            TableQuery<SampleEntity> query = new TableQuery<SampleEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionName));

            // MAKE SURE THERE ARE TABLE ROWS BEFORE ATTEMPTING TO ITERATE
            var results = table.ExecuteQuery(query);
            if (results.Any())
            {

                int maxRowKeyValue = 0;
                // LOOP THROUGH THE RESULTS, CHECKING THE IDS
                foreach (SampleEntity entity in results)
                {
                    int entityRowKeyValue = Int32.Parse(entity.RowKey);
                    if (entityRowKeyValue > maxRowKeyValue) maxRowKeyValue = entityRowKeyValue;
                }

                // GET THE NEXT VALUE IN THE SEQUENCE
                maxRowKeyValue++;
                return maxRowKeyValue.ToString();

            }

            // IF NO RESULTS, RETURN 1
            return 1.ToString();

        }
    }
}
