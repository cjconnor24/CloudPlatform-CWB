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
    public class Mp3sController : BaseController
    {


        /// <summary>
        /// Construct the Mp3 controller and get the necessary references from base class
        /// </summary>
        public Mp3sController() : base(){}


        // POST: api/Mp3s
        /// <summary>
        /// Upload an mp3 related to an mp3. Puts the mp3 into the sample queue to create a sample.
        /// </summary>
        /// <param name="id">Id of the sample entity to upload the file for</param>
        /// <returns>The details of the sample</returns>
        [ResponseType(typeof(Sample))]
        public IHttpActionResult Post(string id)
        {
            
            // GET THE ID - MAKE SURE IT ISNT BLANK
            if (id == null)
            {
                Console.WriteLine(id);
                return BadRequest("Please enter an ID");
            }

            // CHECK THE ID EXISTS IN THE TABLE ALREADY
            TableOperation getOperation = TableOperation.Retrieve<SampleEntity>(partitionName, id);

            // EXECUTE THE GET OPERATION
            TableResult getOperationResult = table.Execute(getOperation);

            // CHCEK TO SEE IF THE SAMPLE EXISTS
            if (getOperationResult.Result == null) return NotFound();

            // OTHERWISE GET THE SAMPLE
            SampleEntity sampleEntity = (SampleEntity)getOperationResult.Result;
            Sample sample = new Sample()
            {
                SampleID = sampleEntity.RowKey,
                Title = sampleEntity.Title,
                Artist = sampleEntity.Artist
            
            };

            // CHECK IF BLOBS EXISTS ALREADY
            if (sampleEntity.Mp3Blob != null)
            {
                // DELETE EXISTING BLOBS
                Delete(sampleEntity);
            }

            // CREATE NAME FROM Sample Data
            string title = sampleEntity.Title;
            string fileName = string.Format("{0}-{1}{2}", Guid.NewGuid(), title.Replace(" ", "-"), ".mp3");
            string path = "files/" + fileName;

            // GET THE BINARY SAMPLE BEING UPLOADED
            var postData = HttpContext.Current.Request;
            var blob = getaudiogalleryContainer().GetBlockBlobReference(path);
            blob.Properties.ContentType = "audio/mpeg3";

            // SAVE THE BLOB
            blob.UploadFromStream(postData.InputStream);
            blob.SetMetadata();

            // WRITE TO THE TABLES
            sampleEntity.Mp3Blob = fileName;

            // SAVE / UPDATE IT
            TableOperation updateoOperation = TableOperation.InsertOrReplace(sampleEntity);
            table.Execute(updateoOperation);

            // ADD A MESSAGE IN THE QUEUE TO PICKUP THE NEW BLOB
            getsamplegeneratorQueue().AddMessage(new CloudQueueMessage(JsonConvert.SerializeObject(sampleEntity)));
            System.Diagnostics.Trace.WriteLine(String.Format("*** WebRole: Enqueued '{0}'", path));

            return Ok(sample);

        }

        // DELETE: api/Mp3s/5
        /// <summary>
        /// Delete any blobs associated with the provided sample ID
        /// </summary>
        /// <param name="sample">The sample ID to check for blobs and delete</param>
        public void Delete(SampleEntity sample)
        {
            if (sample.Mp3Blob != null)
            {
                // GET BLOB REFERENCE AND DELETE
                var Mp3 = getaudiogalleryContainer()
                    .GetDirectoryReference("files")
                    .GetBlobReference(sample.Mp3Blob);
                if (Mp3.Exists())
                {
                    Mp3.Delete();
                }

            }

            // CHECK THE SHORT SAMPLES AND DELETE
            if (sample.SampleMp3Blob != null)
            {
                // GET SAMPLE REFERENCE AND DELETE
                var Mp3Sample = getaudiogalleryContainer()
                    .GetDirectoryReference("samples")
                    .GetBlobReference(sample.SampleMp3Blob);
                if (Mp3Sample.Exists())
                {
                    Mp3Sample.Delete();
                }
            }
            
        }
    }
}
