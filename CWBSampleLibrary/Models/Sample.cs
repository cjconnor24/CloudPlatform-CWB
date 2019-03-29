using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace CWBSampleLibrary.Models
{
    public class Sample
    {


        [Key]
        public string SampleID { get; set; }

        public string Title { get; set; }

        public string Artist { get; set; }

        public string SampleMp3Url { get; set; }



    }
}