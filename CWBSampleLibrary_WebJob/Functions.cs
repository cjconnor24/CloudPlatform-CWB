using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CWBSampleLibrary;
using CWBSampleLibrary.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using NAudio.Wave;

namespace CWBSampleLibrary_WebJob
{
    public class Functions
    {


        // TABLE NAME CONSTANT
        public const String TABLE_NAME = "Samples";

        /// <summary>
        /// Generates a sample based on the message in the queue
        /// </summary>
        /// <param name="queueSample">The sample from which to add the Mp3 Sample</param>
        /// <param name="tableSample">The sample from the table</param>
        /// <param name="tableBinding">The cloud table binding</param>
        /// <param name="logger">Logged object for outputs</param>
        public static void GenerateSample(
            [QueueTrigger("samplegenerator")] SampleEntity queueSample,
            [Table(TABLE_NAME, "{PartitionKey}","{RowKey}")] SampleEntity tableSample,
            [Table(TABLE_NAME)] CloudTable tableBinding, TextWriter logger)
        {

            // GET THE BLOB REFERENCE
            BlobStorageService blobStorageService = new BlobStorageService();
            CloudBlobContainer audioGalleryContainer = blobStorageService.getCloudBlobContainer();

            // GET THE INPUT BLOB FROM STORAGE
            CloudBlob inputBlob = audioGalleryContainer
                .GetDirectoryReference("files")
                .GetBlobReference(tableSample.Mp3Blob);

            // CREATE SAMPLE BLOB NAME
            string newFileName = $"{Guid.NewGuid()}-{tableSample.Title.Replace(" ", "-")}-sample.mp3";
            string path = "samples/" + newFileName;

            // CREATE THE OUTPUT BLOB
            CloudBlockBlob outputBlob = audioGalleryContainer
                .GetBlockBlobReference(path);


            // MAKE SURE THE INCOMING BLOB HAS AN MP3 EXTENSION
            if (Path.GetExtension(inputBlob.Name) == ".mp3")
            {

                // OPEN THE INPUT AND OUTPUT STREAMS FOR MODIFICATION
                using (Stream input = inputBlob.OpenRead())
                using (Stream output = outputBlob.OpenWrite())
                {
                    // CREATE THE SAMPLE FOR 20s AND UPDATE MIME TYPES
                    CreateAudioSample(input, output, 20);
                    outputBlob.Properties.ContentType = "audio/mpeg3";
                }

                logger.WriteLine("GenerateSample() completed...");

                // WRITE THE SAMPLE DATA TO THE TABLE
                tableSample.SampleDate = DateTime.Now;
                tableSample.SampleMp3Blob = newFileName;
                tableSample.SampleMp3Url = outputBlob.Uri.ToString();
                var updateRecord = TableOperation.InsertOrReplace(tableSample);
                tableBinding.Execute(updateRecord);

            }
            else
            {

                logger.WriteLine("Sample not processed. No MP3 extension");
            }

            // DEBUGGING LOG
            logger.WriteLine("GenerateSample() complete.");

        }


        private static void CreateAudioSample(Stream input, Stream output, int duration)
        {

            using (var reader = new Mp3FileReader(input, wave => new NLayer.NAudioSupport.Mp3FrameDecompressor(wave)))

            {

                Mp3Frame frame;

                frame = reader.ReadNextFrame();
                int frameTimeLength = (int)(frame.SampleCount / (double)frame.SampleRate * 1000.0);
                int framesRequired = (int)(duration / (double)frameTimeLength * 1000.0);

                // EXPERIMENTING WITH TAKING CLIP FROM THE MIDDLE
                int middleFrame = (frameTimeLength / 2);
                int startFrame = middleFrame - (framesRequired / 2);
                int endFrame = middleFrame + (framesRequired / 2);


                int frameNumber = 0;

                while ((frame = reader.ReadNextFrame()) != null)
                {

                    frameNumber++;

                    if (frameNumber <= framesRequired)
                    {
                        output.Write(frame.RawData, 0, frame.RawData.Length);
                    }

                    else break;

                }

            }

        }
    }
}
