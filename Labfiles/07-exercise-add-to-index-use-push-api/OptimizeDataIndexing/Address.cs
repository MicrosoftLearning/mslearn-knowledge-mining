using Azure.Search.Documents.Indexes;
using System;

namespace OptimizeDataIndexing
{
    [Serializable]
    public partial class Address
    {
        [SearchableField]
        public string StreetAddress { get; set; }

        [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string City { get; set; }

        [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string StateProvince { get; set; }

        [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string PostalCode { get; set; }

        [SearchableField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
        public string Country { get; set; }
    }
}
