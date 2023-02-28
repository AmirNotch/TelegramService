namespace Domain
{
    public class Token
    {
        public Guid Id { get; set; }
        public int API_ID { get; set; }
        public string API_HASH { get; set; }
        //public int Status { get; set; }
        public long PhoneNumber { get; set; }
    }
}
