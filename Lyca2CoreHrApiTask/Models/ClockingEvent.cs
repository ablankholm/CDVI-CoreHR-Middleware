using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CodeDom;
using System.Data;
using NPoco;
using System.Data.SqlTypes;

namespace Lyca2CoreHrApiTask.Models
{
    [TableName("Events")]
    [PrimaryKey("Event ID")]
    public class ClockingEvent
    {
        [Column("Event ID")]
        public int EventID { get; set; }
        [Column("Event Type")]
        public int EventType { get; set; }
        [Column("Field Time")]
        public DateTime FieldTime { get; set; }
        [Column("Logged Time")]
        public DateTime LoggedTime { get; set; }
        [Column("Operator ID")]
        public int OperatorID { get; set; }
        [Column("Card Holder ID")]
        public int CardHolderID { get; set; }
        [Column("Record Name ID")]
        public int RecordNameID { get; set; }
        [Column("Site Name ID")]
        public int SiteNameID { get; set; }
        [Column("UserNameID")]
        public int UserNameID { get; set; }
    }
}