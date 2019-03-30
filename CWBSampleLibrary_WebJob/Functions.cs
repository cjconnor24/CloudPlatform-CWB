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
        // This class contains the application-specific WebJob code consisting of event-driven
        // methods executed when messages appear in queues with any supporting code.

        // Trigger method  - run when new message detected in queue. "samplegenerator" is name of queue.
        // "audiogallery" is name of storage container; "images" and "thumbanils" are folder names. 
        // "{queueTrigger}" is an inbuilt variable taking on value of contents of message automatically;
        // the other variables are valued automatically.
        //public static void GenerateSample(
        //[QueueTrigger("samplegenerator")] String blobInfo,
        //[Blob("audiogallery/files/{queueTrigger}")] CloudBlockBlob inputBlob,
        //[Blob("audiogallery/samples/{queueTrigger}")] CloudBlockBlob outputBlob, TextWriter logger)

        public const String TABLE_NAME = "Samples";

        public static void GenerateSample(
            [QueueTrigger("samplegenerator")] SampleEntity queueSample,
            [Table(TABLE_NAME, "{PartitionKey}","{RowKey}")] SampleEntity tableSample,
            [Table(TABLE_NAME)] CloudTable tableBinding, TextWriter logger)
        {

            // GET THE BLOB REFERENCE
            BlobStorageService blobStorageService = new BlobStorageService();
            CloudBlobContainer audioGalleryContainer = blobStorageService.getCloudBlobContainer();

            CloudBlob inputBlob = audioGalleryContainer
                .GetDirectoryReference("files")
                .GetBlobReference(tableSample.Mp3Blob);

            CloudBlockBlob outputBlob = audioGalleryContainer
                .GetDirectoryReference("samples")
                .GetBlockBlobReference("sample-" + tableSample.Mp3Blob);


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


                // GET META DATA
                inputBlob.FetchAttributes();
                // WRITE TITLE TO NEW SAMPLE BLOB
                outputBlob.Metadata["Title"] = inputBlob.Metadata["Title"];
                // SAVE THE METADATA
                outputBlob.SetMetadata();

                // WRITE THE SAMPLE DATA TO THE TABLE
                tableSample.SampleDate = DateTime.Now;
                tableSample.SampleMp3Blob = outputBlob.Name;
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
