using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.WindowsAzure.Storage.Table;

namespace CWBSampleLibrary.Models
{
    /// <summary>
    /// Class to represent the sample entity which will be stored
    /// </summary>
    public class SampleEntity : TableEntity
    {

        // SAMPLE PROPERTIES
        public string Title { get; set; }
        public string Artist { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Mp3Blob { get; set; }
        public string SampleMp3Blob { get; set; }
        public string SampleMp3Url { get; set; }
        public DateTime SampleDate { get; set; }



        /// <summary>
        /// Create new instance of SampleEntity
        /// </summary>
        /// <param name="partitionKey">The Partition Key for the Table</param>
        /// <param name="sampleId">The RowId for the actual sample entity</param>
        public SampleEntity(string partitionKey, string sampleId)
        {
            PartitionKey = partitionKey;
            RowKey = sampleId;


        }

        public SampleEntity()
        {

        }

    }
}