namespace ReportAnalizerWASM.Client.Models
{
    public class TransaccionMP
    {
        public string DATE { get; set; }
        public string DESCRIPTION { get; set; }
        public decimal GROSS_AMOUNT { get; set; }
        public decimal MP_FEE_AMOUNT { get; set; }
        public decimal TAXES_AMOUNT { get; set; }
        public decimal NET_CREDIT_AMOUNT { get; set; }
        public decimal NET_DEBIT_AMOUNT { get; set; }
        public string PAYMENT_METHOD { get; set; }
    }
}
