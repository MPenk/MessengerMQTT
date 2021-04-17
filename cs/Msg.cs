using System;
using System.Collections.Generic;
using System.Text;

namespace MessengerMQTT
{
    public class Msg
    {
        public string type { get; set; }
        public string data { get; set; }
        public string topic { get; set; }
        public string name { get; set; }
        public string uID { get; set; }

        public Msg(string type, string data, string topic, string name, string uID)
        {
            this.type = type;
            this.data = data;
            this.topic = topic;
            this.name = name;
            this.uID = uID;
        }
    }
}
