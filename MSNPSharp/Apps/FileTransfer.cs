#region Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice.
All rights reserved. http://code.google.com/p/msnp-sharp/

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its
  contributors may be used to endorse or promote products derived from this
  software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 'AS IS'
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
THE POSSIBILITY OF SUCH DAMAGE. 
*/
#endregion

using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace MSNPSharp.Apps
{
    using MSNPSharp;
    using MSNPSharp.P2P;
    using MSNPSharp.Core;

    #region FTType

    public enum FTType : uint
    {
        NoPreview = 1,
        Background = 4,
        Unknown = 8
    }

    public class FTContext
    {
        private uint _version = 3;
        private ulong _fileSize = 0;
        private FTType _type = FTType.NoPreview;
        private string _filename = string.Empty;
        private byte[] _unknown1 = new byte[0];
        private uint _unknown2 = 0xFFFFFFFF; //0xFFFFFFFE for backgrounds
        private byte[] _unknown3 = new byte[54];
        private byte[] _preview = new byte[0];

        public uint Version
        {
            get
            {
                return _version;
            }
        }

        public ulong FileSize
        {
            get
            {
                return _fileSize;
            }
        }

        public FTType Type
        {
            get
            {
                return _type;
            }
        }

        public string Filename
        {
            get
            {
                return _filename;
            }
        }

        public byte[] Preview
        {
            get
            {
                return _preview;
            }
        }

        public FTContext(byte[] data)
        {
            MemoryStream stream = new MemoryStream(data);
            BinaryReader reader = new BinaryReader(stream);

            uint _headerLen = reader.ReadUInt32();
            _version = reader.ReadUInt32();
            _fileSize = reader.ReadUInt64();
            _type = (FTType)reader.ReadUInt32();
            _filename = Encoding.Unicode.GetString(reader.ReadBytes(520)).Replace("\0", String.Empty).Trim();
            //_unknown1 = Convert.FromBase64String (Encoding.Unicode.GetString(reader.ReadBytes(30)));
            //_unknown2 = (uint)BitUtility.ToInt32 (reader.ReadBytes(4), 0, false);

            if (data.Length > _headerLen)
            {
                // Picture previw
                _preview = new byte[data.Length - _headerLen];
                Array.Copy(data, _headerLen, _preview, 0, _preview.Length);
            }

            reader.Close();
            stream.Close();
        }

        public FTContext(string filename, ulong filesize)
        {
            _filename = filename;
            _fileSize = filesize;
        }

        public byte[] GetBytes()
        {
            int length = 638 + _preview.Length;

            byte[] data = new byte[length];
            MemoryStream stream = new MemoryStream(data);
            BinaryWriter writer = new BinaryWriter(stream);

            writer.Write((uint)length);
            writer.Write((uint)_version);
            writer.Write((ulong)_fileSize);
            writer.Write((uint)_type);
            writer.Write(Pad(Encoding.Unicode.GetBytes(_filename), 520));
            writer.Write(Pad(_unknown1, 30));
            writer.Write((uint)_unknown2);
            writer.Write(_unknown3);

            if (_preview.Length > 0)
                writer.Write(_preview);

            writer.Close();
            stream.Close();

            return data;
        }

        private byte[] Pad(byte[] data, uint len)
        {
            byte[] ret = new byte[len];
            Array.Copy(data, ret, data.Length);
            return ret;
        }
    }

    #endregion

    [P2PApplication(2, "5D3E02AB-6190-11D3-BBBB-00C04F795683")]
    public class FileTransfer : P2PApplication
    {
        public event EventHandler<EventArgs> Progressed;

        private FTContext _context;
        private Stream _dataStream;
        private bool _sending;
        private bool _sendingData;
        private DateTime p2pv2NextRAK = DateTime.Now.AddSeconds(8);

        public FTContext Context
        {
            get
            {
                return _context;
            }
        }

        public Stream DataStream
        {
            get
            {
                return _dataStream;
            }
            set
            {
                _dataStream = value;
            }
        }

        public bool Sending
        {
            get
            {
                return _sending;
            }
        }

        public long Transferred
        {
            get
            {
                if (Sending)
                    return _dataStream.Position;

                return _dataStream.Length;
            }
        }

        public override string InvitationContext
        {
            get
            {
                return Convert.ToBase64String(_context.GetBytes());
            }
        }

        /// <summary>
        /// We are sender.
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="filename"></param>
        public FileTransfer(Contact remote, string filename)
            : this(remote, File.OpenRead(filename), Path.GetFileName(filename))
        {
        }

        /// <summary>
        /// We are sender.
        /// </summary>
        /// <param name="remote"></param>
        /// <param name="data"></param>
        /// <param name="filename"></param>
        public FileTransfer(Contact remote, Stream data, string filename)
            : base(remote.P2PVersionSupported, remote, remote.SelectRandomEPID())
        {
            _context = new FTContext(filename, (ulong)data.Length);
            _dataStream = data;
            _sending = true;
        }

        /// <summary>
        /// We are receiver.
        /// </summary>
        /// <param name="p2pSession"></param>
        public FileTransfer(P2PSession p2pSession)
            : base(p2pSession)
        {
            _context = new FTContext(Convert.FromBase64String(p2pSession.Invitation.BodyValues["Context"].Value));
            _sending = false;

            _dataStream = null; // Must be set when InvitationReceived
        }

        public override void Dispose()
        {
            if (_dataStream != null)
                _dataStream.Close();

            base.Dispose();
        }

        protected virtual void OnProgressed(EventArgs e)
        {
            if (Progressed != null)
                Progressed(this, e);
        }

        public override void SetupInviteMessage(SLPMessage slp)
        {
            slp.BodyValues["RequestFlags"] = "16";

            base.SetupInviteMessage(slp);
        }

        public override bool ValidateInvitation(SLPMessage invite)
        {
            bool ret = base.ValidateInvitation(invite);
            try
            {
                FTContext context = new FTContext(Convert.FromBase64String(invite.BodyValues["Context"].Value));

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                    String.Format("{0} ({1} bytes) (base validate {2})", context.Filename, context.FileSize, ret), GetType().Name);

                return ret && (!string.IsNullOrEmpty(context.Filename)) && (context.FileSize > 0);
            }
            catch (Exception)
            {
                // We can't parse the context, so refuse the invite
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                    String.Format("Unable to parse file transfer invite: {0}", invite.ToDebugString()), GetType().Name);

                return false;
            }
        }

        public override void Start()
        {
            if (_dataStream == null)
                throw new InvalidOperationException("DataStream must be set before start");

            base.Start();

            if (Sending)
            {
                P2PSession.SendDirectInvite(this.NSMessageHandler, P2PSession.Bridge, P2PSession);

                _dataStream.Seek(0, SeekOrigin.Begin);
                _sendingData = true;

                if (P2PSession.Bridge.Ready(P2PSession))
                    SendChunk();
            }
        }

        private void SendChunk()
        {
            if (!_sendingData)
                return;

            P2PDataMessage p2pChunk = new P2PDataMessage(P2PVersion);

            long offset = _dataStream.Position;

            // First chunk
            if (offset == 0)
            {
                if (P2PVersion == P2PVersion.P2PV1)
                {
                    P2PSession.IncreaseLocalIdentifier();

                    p2pChunk.V1Header.TotalSize = (ulong)_dataStream.Length;
                }
                else if (P2PVersion == P2PVersion.P2PV2)
                {
                    P2PSession.dataPacketNumber++;

                    p2pChunk.V2Header.TFCombination = TFCombination.First;
                }
            }

            p2pChunk.Header.Identifier = P2PSession.LocalIdentifier;

            p2pChunk.WriteBytes(_dataStream, P2PSession.Bridge.MaxDataSize);

            if (P2PVersion == P2PVersion.P2PV1)
            {
                p2pChunk.V1Header.Flags = P2PFlag.FileData;

            }
            else if (P2PVersion == P2PVersion.P2PV2)
            {
                p2pChunk.V2Header.PackageNumber = P2PSession.dataPacketNumber;
                p2pChunk.V2Header.TFCombination |= TFCombination.FileTransfer;

                P2PSession.CorrectLocalIdentifier((int)p2pChunk.Header.MessageSize);

                if (p2pv2NextRAK < DateTime.Now)
                {
                    p2pChunk.V2Header.OperationCode |= (byte)OperationCode.RAK;
                    p2pv2NextRAK = DateTime.Now.AddSeconds(8);
                }
            }

            if (_dataStream.Position == _dataStream.Length)
            {
                _sendingData = false;
                SendMessage(p2pChunk);

                // This is the last chunk of data, register the ACKHandler
                P2PMessage rak = new P2PMessage(P2PVersion);
                SendMessage(rak, delegate(P2PMessage ack)
                {
                    Abort();
                    OnTransferFinished(EventArgs.Empty);
                });
            }
            else
            {
                SendMessage(p2pChunk, null);
            }

            OnProgressed(EventArgs.Empty);
        }

        public override void BridgeIsReady()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                String.Format("bridge is ready {0}", _sendingData ? "(Sending Data)" : string.Empty), GetType().Name);

            if (_sendingData)
                SendChunk();
        }


        public override bool ProcessData(P2PBridge bridge, byte[] data)
        {
            _dataStream.Write(data, 0, data.Length);

            OnProgressed(EventArgs.Empty);

            if (_dataStream.Length == (long)_context.FileSize)
            {
                // Finished transfer
                OnTransferFinished(EventArgs.Empty);
                P2PSession.Close();
            }

            return true;
        }
    }
};
