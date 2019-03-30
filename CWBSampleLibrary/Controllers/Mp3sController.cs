using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using CWBSampleLibrary.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace CWBSampleLibrary.Controllers
{
    public class Mp3sController : ApiController
    {

        //        private const String partitionName = "Samples_Partition_1";
        private const String partitionName = "samples_Partition_1";
        // accessor variables and methods for blob containers and queues
        private BlobStorageService _blobStorageService = new BlobStorageService();
        private CloudQueueService _queueStorageService = new CloudQueueService();
        private CloudStorageAccount _storageAccount;
        private CloudTableClient _tableClient;
        private CloudTable table;
        //        private CloudBlobContainer audioGalleryContainer;
        //        private CloudQueue sampleQueue;

        private CloudBlobContainer getaudiogalleryContainer()
        {
            return _blobStorageService.getCloudBlobContainer();
        }

        private CloudQueue getsamplegeneratorQueue()
        {
            return _queueStorageService.getCloudQueue();
        }

        public Mp3sController()
        {
            _storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
            _tableClient = _storageAccount.CreateCloudTableClient();
            table = _tableClient.GetTableReference("Samples");
            //            audioGalleryContainer = _blobStorageService.getCloudBlobContainer();
            //            sampleQueue = _queueStorageService.getCloudQueue();
        }

        // GET: api/Mp3s
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Mp3s/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Mp3s
        [ResponseType(typeof(Sample))]
        public IHttpActionResult Post(string id)
        {



            Console.WriteLine(id);
            // GET THE ID - MAKE SURE IT ISNT BLANK
            if (id == null)
            {
                Console.WriteLine(id);
                return BadRequest("Please enter an ID");
            }

            // CHECK THE ID EXISTS IN THE TABLE ALREADY
            // Create a retrieve operation that takes a sample entity.
            TableOperation getOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // Execute the retrieve operation.
            TableResult getOperationResult = table.Execute(getOperation);

            // Construct response including a new DTO as apprporiate
            if (getOperationResult.Result == null) return NotFound();

            // OTHERWISE GET THE SAMPLE
            SampleEntity sampleEntity = (SampleEntity)getOperationResult.Result;
            //            Sample sample = new Sample()
            //            {
            //                SampleID = sampleEntity.RowKey,
            //                Title = sampleEntity.Title,
            //                Artist = sampleEntity.Artist
            //
            //            };

            // CHECK IF BLOBS EXISTS ALREADY
            if (sampleEntity.Mp3Blob != null)
            {
                // TODO: DELETE THE EXISTING BLOBS
                Delete(id);
            }

            // CREATE NAME FROM Sample Data
            string title = sampleEntity.Title;
            string fileName = string.Format("{0}{1}{2}", Guid.NewGuid(), title.Replace(" ", "-"), ".mp3");
            string path = "files/" + fileName;

            // GET THE BINARY SAMPLE BEING UPLOADED
            var postData = HttpContext.Current.Request;
            //            var filename = 
            var blob = getaudiogalleryContainer().GetBlockBlobReference(path);
            blob.Properties.ContentType = "audio/mpeg3";
            blob.Metadata["Title"] = title;

            // SAVE THE BLOB
            blob.UploadFromStream(postData.InputStream);
            blob.SetMetadata();

            // WRITE TO THE TABLES
            sampleEntity.Mp3Blob = fileName;

            // SAVE / UPDATE IT
            TableOperation updateoOperation = TableOperation.InsertOrReplace(sampleEntity);
            table.Execute(updateoOperation);

            // ADD A MESSAGE IN THE QUEUE TO PICKUP THE NEW BLOB
            //getsamplegeneratorQueue().AddMessage(new CloudQueueMessage(System.Text.Encoding.UTF8.GetBytes(fileName)));
            getsamplegeneratorQueue().AddMessage(new CloudQueueMessage(JsonConvert.SerializeObject(sampleEntity)));
            System.Diagnostics.Trace.WriteLine(String.Format("*** WebRole: Enqueued '{0}'", path));

            return Ok(sampleEntity);
            //            return Ok();

        }

        // PUT: api/Mp3s/5
        public IHttpActionResult Put(int id, [FromBody]string value)
        {
            Console.WriteLine(id);
            Console.WriteLine(value);
            return Ok();
        }

        // DELETE: api/Mp3s/5
        public void Delete(string id)
        {
        }
    }
}
