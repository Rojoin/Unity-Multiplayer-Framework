using System;
using System.Collections.Generic;

namespace RojoinNetworkSystem
{
    [Serializable]
    public class MessageCache
    {
        public MessageType type;
        public List<byte> data;
        public ulong messageId;
        // public Player player;
        public float timerForDelete = 0.0f;
        public float timerForResend = 0.0f;
        public bool startTimer = false;
        public bool canBeResend = false;

        public MessageCache(MessageType newtype, List<byte> newdata, ulong newmessageId)
        {
            type = newtype;
            data = newdata;
            messageId = newmessageId;
            timerForDelete = 0.0f;
            timerForResend = 0.0f;
            startTimer = false;
        }

        public MessageCache(MessageType newtype, ulong newmessageId)
        {
            type = newtype;
            messageId = newmessageId;
            timerForDelete = 0.0f;
            timerForResend = 0.0f;
            startTimer = false;
        }
    }
}