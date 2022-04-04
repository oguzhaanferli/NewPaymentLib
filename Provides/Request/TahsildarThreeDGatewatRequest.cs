using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib.Payment.Provides.Request
{
    public class Card
    {
        public string holder_name { get; set; }
        public string number { get; set; }
        public string expire_month { get; set; }
        public string expire_year { get; set; }
        public string cvc { get; set; }
    }

    public class Purchaser
    {
        public object email { get; set; }
        public object first_name { get; set; }
        public object last_name { get; set; }
        public object mobile_phone { get; set; }
        public object company { get; set; }
        public object description { get; set; }
    }

    public class User
    {
        public object id { get; set; }
        public object customer_id { get; set; }
        public object parent_id { get; set; }
        public object ws_code { get; set; }
        public object customer_ws_code { get; set; }
        public object parent_ws_code { get; set; }
        public object email { get; set; }
        public object password { get; set; }
        public object first_name { get; set; }
        public object last_name { get; set; }
        public object company { get; set; }
        public object tax_office { get; set; }
        public object tax_number { get; set; }
        public object mobile_phone { get; set; }
        public object phone { get; set; }
        public object fax { get; set; }
        public object country_code { get; set; }
        public object city { get; set; }
        public object address { get; set; }
        public object zip { get; set; }
        public object credibility { get; set; }
        public bool is_corporate { get; set; }
        public object identity_no { get; set; }
        public object plasiyer_admin_id { get; set; }
        public object group_id { get; set; }
    }

    public class TahsildarThreeDGatewatRequest
    {
        public string public_key { get; set; }
        public string price { get; set; }
        public string currency { get; set; }
        public string installment { get; set; }
        public string order_no { get; set; }
        public string secure3d { get; set; }
        public string hash { get; set; }
        public string ip { get; set; }
        public string return_url { get; set; }
        public string provision_type { get; set; }
        public Card card { get; set; }
    }
}
