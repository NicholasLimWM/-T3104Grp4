//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ICT3104_Group4_SMS.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public partial class Grade
    {
        public Grade()
        {
            this.Recommendations = new HashSet<Recommendation>();
        }
    
        public int Id { get; set; }
        public string studentId { get; set; }
        public int lecturermoduleId { get; set; }
        public double score { get; set; }

        public virtual ICollection<Recommendation> Recommendations { get; set; }
    }
}
