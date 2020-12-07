using System;
using System.Collections.Generic;
using System.Text;

namespace MessengerMQTT
{
    public class Message : Msg
    {
        static List<Message> listMessages = new List<Message>();


        private int id { get; set; }


        public Message(string data, string topic, string myName)
        : base(data, topic, myName)
        {
            this.id = Message.getSize();
            Message.listMessages.Add(this);
        }
        static public int getSize()
        {

            return Message.listMessages.Count;
        }

        public int getId()
        {

            return this.id;

        }
        public string getMessage()
        {

            return this.data;

        }

    }
}