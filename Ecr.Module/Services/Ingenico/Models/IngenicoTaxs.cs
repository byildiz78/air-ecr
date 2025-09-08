using System;

namespace Ecr.Module.Services.Ingenico.Models
{
    public class IngenicoTaxs
    {
        public int ITaxGroupID { get; set; }
        public string IGroupName { get; set; }
        public int ITaxRate { get; set; }
        public int ITaxIndex { get; set; }

        public string IUpdateDateTime { get; set; }
        public IngenicoTaxs()
        {
            ITaxGroupID = 0;
            IGroupName = "";
            ITaxRate = 0;
            ITaxIndex = 0;
            IUpdateDateTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
        }
    }
}
