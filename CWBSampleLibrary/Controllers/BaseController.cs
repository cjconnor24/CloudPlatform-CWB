using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace CWBSampleLibrary.Controllers
{
    /// <summary>
    /// Base controller to keep the shared logic for both the samples and mp3 controllers
    /// </summary>
    public abstract class BaseController : ApiController
    {

        // CONSTANT FOR REPRESENT TABLE NAME
        protected const String partitionName = "samples_Partition_1";
        public const String TABLE_NAME = "Samples";

        // TABLE, STORAGE AND QUEUE REFERENCES
        protected BlobStorageService _blobStorageService = new BlobStorageService();
        protected CloudQueueService _queueStorageService = new CloudQueueService();
        protected CloudStorageAccount _storageAccount;
        protected CloudTableClient _tableClient;
        protected CloudTable table;

        protected BaseController()
        {
            _storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ToString());
            _tableClient = _storageAccount.CreateCloudTableClient();
            table = _tableClient.GetTableReference(TABLE_NAME);
        }


        protected CloudBlobContainer getaudiogalleryContainer()
        {
            return _blobStorageService.getCloudBlobContainer();
        }

        protected CloudQueue getsamplegeneratorQueue()
        {
            return _queueStorageService.getCloudQueue();
        }

    }
}