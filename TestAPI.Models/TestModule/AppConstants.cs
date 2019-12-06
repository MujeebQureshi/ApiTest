using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAPI.Models
{
    public static class AppConstants
    {
        #region Key

        public const string AppSecretKeyFormsAuth = "mCUHr0ZYTaQvCPiXocWnSqFtXFA";
        public const string AppSecretKeyPassword  = "mCUHr0ZYTaQvCPiXocWnSqFtXP";
        public const string AppSecretLinkObject   = "mCUHr0ZYTaQvCPiXocWnSqFtXLO";

        #endregion

        #region DDLs

        private class DDList
        {
            public DDList(string Text, object Value)
            {
                this.Text = Text;
                this.Value = Value;
            }
            public string Text { get; set; }
            public object Value { get; set; }
            public bool isSelected { get; set; }
        }
        
        internal static object getPropertyType()
        {
            return new List<DDList>()
            {{ new DDList("--Select Property Type--", "") },
                { new DDList("Developmental", "D") },
                { new DDList("Rental", "R") },
                { new DDList("Land", "L") }
            };
        }

        internal static object getSizeUnit()
        {
            return new List<DDList>()
            {{ new DDList("--Select size unit--", "") },
                { new DDList("Square Yards", "SQYD") },
                { new DDList("Square foot", "SQFT") },
                { new DDList("Land", "LAND") }
            };
        }

        internal static object getYN()
        {
            return new List<DDList>()
            { { new DDList("--Select an option--", "") },
                { new DDList("Yes", "Y") },
                { new DDList("No", "N") }  
            };
        }

        internal static object getCommercialResidential()
        {
            return new List<DDList>()
            { { new DDList("--Select an option--", "") },
                { new DDList("Commercial", "C") },
                { new DDList("Residential", "R") }
            };
        }
        internal static object getShareStatus()
        {
            return new List<DDList>()
            { { new DDList("--Select an option--", "") },
                { new DDList("Approved", "A") },
                { new DDList("OnHold", "H") }, 
                { new DDList("Cancel", "C") },
                 { new DDList("Sold", "S") }
            };
        }

        internal static object getPropertyStatus()
        {
            return new List<DDList>()
            { { new DDList("--Select an option--", "") },
                { new DDList("On Hold", "H") },
                { new DDList("Open", "O") },
                { new DDList("Completed", "C") },
                { new DDList("Sold", "S") },
            };
        }

        internal static object getTimeUnit()
        {
            return new List<DDList>()
            { { new DDList("--Select an option--", "") },
                { new DDList("Year", "Y") },
                { new DDList("Month", "M") }
            };
        }

        internal static object getShareBreak()
        {
            return new List<DDList>()
            {
                { new DDList("--Select option--", 0) },
                { new DDList("0.1", 0.1) },
                { new DDList("0.2", 0.2) },
                { new DDList("0.3", 0.3) },
                { new DDList("0.4", 0.4) },
                { new DDList("0.5", 0.5) }
            };
        }
        
        #endregion

    }
}
