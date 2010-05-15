using System;
using System.Collections.Generic;
using System.Text;

namespace MSNPSharp
{
    /// <summary>
    /// Represent the information of a certain endpoint.
    /// </summary>
    public class EndPointData
    {
        #region Fields and Properties

        private Guid id = Guid.Empty;

        /// <summary>
        /// The Id of endpoint data.
        /// </summary>
        public Guid Id
        {
            get { return id; }
        }

        private ClientCapacities clientCapacities = ClientCapacities.None;

        /// <summary>
        /// The capacities of the client at this enpoint.
        /// </summary>
        public ClientCapacities ClientCapacities
        {
            get { return clientCapacities; }

            internal set 
            { 
                clientCapacities = value; 
            }
        }

        private ClientCapacitiesEx clientCapacitiesEx = ClientCapacitiesEx.None;

        /// <summary>
        /// The new capacities of the client at this enpoint.
        /// </summary>
        public ClientCapacitiesEx ClientCapacitiesEx
        {
            get { return clientCapacitiesEx; }
            internal set { clientCapacitiesEx = value; }
        }


        #endregion

        protected EndPointData()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="epId">The endpoint Id.</param>
        public EndPointData(Guid epId)
        {

        }
    }

    public class PrivateEndPointData : EndPointData
    {
        private string name = string.Empty;

        /// <summary>
        /// The EpName xml node of UBX command payload.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private string clientType = string.Empty;

        /// <summary>
        /// The ClientType xml node of UBX command payload.
        /// </summary>
        public string ClientType
        {
            get { return clientType; }
            set { clientType = value; }
        }

        private bool idle = false;

        /// <summary>
        /// The Idle xml node of UBX command payload.
        /// </summary>
        public bool Idle
        {
            get { return idle; }
            set { idle = value; }
        }

        private PresenceStatus state = PresenceStatus.Unknown;

        public PresenceStatus State
        {
            get { return state; }
            set { state = value; }
        }

        public PrivateEndPointData(Guid epId)
            : base(epId)
        {
        }
    }
}
