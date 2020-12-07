using System;
using System.Collections.Generic;
using System.Text;

namespace MessengerMQTT
{
    public class Msg
    {
        public string data { get; set; }
        public string topic { get; set; }
        public string myName { get; set; }

        public Msg(string data, string topic, string myName)
        {
            this.data = data;
            this.topic = topic;
            this.myName = myName;
        }
    }
}
