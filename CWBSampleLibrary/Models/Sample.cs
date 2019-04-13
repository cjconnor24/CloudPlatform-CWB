using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CWBSampleLibrary.Models
{
    /// <summary>
    /// Class to represent the Sample model
    /// </summary>
    public class Sample
    {



        [Key]
        public string SampleID { get; set; }

        // PROPERTIES FOR THE SAMPLE
        public string Title { get; set; }

        public string Artist { get; set; }

        public string SampleMp3Url { get; set; }



    }
}