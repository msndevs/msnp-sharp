using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace MSNPSharp
{
    using MSNPSharp.DataTransfer;

    /// <summary>
    /// Delay sending the p2p invitation messages, avoid p2p data transfer request a new conversation.
    /// </summary>
    internal static class P2PInvitationScheduler
    {
        private static object syncObject = new object();
        private const int delayTime = 5000; //In ms.
        private static Timer timer = new Timer(OnTimerCallback, null, 0, DelayTime);
        private static Queue<KeyValuePair<DateTime, KeyValuePair<P2PMessage, P2PMessageSession>>> messageQueue = new Queue<KeyValuePair<DateTime, KeyValuePair<P2PMessage, P2PMessageSession>>>();

        public static int DelayTime
        {
            get { return delayTime; }
        } 


        #region Private method

        private static void EnqueueMessage(P2PMessageSession p2pSession, P2PMessage message)
        {
            if (p2pSession == null || message == null)
                return;

            lock (syncObject)
            {

                messageQueue.Enqueue(new KeyValuePair<DateTime, KeyValuePair<P2PMessage, P2PMessageSession>>(
                    DateTime.Now, new KeyValuePair<P2PMessage, P2PMessageSession>(message, p2pSession)));
            }
        }

        private static void OnTimerCallback(object state)
        {
            lock (syncObject)
            {
                if (messageQueue.Count == 0)
                    return;

                DateTime currentTime = DateTime.Now;
                TimeSpan span = new TimeSpan(0, 0, 0, 0, DelayTime);

                while (messageQueue.Count > 0 && currentTime - messageQueue.Peek().Key >= span)
                {
                    KeyValuePair<DateTime, KeyValuePair<P2PMessage, P2PMessageSession>> item = messageQueue.Dequeue();

                    item.Value.Value.SendMessage(item.Value.Key);
                }
            }
        }

        #endregion

        #region Public Method

        /// <summary>
        /// Add the message to sending queue.
        /// </summary>
        /// <param name="p2pSession"></param>
        /// <param name="message"></param>
        public static void Enqueue(P2PMessageSession p2pSession, P2PMessage message)
        {
            EnqueueMessage(p2pSession, message);
        }

        #endregion
    }
}
