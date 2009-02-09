using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    public enum P2PApplicationStatus
    {
        Waiting,
        Active,
        Finished,
        Aborted,
        Error
    }

    public abstract class P2PApplication : IDisposable, IMessageHandler
    {
        #region Events

        public event EventHandler<EventArgs> TransferStarted;
        public event EventHandler<EventArgs> TransferFinished;
        public event EventHandler<ContactEventArgs> TransferAborted;
        public event EventHandler<EventArgs> TransferError;

        #endregion

        #region Members

        private P2PApplicationStatus status = P2PApplicationStatus.Waiting;
        private IMessageProcessor iProcessor;
        private NSMessageHandler nsMessageHandler;
        private P2PSession p2pSession;
        private Contact remote;

        #endregion

        #region .ctor

        protected P2PApplication(Contact remote)
        {
            this.remote = remote;
            this.nsMessageHandler = remote.NSMessageHandler;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Application created", GetType().Name);
        }

        protected P2PApplication(P2PSession session)
            : this(session.Remote)
        {
            Session = session;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Application associated with session: " + session.SessionId, GetType().Name);
        }

        #endregion

        #region Properties

        #region IMessageHandler Members

        public IMessageProcessor MessageProcessor
        {
            get
            {
                return iProcessor;
            }
            set
            {
                iProcessor = value;
            }
        }

        #endregion

        public abstract string InvitationContext
        {
            get;
        }

        public Owner Local
        {
            get
            {
                return NSMessageHandler.Owner;
            }
        }

        public Contact Remote
        {
            get
            {
                return remote;
            }
        }

        public NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
        }

        public P2PApplicationStatus ApplicationStatus
        {
            get
            {
                return status;
            }
        }

        public virtual bool AutoAccept
        {
            get
            {
                return false;
            }
        }

        public virtual uint ApplicationId
        {
            get
            {
                return P2PApplications.FindApplicationId(this);
            }
        }

        public virtual Guid ApplicationEufGuid
        {
            get
            {
                return P2PApplications.FindApplicationEufGuid(this);
            }
        }

        public P2PSession Session
        {
            get
            {
                return p2pSession;
            }
            set
            {
                if (p2pSession != null)
                {
                    p2pSession.Closing -= P2PSessionClosing;
                    p2pSession.Closed -= P2PSessionClosed;
                    p2pSession.Error -= P2PSessionError;
                }

                p2pSession = value;

                if (p2pSession != null)
                {
                    p2pSession.Closing += P2PSessionClosing;
                    p2pSession.Closed += P2PSessionClosed;
                    p2pSession.Error += P2PSessionError;
                }
            }
        }


        #endregion


        public abstract void HandleMessage(IMessageProcessor bridge, NetworkMessage message);


        /// <summary>
        /// Validate invitation for this application.
        /// </summary>
        /// <param name="invitation"></param>
        /// <returns></returns>
        public virtual bool ValidateInvitation(SLPMessage invitation)
        {
            return (invitation.ToMail == Local.Mail);
        }

        public virtual void Start()
        {
            OnTransferStarted(EventArgs.Empty);
        }

        public virtual void Accept()
        {
            p2pSession.Accept();
        }

        public virtual void Decline()
        {
            p2pSession.Decline();
        }

        public virtual void Abort()
        {
            OnTransferAborted(new ContactEventArgs(nsMessageHandler.Owner));
            p2pSession.Close();
        }

        public virtual void Dispose()
        {
            Session = null;

            if ((status != P2PApplicationStatus.Aborted) &&
                (status != P2PApplicationStatus.Finished) &&
                (status != P2PApplicationStatus.Error))
            {
                OnTransferError(EventArgs.Empty);
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Application disposed", GetType().Name);
        }

        #region Protected

        protected virtual void OnTransferStarted(EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Transfer started", GetType().Name);

            status = P2PApplicationStatus.Active;

            if (TransferStarted != null)
                TransferStarted(this, e);
        }

        protected virtual void OnTransferFinished(EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Transfer finished", GetType().Name);

            status = P2PApplicationStatus.Finished;

            if (TransferFinished != null)
                TransferFinished(this, e);
        }

        protected virtual void OnTransferAborted(ContactEventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, String.Format("Transfer aborted ({0})", e.Contact == nsMessageHandler.Owner ? "locally" : "remotely"), GetType().Name);

            status = P2PApplicationStatus.Aborted;

            if (TransferAborted != null)
                TransferAborted(this, e);
        }

        protected virtual void OnTransferError(EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Transfer error", GetType().Name);

            status = P2PApplicationStatus.Error;

            if (TransferError != null)
                TransferError(this, e);
        }

        #endregion

        #region P2PSession Handling

        private void P2PSessionClosing(object sender, ContactEventArgs e)
        {
            if (e.Contact != nsMessageHandler.Owner)
            {
                if (status == P2PApplicationStatus.Waiting || status == P2PApplicationStatus.Active)
                    OnTransferAborted(e);
            }
        }

        private void P2PSessionClosed(object sender, ContactEventArgs e)
        {
            if (status == P2PApplicationStatus.Waiting || status == P2PApplicationStatus.Active)
            {
                OnTransferAborted(e);
            }
        }

        private void P2PSessionError(object sender, EventArgs e)
        {
            if (status != P2PApplicationStatus.Error)
                OnTransferError(e);
        }

        #endregion
    }
};
